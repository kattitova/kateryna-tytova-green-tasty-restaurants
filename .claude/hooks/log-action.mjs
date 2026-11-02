#!/usr/bin/env node
// Claude Code hook for PreToolUse, PostToolUse, PostToolUseFailure, SubagentStart and SubagentStop.
// Appends ONE JSON line per event to .agent-log/actions.jsonl:
//   PreToolUse         -> { ts, event, id, session, mode, agent?, tool, path | cmd | pattern | url | subagent | to }  = "agent proposed"
//   PostToolUse        -> { ..., exit: 0, ms }                                                                         = "agent did"
//   PostToolUseFailure -> { ..., exit: N | "error" | "interrupted", ms }                                               = "agent tried, it failed"
//   SubagentStart/Stop -> { ts, event, session, agent, agent_id }                                                      = subagent lifecycle
// `agent` is the subagent type (for example `implementer`). It is absent for the main thread.
// A PreToolUse line without a matching Post line (same id) = proposed but never executed (blocked or denied).
// The hook never blocks the agent: any error -> exit 0 silently.
// Read the log with: node .claude/hooks/audit-log.mjs
import { appendFileSync, mkdirSync } from "node:fs";
import { join } from "node:path";
import { makeRel, agentOf } from "./rules.mjs";

let raw = "";
process.stdin.setEncoding("utf8");
for await (const chunk of process.stdin) raw += chunk;

let ev = {};
try {
  ev = JSON.parse(raw || "{}");
} catch {
  process.exit(0);
}

const root = process.env.CLAUDE_PROJECT_DIR || ev.cwd || process.cwd();
const event = ev.hook_event_name ?? "unknown";
const ti = ev.tool_input ?? {};
const tool = ev.tool_name;
const rel = makeRel(root, ev.cwd);
const agent = agentOf(ev);

const entry = {
  ts: new Date().toISOString(),
  event,
  ...(ev.tool_use_id ? { id: ev.tool_use_id } : {}),
  session: String(ev.session_id ?? "").slice(0, 8),
  ...(ev.permission_mode ? { mode: ev.permission_mode } : {}),
  ...(agent ? { agent } : {}),
  ...(ev.agent_id ? { agent_id: String(ev.agent_id).slice(0, 12) } : {}),
  ...(tool ? { tool } : {}),
  ...(ti.file_path ? { path: rel(ti.file_path) } : {}),
  ...(ti.notebook_path ? { path: rel(ti.notebook_path) } : {}),
  ...(ti.command ? { cmd: String(ti.command).slice(0, 200) } : {}),
  ...(ti.pattern ? { pattern: ti.pattern } : {}),
  ...(ti.url ? { url: ti.url } : {}),
  ...(ti.subagent_type ? { subagent: ti.subagent_type } : {}),
  ...(ti.to ? { to: String(ti.to).slice(0, 40) } : {}),
};

if (event === "PostToolUse") {
  entry.exit = 0;
} else if (event === "PostToolUseFailure") {
  const m = /^Exit code (\d+)/.exec(ev.error ?? "");
  entry.exit = m ? Number(m[1]) : ev.is_interrupt ? "interrupted" : "error";
}
if (typeof ev.duration_ms === "number") entry.ms = ev.duration_ms;

try {
  const dir = join(root, ".agent-log");
  mkdirSync(dir, { recursive: true });
  appendFileSync(join(dir, "actions.jsonl"), JSON.stringify(entry) + "\n");
} catch {
  /* logging must never fail the session */
}
process.exit(0);
