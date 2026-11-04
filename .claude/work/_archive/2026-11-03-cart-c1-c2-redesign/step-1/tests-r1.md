# Test-writer report: step 1, round 1

## Tests written

File: `frontend/restaurant-frontend-app/src/app/shared/helpers/group-reservation.helpers.spec.ts` (10 `it()` blocks, table-driven, from the plan contract only).

- `MIN_GROUP_GUEST_COUNT is 2` — the constant is 2.
- `isGroupReservation returns false for 0 guest(s)` — the empty boundary.
- `isGroupReservation returns false for 1 guest(s)` — one below the threshold.
- `isGroupReservation returns true for 2 guest(s)` — the threshold (`>=`, not `>`).
- `isGroupReservation returns true for 6 guest(s)` — above the threshold.
- `toCartOrderMode returns 'unknown' for guestCount undefined` — no reservation data.
- `toCartOrderMode returns 'single' for guestCount 0` — 0 is a number, not `undefined` (catches a falsy check `!guestCount`).
- `toCartOrderMode returns 'single' for guestCount 1`.
- `toCartOrderMode returns 'group' for guestCount 2` — the threshold.
- `toCartOrderMode returns 'group' for guestCount 5`.

The cases cover a default return (`false`, `undefined`, a constant mode): each one fails at least one row.

## Red run

Log of actions:
1. `npx tsc -p tsconfig.spec.json --noEmit` → exit 0, 0 errors.
2. `npm test -- --watch=false` → Karma cannot start Chrome: "Missing X server or $DISPLAY", "Chrome failed 2 times (cannot start). Giving up." No tests executed. This is an environment limit (no display), not a code defect.
3. `npm test -- --watch=false --browsers=ChromeHeadless` (full suite, with the new spec) → `Executed 266 of 266`, **TOTAL: 55 FAILED, 211 SUCCESS**.
4. `npm test -- --watch=false --browsers=ChromeHeadless --include='src/app/shared/helpers/group-reservation.helpers.spec.ts'` → `Executed 10 of 10`, **TOTAL: 9 FAILED, 1 SUCCESS**.
5. Baseline (new spec moved aside temporarily, then restored) → **TOTAL: 46 FAILED, 210 SUCCESS**.
6. Diff of failing test names, baseline vs. with the spec: the only additions are the 9 new tests. No other test changes state.

Failure reason for each of the 9 failing new tests: `Error: Not implemented` (thrown at `group-reservation.helpers.ts`, the scaffold stub). The `MIN_GROUP_GUEST_COUNT is 2` test passes, as expected: the scaffold has the real constant.

## Existing tests

The 46 failures in the baseline are pre-existing and not touched by this step (for example `WaiterDashboardComponent should create`: NG0201 no provider for `ActivatedRoute`. `TimeSlotComponent should create`: reads `start` of undefined). These are the CLI stub specs and missing-provider specs listed as debt in the frontend `CLAUDE.md` §6/§8. The other helper specs in `shared/helpers/` are not in the diff. The same set of 46 fails with and without the new spec.

## Not tested

- Nothing. The step is a pure function and a type.
- Only the compiler checks the `CartOrderMode` type. The `tsc` gate passes, and the spec imports the type with `@app/...`.

## Notes for the implementer

- `isGroupReservation(guestCount)` must use `>= MIN_GROUP_GUEST_COUNT` (2 → `true`).
- `toCartOrderMode` must check `undefined` explicitly. `0` must give `'single'`, not `'unknown'`.
- Return values are the exact strings `'single'`, `'group'`, `'unknown'`.
- To run Karma in this environment, add `--browsers=ChromeHeadless`. The plain `npm test -- --watch=false` cannot start Chrome here.
