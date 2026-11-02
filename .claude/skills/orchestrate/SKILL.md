---
name: orchestrate
description: Runs a task through the full agent pipeline of this repo — explorer, planner, plan-reviewer, test-writer, implementer, code-reviewer / infra-reviewer — with a separate subagent for every maker and every checker. Use when the user runs /orchestrate <task>, or asks to do a task "with agents", "through the pipeline", or "maker/checker".
argument-hint: <task description or issue text>
---

# Orchestrate

You are the orchestrator. You run in the **main thread**, because subagents cannot start other subagents. You coordinate. You do not explore, plan, write tests, implement or review yourself.

## Core rule: maker ≠ checker

Every artifact is checked by a **different subagent** from the one that made it:

| Artifact | Maker | Checker | Loop |
|---|---|---|---|
| Exploration report | `explorer` | `planner` uses it (open questions go to the user) | — |
| Plan | `planner` | `plan-reviewer` | findings → `planner` → `plan-reviewer`, max 3 rounds |
| Tests | `test-writer` | `implementer` runs them, `code-reviewer` reviews them | test defect → `test-writer` |
| Code | `implementer` | `code-reviewer` / `infra-reviewer` | findings → `implementer` → reviewer, max 3 rounds |

Hard constraints:

- Start each checker as a **new** subagent. Never ask a maker to check its own work. Never reuse a reviewer as a maker.
- The `implementer` never writes or edits test files. The `test-writer` never edits production code.
- You never edit code, tests, plans or reviews. You write only the work files in `.claude/work/`.
- Subagents do not share context. Pass every input explicitly: the task text, the paths of earlier artifacts, the plan step.

## Work folder

Create `.claude/work/<yyyy-mm-dd>-<task-slug>/` (gitignored). Save each subagent result there **verbatim**, as soon as it returns:

```
00-task.md           user task, verbatim
01-explore.md        explorer report
02-plan-r1.md        planner output, round 1
03-plan-review-r1.md plan-reviewer output, round 1
02-plan-r2.md …      next rounds
plan.md              approved plan (copy of the last approved round)
step-<n>/tests.md            test-writer report
step-<n>/impl-r1.md          implementer report
step-<n>/review-r1.md        reviewer report
summary.md           final summary
```

Pass these paths to the next subagent, and tell it to read them. Do not paste long reports into prompts.

## Pipeline

### 1. Explore

Start `explorer` with the task text. Save `01-explore.md`.

If the report has **blocking** open questions, ask the user before you plan. Use AskUserQuestion. Save the answers to `00-task.md`.

### 2. Plan ⇄ plan review

1. Start `planner` with: `00-task.md`, `01-explore.md`. Save `02-plan-r1.md`.
2. Start a **new** `plan-reviewer` with: `00-task.md`, `01-explore.md`, the plan file. Save `03-plan-review-r<n>.md`.
3. If the verdict is `changes required`: continue the same `planner` (SendMessage) with the review file. If you cannot continue it, start a new `planner` with the task, the exploration report, the last plan and the review. The planner returns a revised plan with a "Changes after review" section. Go to 2 with a **new** `plan-reviewer`.
4. Stop the loop when the verdict is `approve` or `approve with minor`, or after round 3. After round 3, show the open findings to the user and ask how to continue.
5. Copy the approved plan to `plan.md`.

**Checkpoint:** show the user the plan summary (goal, steps, risks) and the plan-review verdict. Wait for approval before you change any file. Skip this checkpoint only if the user said "no checkpoints" in this session.

### 3. For each plan step, in order

Step type comes from the plan: `code` (backend or frontend), `infra`, or `docs`.

**a. Scaffold (only if the step adds new public types or signatures).**
Start `implementer` in **scaffold mode**: create only the types, interfaces and method signatures from the plan. Bodies throw `NotImplementedException` (C#) or `new Error('Not implemented')` (TypeScript). This lets the tests compile and fail on assertions. Save `step-<n>/scaffold.md`.

**b. Tests (code steps only).**
Start `test-writer` with `plan.md` and the step number. It writes the tests and runs them. They must fail for the expected reason (red). Save `step-<n>/tests.md`.
If the test-writer reports that the step cannot be tested honestly, record the reason. The reviewer must agree with it in step d.

**c. Implement.**
Start `implementer` with `plan.md`, the step number and `step-<n>/tests.md`. It makes the tests pass without changing them, and runs the area checks. Save `step-<n>/impl-r1.md`.
If the implementer reports a **test defect** (the test contradicts the plan or the contract), start `test-writer` again with that report. Then continue the implementer.

**d. Review ⇄ fix.**
Start a **new** reviewer:
- `code-reviewer` for backend or frontend changes. It reviews the code and the tests.
- `infra-reviewer` for `infra/**`, `*.tf`, `deployment_resources.json`, `lambda_config.json`.
- Both if the step touches both.
Give it `plan.md`, the step number, `step-<n>/tests.md` and `step-<n>/impl-r<n>.md`. Save `step-<n>/review-r<n>.md`.

If the verdict is `changes required`:
- Send code findings to the **same** `implementer` (SendMessage), or start a new one with the review file.
- Send test findings to `test-writer`, not to the implementer.
- Start a **new** reviewer for the next round.
- Stop after round 3 and ask the user.

A step is done when the reviewer verdict is `approve` or `approve with minor`, and the area checks pass.

**Docs steps:** `implementer` writes the doc. `code-reviewer` checks facts and runs `ste-lint.py`. No test-writer.

### 4. Finish

1. Run the verification commands from the root `CLAUDE.md` for every touched area. You may run read-only commands yourself.
2. Write `summary.md`: steps, files changed, check results with exact numbers, review rounds per step, open minor findings, what needs a deploy to check.
3. Report the summary to the user in the user's language.
4. Do not commit. Offer a branch name and a commit message.

## Parallelism

- Run independent subagents in parallel only when they cannot touch the same files. Example: `code-reviewer` and `infra-reviewer` on the same step.
- Never run two makers on the same area at the same time.

## When to stop and ask the user

- Blocking open questions after exploration.
- 3 rounds without approval in any loop.
- A plan or review that requires `terraform apply`, `syndicate deploy`, a new package, a data migration, or a destroy/replace of a stateful resource.
- A subagent that fails twice on the same input.
