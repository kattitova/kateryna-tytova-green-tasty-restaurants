# Implementer report: step 1, implement, round 1

## 1. Mode and step

Mode: implement. Step 1: "Data shape: order mode type and pure helper".

## 2. Log

1. Read `step-1/tests-r1.md` and `group-reservation.helpers.spec.ts`. The spec matches the plan contract (10 cases), so there is no test defect.
2. Red check: `npm test -- --watch=false --browsers=ChromeHeadless --include='src/app/shared/helpers/group-reservation.helpers.spec.ts'` gave `Executed 10 of 10`, `TOTAL: 9 FAILED, 1 SUCCESS`. The 9 failures are `Error: Not implemented` from the scaffold stubs, which is what the test-writer report says.
3. Replaced the two stub bodies with the smallest correct code:
   - `isGroupReservation`: `return guestCount >= MIN_GROUP_GUEST_COUNT;`
   - `toCartOrderMode`: an explicit `guestCount === undefined` check returns `'unknown'`. Otherwise it returns `isGroupReservation(guestCount) ? 'group' : 'single'`. The explicit check means `0` gives `'single'`. A falsy check would give `'unknown'`.
   - The threshold lives only in `MIN_GROUP_GUEST_COUNT`. `toCartOrderMode` reuses `isGroupReservation`, so the comparison is in one place.
4. Green check: the same targeted run gave `TOTAL: 10 SUCCESS`.
5. Ran both `tsc` gates and the full Karma suite (results below).
6. Compared the full run with the test-writer's baseline: 46 failures, the same count as the baseline (`46 FAILED, 210 SUCCESS` without the new spec). None of the 46 failing names is a test from the new spec. SUCCESS went from 210 to 220, which equals the 10 new tests. There are no new failures.
7. Refactor: nothing more to do. Both functions have a cyclomatic complexity of 1 or 2 and 100% of their branches are covered.

## 3. Files changed

- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/shared/helpers/group-reservation.helpers.ts`: the stub bodies are now implemented.
- `cart-order-mode.model.ts`: no change since scaffold.

## 4. Check results

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | exit 0, 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | exit 0, 0 errors |
| Helper spec only (`--include`) | `Executed 10 of 10`, `TOTAL: 10 SUCCESS` |
| Full suite `npm test -- --watch=false --browsers=ChromeHeadless` | `Executed 266 of 266 (46 FAILED)`, `TOTAL: 46 FAILED, 220 SUCCESS`, exit 1 |

The 46 failures are pre-existing (baseline in `tests-r1.md`: 46 FAILED, 210 SUCCESS). None of them is in the new spec. Examples: `AppComponent should render title`, `DishModalComponent should create`, `GeneralInformationComponent ...`. These tests are stub specs or specs with missing providers. This step does not touch them.

Not run in this step: the production build. The step adds no import to the app graph, so the bundle cannot change.

## 5. Fix mode

Not applicable.

## 6. Test defects

None.

## 7. Not checked locally

Nothing. This step contains only a pure function and a type.

## 8. Follow-ups (not done)

- `reservation-invite-modal.component.ts:20` has its own copy of the threshold (`MIN_GUESTS_FOR_INVITE = 2`). It could use `MIN_GROUP_GUEST_COUNT`, but the plan lists this as out of scope.
- 46 Karma tests fail on `main`. This is pre-existing debt, not caused by this task.
