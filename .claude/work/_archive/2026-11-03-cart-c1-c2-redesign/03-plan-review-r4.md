# Plan review r4 (delta): Cart redesign, screens C1 and C2

Reviewed: `02-plan-r4.md` against `02-plan-r3.md` (full `diff`), `03-plan-review-r3.md`, and the code: `src/app/components/cart/cart.component.html:43-74`, `package.json` scripts, `scripts/check-bundle.mjs:35`, `frontend/restaurant-frontend-app/CLAUDE.md` §6.

Scope: the 3 r3 findings, and no other change.

## Verdict
approve

## Findings
None.

## Earlier findings (r3)
- [major] Step 2: required `orderMode` with no parent binding — fixed. The step 2 scaffold now adds `[orderMode]="'single'"` to `<app-not-submitted-order>` (`cart.component.html:44-50` added to the step 2 files). Under strict templates the string literal is contextually typed and is assignable to `CartOrderMode`, so there is no NG8008 and no type error. `'single'` keeps today's Submit behaviour, so the plan correctly says that the cart spec does not change in step 2. Step 3 states that `[orderMode]="view.mode"` replaces the interim binding. Step 5 applies the same rule: the scaffold adds the required input, `submittedOrderViews` and `[orderMode]="view.mode"` on `app-submitted-order` (`cart.component.html:68-72`) in one run. The checks of steps 2, 3, 4 and 5 now include `BACKEND_URL=https://example.com npm run build` (ends with "Bundle check passed", `scripts/check-bundle.mjs:35`, postbuild in `package.json:20`) and `npm test -- --watch=false`. The new F-line and the "Build order (r4)" risk record why `tsc --noEmit` is not enough.
- [minor] Step 5: no cart test for `'unknown'` in `editOrder` — fixed. Two tests: reservations load fails, and `reservationId` not in the list. Both assert that `unsubmitPreOrderByReservationId` is not called. The guard text now names `'group'` and `'unknown'`.
- [minor] Step 5: existing "should emit edited" test depends on `beforeEach` mode — fixed. The plan states the dependency and tells the test-writer to keep the mode setup in `beforeEach`.

## Diff check (r3 → r4)
Other changes, all acceptable:
- Header delta list replaced with the r4 delta.
- Summary sentence split in two (wording only).
- F7 and "Baseline": wording about `node_modules` (it is now installed). No change to the gates.
- Step 2: the new `saved` output note "The cart does not bind it until step 3" (fact, no behaviour change).
- Step 5 test lists reformatted as bullet lists; the content is the same plus the two r4 tests.

## Confirmed
- `app-not-submitted-order` has one user, `cart.component.html:44`; `app-submitted-order` has one user, `cart.component.html:68`. Both bind only `[order]` today.
- `npm run build` runs `prebuild` (`set-env.mjs`, needs `BACKEND_URL`) and `postbuild` (`check-bundle.mjs`), which prints "Bundle check passed".
- Steps 1-4 have no other changes beyond the items above.
