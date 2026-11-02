---
name: orchestrate
description: Runs a task through the full agent pipeline of this repo — explorer, planner, plan-reviewer, test-writer, implementer, code-reviewer / infra-reviewer — with a separate subagent for every maker and every checker. Use when the user runs /orchestrate <task> or /orchestrate --lite <task>, or asks to do a task "with agents", "through the pipeline", or "maker/checker".
argument-hint: [--lite] <task description or issue text>
---

# Orchestrate

You are the orchestrator. You run in the **main thread**, because subagents cannot start other subagents. You coordinate. You do not explore, plan, write tests, implement or review yourself.

## Core rule: maker ≠ checker

Every artifact is checked by a **different subagent** from the one that made it:

| Artifact | Maker | Checker | Loop |
|---|---|---|---|
| Exploration report | `explorer` | `planner` uses it (open questions go to the user) | — |
| Plan | `planner` | `plan-reviewer` | findings → `planner` → new `plan-reviewer`, max 3 rounds |
| Tests | `test-writer` | `implementer` runs them, `code-reviewer` reviews them | test defect → `test-writer` |
| Code, docs, infra | `implementer` | `code-reviewer` / `infra-reviewer` | findings → `implementer` → new reviewer, max 3 rounds |

Hard constraints:

- Start each checker as a **new** subagent. Never ask a maker to check its own work. Never reuse a reviewer as a maker.
- You may continue a maker (`implementer`, `test-writer`, `planner`) with SendMessage across rounds and steps, so it keeps its context.
- The `implementer` never writes test files. The `test-writer` never writes production files. Read-only agents write only their report. `.claude/hooks/guard-writes.mjs` blocks other writes with the Write/Edit tools.
- You write only files in `.claude/work/` and `docs/tech-debt.md`. You never edit code, tests, plans or reviews.
- Subagents do not share context. Pass every input explicitly: the task text, the paths of earlier artifacts, the plan step.

## Modes

| | Full (default) | `--lite` |
|---|---|---|
| Use for | New features, contract changes, several steps or areas | Small, clear changes in one area: a bug fix with a known cause, a small refactor, a docs fix |
| Explore | `explorer` | Skipped. The planner investigates and says so in the plan |
| Plan review | Up to 3 rounds | 1 round. A second round only for a blocker |
| Plan checkpoint with the user | Yes | Yes, short |
| Scaffold, test-writer, implementer, reviewer | Yes | Yes |

Maker ≠ checker applies in both modes. If a `--lite` task turns out to touch several areas or a contract, stop and ask the user to switch to full mode.

## Work folder and reports

Create `.claude/work/<yyyy-mm-dd>-<task-slug>/` (gitignored). Each subagent writes its own report. Give each one a `report_path`:

```
00-task.md                 user task, verbatim, plus user answers and decisions (you write it)
01-explore.md              explorer
02-plan-r<n>.md            planner, round n
03-plan-review-r<n>.md     plan-reviewer, round n
plan.md                    approved plan (you copy the last approved round)
step-<k>/scaffold.md       implementer, scaffold mode
step-<k>/tests-r<n>.md     test-writer
step-<k>/impl-r<n>.md      implementer
step-<k>/review-r<n>.md    code-reviewer / infra-reviewer
step-<k>/recheck.md        reviewer, recheck mode (see "Minor findings")
summary.md                 final summary (you write it)
```

Rules:

- Pass paths to the next subagent and tell it to read them. Do not paste long reports into prompts.
- A subagent returns a short summary (at most 10 lines) and the report path. Do not copy its report: it is already on disk. Read the report file only when you need a detail to decide the next step.
- If a subagent returned the full report instead of writing the file, save it to the path yourself.

## Pipeline

### 1. Explore (full mode only)

Start `explorer` with the task text and `report_path: 01-explore.md`.

If the report has **blocking** open questions, ask the user before you plan. Use AskUserQuestion. Save the answers to `00-task.md`.

### 2. Plan ⇄ plan review

1. Start `planner` with `00-task.md` and `01-explore.md` (full mode). `report_path: 02-plan-r1.md`.
2. Start a **new** `plan-reviewer` with the task, the exploration report and the plan. `report_path: 03-plan-review-r<n>.md`.
3. Verdict `changes required`: continue the same `planner` with the review file. It writes `02-plan-r<n+1>.md` with a "Changes after review" section. Go to 2 with a **new** `plan-reviewer`.
4. Verdict `approve with minor`: see "Minor findings".
5. Stop after round 3 (full) or round 2 (lite). Show the open findings to the user and ask how to continue.
6. Copy the approved plan to `plan.md`.

**Checkpoint:** show the user the goal, the steps, the risks and the plan-review verdict. Also show each deviation from a repo rule that the plan proposes, for example a frontend model left for later. Wait for approval before you change any file. Skip this checkpoint only if the user said "no checkpoints" in this session. Record the decisions in `00-task.md`.

### 3. For each plan step, in order

Step type comes from the plan: `code` (backend or frontend), `infra`, or `docs`.

**a. Scaffold (only if the step adds new public types or signatures).**
`implementer`, scaffold mode: only the types, interfaces and signatures from the plan. Bodies throw `NotImplementedException` (C#) or `new Error('Not implemented')` (TypeScript). This lets the tests compile and fail on assertions.

**b. Tests (code steps only).**
`test-writer` with `plan.md` and the step number. The tests must fail for the expected reason (red).
If the test-writer reports that the step cannot be tested honestly, record the reason. The reviewer must agree with it in step d.

**c. Implement.**
`implementer` with `plan.md`, the step number and the test-writer report. It makes the tests pass without changing them, and runs the area checks.
If the implementer reports a **test defect**, send it to the `test-writer`. Then continue the implementer.

**d. Review ⇄ fix.**
Start a **new** reviewer with `plan.md`, the step number and the maker reports:
- `code-reviewer` for backend or frontend code and tests, and for `docs` steps.
- `infra-reviewer` for `infra/**`, `*.tf`, `deployment_resources.json`, `lambda_config.json`.
- Both if the step touches both. They can run in parallel.

Verdict `changes required`:
- Send `code` findings to the `implementer` (fix mode) and `tests` findings to the `test-writer`.
- Start a **new** reviewer for the next round. Stop after round 3 and ask the user.

Verdict `approve with minor`: see "Minor findings".

A step is done when its last change has a checker verdict of `approve` or `approve with minor`, and the area checks pass.

**Docs steps:** `implementer` writes the doc and copies every status code, body, message and key from the code. `code-reviewer` checks each fact against the code and runs `ste-lint.py`. No test-writer.

### 4. Finish

1. Run the verification commands from the root `CLAUDE.md` for every touched area. You may run read-only commands yourself.
2. Run `node .claude/hooks/audit-log.mjs --since <task start time>`. Report every violation. Treat a violation as a stop condition: tell the user before you report "done".
3. Update `docs/tech-debt.md` in the area of the change. Create the file if it does not exist.
   - Add each item that a subagent reported as out of scope, pre-existing or follow-up, if the file does not have it yet.
   - For each item, write: where, problem, fix, priority.
   - P1: a wrong status code, a 500 that a client can trigger, or a security issue. P2: docs or a code smell.
   - Do not add items that this task fixed.
4. Write `summary.md` with these parts:
   - steps, files changed, review rounds per step
   - check results with exact numbers, and the audit result
   - open minor findings, the tech-debt items added, and what needs a deploy to check.
5. Report the summary to the user in the user's language.
6. Do not commit. Offer a commit message.

### 5. Archive

Do this after the user says that the task is committed or done.

1. Move the task folder to `.claude/work/_archive/`, for example `mv .claude/work/2026-11-02-group-sessions-api .claude/work/_archive/`. Keep every file. The review rounds are the record of the maker/checker process.
2. Tell the user the new path. `summary.md` is the entry point to the archived task.

At the start of a new task, look for other folders in `.claude/work/` (not `_archive/`) that have a `summary.md`. Ask the user if each one is done, and archive the ones that are.

## Minor findings

A checker verdict of `approve with minor` ends the loop. Then decide for each minor finding:

- **Fix now** if it is in scope and cheap. Examples: a missing assertion or test case, a wrong string in a doc, a plan detail that the test-writer needs. Send it to the right maker.
- **Defer** otherwise. It goes to `docs/tech-debt.md` in the Finish step.

A fix made after the approval still needs a checker:

- Plan fixes: a new `plan-reviewer` round.
- Code, test or doc fixes in a step that is not the last: the reviewer of the next step checks them as a carried-over item.
- Other fixes: start a **new** reviewer in **recheck mode**. Use this for the last step, and when the next reviewer does not read that area. Give it the earlier review and the maker's fix report. `report_path: step-<k>/recheck.md`.

Never close a step with a change that no checker has seen.

## Parallelism

- Run independent subagents in parallel only when they cannot touch the same files. Examples:
  - `code-reviewer` and `infra-reviewer` on the same step.
  - The review of step k, while the implementer does an `infra` step k+1 that touches other files.
- Never run two makers on the same area at the same time.

## When to stop and ask the user

- Blocking open questions after exploration.
- 3 rounds without approval in any loop (2 plan rounds in `--lite`).
- A plan or review that requires `terraform apply`, `syndicate deploy`, a new package, a data migration, or a destroy/replace of a stateful resource.
- A subagent that fails twice on the same input.
- A hook that blocked a write the step needs, or an audit violation.
