---
name: ub-mode
description: >
  Engineering discipline for writing or changing code in this repo. Every change
  follows First Make it Work, then Make it Right, then Follow Practice.
  Use when you implement, fix, or refactor backend or frontend code, or when the user runs /ub-mode.
---

<!--
Source: https://github.com/unclebob/ub-mode (commit 57a8bdd28e45), by Robert C. Martin.
Adapted for marathon2026. Changes from upstream:
- "Crapper", "Dryer", "Mutator": the upstream tools are not installed here. Each section now names
  the tool that this repo has, and keeps the upstream threshold as a review target.
- "Commit and Push": replaced. Upstream says "push to the main line". This repo forbids direct
  commits to main, and agents commit only when the user asks.
- "TDD": added the repo test commands and the backend limit (no local Lambda host).
Everything else is unchanged.
-->

# ub-mode

Every time you are asked to make a change follow:
* First Make it Work.
* Then Make it Right.
* Follow Practice

The area rules win over this skill when they conflict: the nearest `CLAUDE.md` and `.claude/rules/<area>.md`.

# First Make it Work

## Laziness Protocol

* One decision lives in one place.
* Ship the smallest edit that solves the problem.

## Foundational Thinking

* Get the data shape right before you write the logic.
* Do first whatever helps every later phase.
* If a concurrent writer would matter, do not share state.

## TDD

* Write the failing test before you change the code.
* See it fail for the reason you expect.
* Make the smallest change that makes it pass.
* If an honest test is impractical, do not write a bad one.

Test commands in this repo:

| Area | Command (run from the project folder) |
|---|---|
| Backend | `dotnet test Tests/APIHandler.Tests.csproj` in `backend/restaurant-backend-app` |
| Frontend | `npm test -- --watch=false` in `frontend/restaurant-frontend-app` |
| Infra | `terraform fmt -check -recursive` and `terraform validate` in `infra/terraform` |

The backend has no local host. Unit tests cover services with mocked repositories. Handler and DynamoDB behaviour needs a real `syndicate deploy`, so say so instead of claiming it works.

## Redesign From First Principles

* Do not bolt a new requirement onto the old design.
* Ask what you would have built if it had been there from the start, think that through, and deliver it in pieces.

## Attack the Premise

* When two fixes that share one premise fail the same gate, write that premise in one sentence and count which actors hold the imbalance.
* Remove the assignment.
* Do not start another fix until you have done that.

## Subtract Before You Add

* Take complexity out before you add anything.

## Outcome-Oriented Execution

* Aim at the end state you can check.
* A break in the middle is allowed when you planned it and can undo it.

## Build the Lever

* If the work is not obvious at a glance, build the tool that does it.
* Do one unit by hand, then make the tool match that unit.

## Architecture

### Model the Domain

* Put the domain in a structure that makes the illegal states impossible.
* Maintain a glossary of domain terms.
* If the code is already clear and local, leave it.

### Boundary Discipline

* Check the data where it enters.
* Inside, trust it.
* Keep the business logic in a pure function.

### Type System Discipline

* Make the illegal value impossible to write.
* Parse outside data once, where it comes in.
* Do not silence the compiler.

### Make Operations Idempotent

* Running it twice, or running it after a crash halfway through, ends in the same state.

### Separate Before Serializing Shared State

* If they do not need the same mutable thing, give each its own.
* If they do, allow one writer, and enforce that in the structure.

# Then Make it Right

## Fix Root Causes

* Reproduce it.
* Fix the cause.
* A guard that only hides the crash is not a fix.

## Sequence work into verifiable units

* Finish one small unit and check it.
* Then start the next.

## Test Behavior, Not Implementation

* The test calls the code the way its user does and checks the result that user would see.
* If the test would still pass when every function returned nothing, throw it out.

## Complexity (upstream: Crapper)

* Target: every function you touch stays under CRAP 10. CRAP grows with cyclomatic complexity and falls with test coverage.
* Reduce a complex function by splitting it, by testing it, or both.
* Consider whether case statements are worth reducing.
* Coverage tools in this repo:
  * Backend: `dotnet test Tests/APIHandler.Tests.csproj --collect:"XPlat Code Coverage"` (coverlet is already referenced).
  * Frontend: `npm test -- --watch=false --code-coverage`.

## Duplication (upstream: Dryer)

* Eliminate duplication that is at least 80% similar in structure.
* No duplication tool is installed. Search for the pattern with Grep before you write a new helper.

## Mutation (upstream: Mutator)

* Cover the uncovered.
* Kill the survivors: for each test you write, ask which one-line change in the code would still pass it, and close that gap.
* Delete dead code.
* No mutation tool is installed. Do not add one (for example Stryker) without the user's approval.

## Refactoring

* Pin what the code does.
* Change the structure.
* Prove the behavior did not change.

# Practice

## Keep a Log

* Keep a log of every action you take and the reason you took it.
* Report that log upon completion.

## Guard the Context Window

* Keep the main thread small.
* Send the bulk elsewhere, and keep the summary.

## Worktree Cleanup

* Find what is safe to remove, confirm nothing still in use needs it, then reclaim the disk.
* Uncommitted work waits for a decision.

## Commit and Push

* Commit only when the user asks.
* Never commit to `main` directly. Use `feature/<short-description>`, `bugfix/<short-description>` or `fix/<short-description>`.
* Commit often, in small ordered commits. Keep one logical change per commit.
* Each commit says why, what changed, and how you proved it.
* Push only when the user asks.
