# Summary: Group Pre-Order modal (owner)

Mode: full. Started 2026-11-04T11:50:21Z, base `fa6b784`. All work on `main`, one commit per approved step (user request). Subagents ran on sonnet (user budget decision).

## Steps, commits, review rounds

| Step | Content | Commit | Review |
|---|---|---|---|
| Plan | explorer → plan r1 (changes required) → r2 (approve with minor) | — | 2 rounds |
| BE-1 | bonus calculator, owner own items | `a5ac82d` | approve |
| BE-2 | timestamps, status `orderedCount` + `bonus`, owner in sessions | `06a853d` | approve with minor |
| BE-3 | group submit with any pre-order, owner order created | `4b5141f` | approve |
| BE-4 | ApiRoutes.md, ADR 0002, FE tech debt | `de6313a` | approve |
| FE-1 | interceptor token for `/guest/groups/` | `e43496e` | approve |
| FE-2 | models, `GroupOrdersService`, mock | `212fdad` | approve with minor |
| FE-3 | view helpers | `2449dc6` | approve |
| FE-4 | card: trash icon, Group Pre-Order button, small buttons | `0de93ca` | approve with minor (icon position fixed in FE-5) |
| FE-5 | modal read-only + page wiring | `03e5c9d` | approve with minor (2 fixed in FE-6) |
| FE-6 | copy link, remove guest, submit + dialogs | `69e3c85` | approve with minor |
| FE-7 | owner dish editing, Add my dishes | `acef739` | approve with minor |

All steps: one review round.

## Checks (orchestrator, final state on main)

- Frontend: `tsc` app 0, spec 0; build "Bundle check passed", initial 846.44 kB raw / 179.41 kB transfer (start of task ~806 kB); Karma (ChromeHeadless) 397 SUCCESS, 0 failed (baseline 364).
- Backend: `dotnet build --no-incremental` 0 errors, 97 warnings (baseline 97); `dotnet test` 361 passed, 0 failed (baseline 337).
- Style budget: `group-pre-order-modal.component.scss` 5.52 kB (warning at 4 kB).

## Audit

`audit-log.mjs --since 2026-11-04T11:50:21Z`: 10 violations, all "restricted command (needs an explicit user request)" = the per-step `git commit` / `git push` to `main`. The user asked for a commit and push after each approved step at the plan checkpoint.

## Decisions and deviations

D1–D6 and Q1–Q6 in `00-task.md`. No seats stepper, no "Add to calendar", no reminder button (the copy link is the reminder), no dot on the card button. Discount is display only. M04 text says the seat becomes free. Prices use `$100.00`.

## Tech debt added

- Frontend `docs/tech-debt.md` section 3 (3.1–3.6, all P2).
- Backend `docs/tech-debt.md` 4.1 marked fixed; section 5 (5.1–5.5, all P2).

## Check after a frontend deploy (backend already deployed by the user)

1. Owner calls to `/guest/groups/{id}/sessions`, DELETE session and submit return 200, not 401.
2. Guest flow `/join/{token}` still works.
3. Status returns `orderedCount`, `bonus`; `deadline` ends with `Z`.
4. Group submit: owner-only, guests-only, nothing to submit (400).
5. Card layout at 1440 px and on mobile; modal on a 12-seat reservation (slider).
