# Green & Tasty Restaurants — monorepo

Restaurant booking app: Angular frontend, .NET Lambda backend, shared AWS infrastructure in Terraform. One repository, three deployment tracks.

| Area | Path | Stack | Area guide |
|---|---|---|---|
| Frontend | `frontend/restaurant-frontend-app` | Angular 22 SPA, SCSS, Karma + Jasmine | `frontend/restaurant-frontend-app/CLAUDE.md` |
| Backend | `backend/restaurant-backend-app` | .NET 10 AWS Lambda, DynamoDB, Cognito, deployed with AWS Syndicate | `backend/restaurant-backend-app/CLAUDE.md` |
| Infra | `infra/terraform` | Terraform ≥ 1.10, AWS provider ~> 5.0, S3 state backend | `infra/terraform/CLAUDE.md` |

Deployment order and prerequisites: `README.md`. Deploy order is Terraform → Syndicate backend → frontend to S3.

## Language

Write all repository files in English: code, comments, Markdown, commit messages, PR descriptions. Chat with the user can be in any language.

## Precedence

When two instructions conflict, the higher one wins:

1. The user's request in the current session.
2. The nearest area `CLAUDE.md`.
3. `.claude/rules/<area>.md`.
4. Skills in `.claude/skills/`. They are adapted from upstream sources (see `.claude/skills/VENDORED.md`), but generic advice in their `references/` folders can still disagree with the code.
5. The existing code. If a rule describes a target state that the code does not reach yet, follow the code and say so.

## Harness

Agents are split by **role**. Area knowledge comes from the area `CLAUDE.md`, the path-scoped rules and the skills.

**Maker ≠ checker.** A different subagent checks every artifact: plan, tests, code. The agent that makes an artifact never reviews it.

| Agent | Role | Writes |
|---|---|---|
| `orchestrator` | Runs the pipeline. Main thread only: `claude --agent orchestrator`, or `/orchestrate [--lite] <task>` in a normal session | `.claude/work/`, `docs/tech-debt.md` |
| `explorer` | Collects facts about the task before planning | its report only |
| `planner` | Turns the task and the exploration report into steps, contracts and test cases. Revises after review | its report only |
| `plan-reviewer` | Checks the plan against the task and the code. Returns findings to the planner | its report only |
| `test-writer` | Writes failing tests from the plan contract, before the code | test files, its report |
| `implementer` | Makes the tests pass. Scaffold / implement / fix modes | production code and docs, its report (never tests) |
| `code-reviewer` | Reviews backend and frontend code **and** tests, and docs. Returns findings to the implementer / test-writer. Recheck mode for fixes after approval | its report only |
| `infra-reviewer` | Reviews Terraform and Syndicate resource changes for security, data loss, cost and drift | its report only |

The "Writes" column is enforced: `.claude/hooks/guard-writes.mjs` blocks Write/Edit calls outside each agent's zone (zones are defined in `.claude/hooks/rules.mjs`). Bash writes are not blocked, but the audit reports them.

Pipeline (details in `.claude/skills/orchestrate/SKILL.md`):

```
explorer → planner ⇄ plan-reviewer (≤3 rounds) → user approves plan
  → for each step: [implementer: scaffold] → test-writer (red) → implementer (green)
                   ⇄ code-reviewer / infra-reviewer (≤3 rounds) [→ recheck of minor fixes]
  → verification → audit log → docs/tech-debt.md → summary → user commits

--lite: no explorer, 1 plan-review round; maker ≠ checker stays.
```

| Skill | Use for |
|---|---|
| `orchestrate` | Running a task through the full agent pipeline |
| `ub-mode` | Every code change: Make it Work → Make it Right → Practice |
| `dotnet-best-practices`, `csharp-async`, `csharp-xunit` | Backend C# code and tests |
| `angular-developer` | Frontend Angular code and tests |
| `asd-ste100` | Any Markdown file, agent/skill text, error messages |

Rules in `.claude/rules/` load automatically by path and name the skills for each area.

For a small change without the pipeline, the main session can do the work itself. It still asks a separate `code-reviewer` or `infra-reviewer` to check the result.

## Cross-area contracts

- The API contract is `backend/restaurant-backend-app/docs/ApiRoutes.md` and `dnapp/lambdas/api-handler/Routing/RouteRegistry.cs`. When a frontend model and the API disagree, the backend wins. A change to a route updates `ApiRoutes.md` and the frontend model in the same task. Exception: the user can approve a separate frontend task, for example at the plan checkpoint. Then record the frontend follow-up in `docs/tech-debt.md`.
- Infra and backend share names: Terraform `infra/terraform/locals.tf` derives Lambda names and ARNs from Syndicate configs. Rename a Lambda only in both places.
- The frontend production build needs `BACKEND_URL` (API Gateway stage URL).

## Safety

- Do not run `terraform apply`, `terraform destroy`, `syndicate deploy`, `syndicate clean` or `aws s3 sync` unless the user asks in this session. `.claude/settings.json` asks for confirmation on them.
- Never commit `*.tfstate`, `*.tfvars`, `.env`, credentials, `node_modules/`, `dist/`, `bin/`, `obj/`, `syndicate_venv/`. `.gitignore` covers them. Check `git status` before a commit anyway.
- Do not create commits or pushes unless the user asks.
- This is a single-developer learning project. Work and commits happen on `main`. Do not create branches unless the user asks.

## Verification commands

| Area | Commands | Baseline |
|---|---|---|
| Backend | `dotnet build restaurant-backend-app.sln --no-incremental`, `dotnet test Tests/APIHandler.Tests.csproj` | all tests pass, 0 failed |
| Frontend | the four gates in `frontend/restaurant-frontend-app/CLAUDE.md` §6 | see that file §8 |
| Infra | `terraform fmt -check -recursive`, `terraform validate`, `terraform plan` (read-only, needs AWS credentials) | fmt and validate pass |
| Docs | `python .claude/skills/asd-ste100/scripts/ste-lint.py <file.md>` | — |

Report exact numbers from these commands. Do not report impressions.

## Check rules

- **Warnings.** Compare warnings only from builds with `--no-incremental`. An incremental build prints a different total. Report the warnings in the files that you changed (target: 0 new). Do not compare totals across runs: new NuGet advisories change them without a code change.
- **A test fails that the change does not touch.** Run the suite once more. If the test passes now, it is flaky: name it in the report and add it to `docs/tech-debt.md`. If it fails again, treat it as a real failure.
- **Test totals.** Report the totals from the run. Do not compare with a fixed number in a doc.

## Agent action log

Hooks in `.claude/settings.json` log every tool call to `.agent-log/actions.jsonl` (gitignored), with the subagent type in the `agent` field.

- `node .claude/hooks/audit-log.mjs` prints calls per agent, rule violations and warnings. Options: `--since <ISO time>`, `--session <id>`, `--json`. Exit code 1 if it finds a violation.
- The orchestrator runs the audit in its Finish step.
