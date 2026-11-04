# Code review: step 1, round 1

Step: "Data shape: order mode type and pure helper" (`plan.md`).
Inputs: `plan.md` step 1, `step-1/scaffold.md`, `step-1/tests-r1.md`, `step-1/impl-r1.md`.

## Scope

Reviewed files (all new, untracked):

- `frontend/restaurant-frontend-app/src/app/models/cart/cart-order-mode.model.ts`
- `frontend/restaurant-frontend-app/src/app/shared/helpers/group-reservation.helpers.ts`
- `frontend/restaurant-frontend-app/src/app/shared/helpers/group-reservation.helpers.spec.ts`

During the review, `git status` also showed changes in `cart.component.html` and `not-submitted-order.component.ts`. These are step 2 changes from agents that work in parallel. I did not review them. No `infra/` or `*.tf` change.

## Plan conformance

- `CartOrderMode = 'single' | 'group' | 'unknown'`: matches the contract.
- `MIN_GROUP_GUEST_COUNT = 2`, `isGroupReservation(guestCount: number): boolean` → `>= MIN_GROUP_GUEST_COUNT`: matches.
- `toCartOrderMode(guestCount: number | undefined): CartOrderMode`: explicit `=== undefined` → `'unknown'`, else it uses `isGroupReservation`: matches. The threshold is in one place.
- Spec covers all plan cases (`isGroupReservation`: 0, 1, 2, 6. `toCartOrderMode`: undefined, 0, 1, 2, 5. The constant). No missing part, no change outside the step.

## Maker/checker split

- Test-writer: changed only the spec file (report and file list agree).
- Implementer: changed only `group-reservation.helpers.ts` (stub bodies). The model file is from the scaffold. No test file changed by the implementer.
- No violation.

## Findings

### [minor] [code] src/app/shared/helpers/group-reservation.helpers.ts — file name pattern differs from the existing helpers
- Problem: the existing helpers use `<topic>-helpers.ts` (`date-helpers.ts`, `http-error-helpers.ts`). The frontend `CLAUDE.md` placement table names `date-helpers.ts`. The new file uses `group-reservation.helpers.ts`.
- Scenario: no runtime effect. A search for `*-helpers.ts` misses the new file.
- Fix: optional. The plan contract fixes this name and steps 3 and 5 import it, so a rename now costs more than it gives. Accept as is, or rename to `group-reservation-helpers.ts` in all plan steps before step 3 imports it.

No other findings. Notes (no action):

- Mutation check: `>=` → `>` fails the row 2 → true. `=== undefined` → `!guestCount` fails the row 0 → `'single'`. A constant return fails at least one row of each table. A function that returns nothing fails 9 of 10 tests.
- `@app/...` imports: frontend `CLAUDE.md` §5 says they do not resolve, but `tsconfig.json` has `paths` and both `tsc` gates pass. The plan (F8) records this deviation. Not a finding.
- The duplicate threshold `MIN_GUESTS_FOR_INVITE = 2` in `reservation-invite-modal.component.ts:20` is in untouched code and out of scope (implementer follow-up). The orchestrator can record it in `docs/tech-debt.md`.

## Check results

Run from `frontend/restaurant-frontend-app`:

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | exit 0, 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | exit 0, 0 errors |
| `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/shared/helpers/group-reservation.helpers.spec.ts` | `Executed 10 of 10 SUCCESS`, `TOTAL: 10 SUCCESS` |
| `npm test -- --watch=false --browsers=ChromeHeadless` (full suite, with the parallel step 2 edits in the tree) | `TOTAL: 46 FAILED, 220 SUCCESS` |

The 46 failures equal the pre-existing baseline in `tests-r1.md` (46 FAILED without the new spec). None of the failing tests is in the new spec (grep of the failure lines for `group`, `isGroupReservation`, `toCartOrderMode`, `MIN_GROUP`: no match). Production build not run: the step adds no import to the app graph.

## Verdict

approve with minor (1 minor, optional. 0 blocker, 0 major).
