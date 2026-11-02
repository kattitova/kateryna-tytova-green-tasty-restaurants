#!/usr/bin/env node
// Claude Code PreToolUse hook for Write, Edit, MultiEdit and NotebookEdit.
// Enforces maker != checker: each subagent may write only its own zone (see rules.mjs).
// Denied calls get a reason that the agent sees. The main thread is not restricted.
// Bash writes are not blocked here; audit-log.mjs reports them.
// Any internal error -> allow (exit 0), so a broken hook never stops the session.
import { makeRel, agentOf, checkWrite, WRITE_TOOLS } from "./rules.mjs";

let raw = "";
process.stdin.setEncoding("utf8");
for await (const chunk of process.stdin) raw += chunk;

try {
  const ev = JSON.parse(raw || "{}");
  if (!WRITE_TOOLS.has(ev.tool_name)) process.exit(0);

  const root = process.env.CLAUDE_PROJECT_DIR || ev.cwd || process.cwd();
  const ti = ev.tool_input ?? {};
  const path = makeRel(root, ev.cwd)(ti.file_path ?? ti.notebook_path);
  const agent = agentOf(ev);
  const broken = checkWrite(agent, path);

  if (broken) {
    process.stdout.write(
      JSON.stringify({
        hookSpecificOutput: {
          hookEventName: "PreToolUse",
          permissionDecision: "deny",
          permissionDecisionReason: `Harness rule: ${broken}. Blocked write to ${path}. Report the needed change instead of making it.`,
        },
      }),
    );
  }
} catch {
  /* fail open */
}
process.exit(0);
