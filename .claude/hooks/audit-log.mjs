#!/usr/bin/env node
// Reads .agent-log/actions.jsonl and reports how agents followed the harness rules.
// Usage:
//   node .claude/hooks/audit-log.mjs                 all sessions
//   node .claude/hooks/audit-log.mjs --since 2026-11-02T10:00
//   node .claude/hooks/audit-log.mjs --session ab12cd34
//   node .claude/hooks/audit-log.mjs --json          machine-readable output
// Exit code: 1 if it finds a violation, else 0.
import { readFileSync, existsSync } from "node:fs";
import { join } from "node:path";
import { checkWrite, WRITE_TOOLS, MUTATING_CMD, RESTRICTED_CMD, READ_ONLY_AGENTS } from "./rules.mjs";

const args = process.argv.slice(2);
const opt = (name) => {
  const i = args.indexOf(name);
  return i >= 0 ? args[i + 1] : undefined;
};
const since = opt("--since");
const session = opt("--session");
const asJson = args.includes("--json");

const root = process.env.CLAUDE_PROJECT_DIR || process.cwd();
const file = join(root, ".agent-log", "actions.jsonl");
if (!existsSync(file)) {
  console.log(`No log yet: ${file}`);
  process.exit(0);
}

const rows = readFileSync(file, "utf8")
  .split("\n")
  .filter(Boolean)
  .map((l) => {
    try {
      return JSON.parse(l);
    } catch {
      return null;
    }
  })
  .filter(Boolean)
  .filter((r) => (!since || r.ts >= since) && (!session || r.session === session));

const pre = rows.filter((r) => r.event === "PreToolUse");
const post = new Map(
  rows.filter((r) => r.event === "PostToolUse" || r.event === "PostToolUseFailure").map((r) => [r.id, r]),
);
const who = (r) => r.agent ?? "main";

const violations = [];
const warnings = [];
const stats = {};

for (const r of pre) {
  const a = who(r);
  const s = (stats[a] ??= { calls: 0, writes: new Set(), blocked: 0, failed: 0, spawned: {} });
  s.calls++;
  const p = post.get(r.id);
  const executed = p?.event === "PostToolUse";
  if (!p) s.blocked++;
  else if (p.event === "PostToolUseFailure") s.failed++;
  if (r.tool === "Agent" && r.subagent) s.spawned[r.subagent] = (s.spawned[r.subagent] ?? 0) + 1;

  if (WRITE_TOOLS.has(r.tool) && r.path) {
    if (executed) s.writes.add(r.path);
    const broken = checkWrite(r.agent, r.path);
    if (broken) (executed ? violations : warnings).push({ ts: r.ts, agent: a, what: `${r.tool} ${r.path}`, rule: broken, executed });
  }

  if (r.tool === "Bash" && r.cmd) {
    if (RESTRICTED_CMD.test(r.cmd))
      violations.push({ ts: r.ts, agent: a, what: r.cmd, rule: "restricted command (needs an explicit user request)", executed });
    else if (READ_ONLY_AGENTS.has(r.agent) && MUTATING_CMD.test(r.cmd) && !/\.claude\/work\//.test(r.cmd))
      warnings.push({ ts: r.ts, agent: a, what: r.cmd, rule: "read-only agent ran a command that may change files", executed });
    else if (r.agent === "implementer" && MUTATING_CMD.test(r.cmd) && /(Tests\/|\.spec\.ts)/.test(r.cmd))
      warnings.push({ ts: r.ts, agent: a, what: r.cmd, rule: "implementer command touches test files", executed });
    else if (r.agent === "test-writer" && /\bsed\s+-i\b|\btee\b|>{1,2}/.test(r.cmd) && !/(Tests\/|\.spec\.ts|\.claude\/work\/)/.test(r.cmd))
      warnings.push({ ts: r.ts, agent: a, what: r.cmd, rule: "test-writer command may change non-test files", executed });
  }
}

const noAgentField = rows.some((r) => r.event === "SubagentStart") && !rows.some((r) => r.agent && r.event === "PreToolUse");

if (asJson) {
  const out = Object.fromEntries(
    Object.entries(stats).map(([a, s]) => [a, { ...s, writes: [...s.writes] }]),
  );
  console.log(JSON.stringify({ stats: out, violations, warnings, noAgentField }, null, 2));
} else {
  console.log(`Events: ${rows.length}, tool calls: ${pre.length}${since ? `, since ${since}` : ""}${session ? `, session ${session}` : ""}\n`);
  console.log("Per agent:");
  for (const [a, s] of Object.entries(stats)) {
    const spawned = Object.entries(s.spawned).map(([k, v]) => `${k}×${v}`).join(", ");
    console.log(`  ${a.padEnd(15)} calls ${String(s.calls).padStart(4)}  blocked/denied ${s.blocked}  failed ${s.failed}  files written ${s.writes.size}${spawned ? `  spawned: ${spawned}` : ""}`);
  }
  console.log(`\nViolations (rule broken and executed or attempted): ${violations.length}`);
  for (const v of violations) console.log(`  ${v.ts} [${v.agent}] ${v.executed ? "DONE" : "tried"}: ${v.what}\n      rule: ${v.rule}`);
  console.log(`\nWarnings (check by hand): ${warnings.length}`);
  for (const w of warnings) console.log(`  ${w.ts} [${w.agent}] ${w.executed ? "done" : "tried/blocked"}: ${w.what}\n      ${w.rule}`);
  if (noAgentField)
    console.log("\nNote: the log has SubagentStart events but no tool call has an `agent` field. This Claude Code version may not send agent_type to tool hooks, so per-agent rules cannot be checked.");
}
process.exit(violations.length ? 1 : 0);
