// Shared harness rules for guard-writes.mjs (blocks) and audit-log.mjs (reports).
// One place defines which agent may write which files. Keep it in sync with the root CLAUDE.md "Harness" table.

// Normalize Windows (`D:\x`), POSIX (`/d/x`, Git Bash) and mixed paths.
export const norm = (p) =>
  String(p)
    .replace(/\\/g, "/")
    .replace(/^\/([a-zA-Z])\//, (_, d) => `${d.toUpperCase()}:/`)
    .replace(/^([a-zA-Z]):\//, (_, d) => `${d.toUpperCase()}:/`)
    .replace(/\/$/, "");

// Returns a function that turns an absolute path into a repo-relative path.
export const makeRel = (...rootDirs) => {
  const roots = rootDirs.filter(Boolean).map((r) => norm(r) + "/");
  return (p) => {
    if (typeof p !== "string") return undefined;
    const n = norm(p);
    const hit = roots.find((r) => n.toLowerCase().startsWith(r.toLowerCase()));
    return hit ? n.slice(hit.length) : n;
  };
};

// Subagent type from hook input. Absent for the main thread.
export const agentOf = (ev) => ev.agent_type || undefined;

const isWork = (p) => p.startsWith(".claude/work/");
const isTest = (p) =>
  p.startsWith("backend/restaurant-backend-app/Tests/") || p.endsWith(".spec.ts");
const isTechDebt = (p) => /(^|\/)docs\/tech-debt\.md$/.test(p);

// Write zones per agent. `undefined` = no restriction (main thread or an agent not listed here).
const zones = {
  explorer: { allow: isWork, rule: "read-only agent: writes only its report in .claude/work/" },
  planner: { allow: isWork, rule: "read-only agent: writes only its report in .claude/work/" },
  "plan-reviewer": { allow: isWork, rule: "checker: writes only its report in .claude/work/" },
  "code-reviewer": { allow: isWork, rule: "checker: writes only its report in .claude/work/" },
  "infra-reviewer": { allow: isWork, rule: "checker: writes only its report in .claude/work/" },
  "test-writer": {
    allow: (p) => isWork(p) || isTest(p),
    rule: "test-writer: writes only test files (Tests/**, *.spec.ts) and its report",
  },
  implementer: {
    allow: (p) => !isTest(p),
    rule: "implementer: never writes test files (Tests/**, *.spec.ts)",
  },
  orchestrator: {
    allow: (p) => isWork(p) || isTechDebt(p),
    rule: "orchestrator: writes only .claude/work/ and docs/tech-debt.md",
  },
};

// Returns null when the write is allowed, or the broken rule as text.
export const checkWrite = (agent, relPath) => {
  const z = agent && zones[agent];
  if (!z || !relPath) return null;
  return z.allow(relPath) ? null : z.rule;
};

export const WRITE_TOOLS = new Set(["Write", "Edit", "MultiEdit", "NotebookEdit"]);

// Bash commands that change files or state. Heuristic: audit flags them, the guard does not block them.
export const MUTATING_CMD =
  /(\bsed\s+-i\b|\btee\b|(^|[^>&|])>{1,2}\s*[^&\s]|\bmv\s|\bcp\s|\brm\s|\bmkdir\b|\btouch\b|\bdotnet\s+(add|remove)\b|\bnpm\s+(i|install|uninstall)\b|\bgit\s+(commit|push|checkout|reset|rebase|merge|stash|add)\b)/;

// Commands that need the user's explicit request (root CLAUDE.md "Safety").
export const RESTRICTED_CMD =
  /\b(terraform\s+(apply|destroy|import|state|taint)|syndicate\s+(deploy|update|clean)|aws\s+s3\s+(sync|rm)|git\s+(commit|push))\b/;

export const READ_ONLY_AGENTS = new Set([
  "explorer",
  "planner",
  "plan-reviewer",
  "code-reviewer",
  "infra-reviewer",
]);
