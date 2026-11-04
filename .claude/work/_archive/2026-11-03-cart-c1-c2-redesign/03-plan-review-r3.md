# Plan review r3 (delta): Cart redesign, screens C1 and C2

Reviewed: `02-plan-r3.md` against `02-plan-r2.md` (full `diff`), `00-task.md` "Decisions" (M1-M4, checkpoint Q5 (b)), `03-plan-review-r2.md`, and the code: `cart.component.ts/.html/.spec.ts`, `submitted-order.component.ts/.html/.spec.ts`, `not-submitted-order` usage, `tsconfig.json`, and the Angular 22 template type checker in `node_modules/@angular/compiler`.

Scope of this review: (1) M1-M4 in steps 2-4, and no other change to steps 1-4. (2) new step 5.

## Verdict
changes required

One major finding: M3 (`input.required`) makes step 2 leave the build red, because the parent binding comes only in step 3. Two minor findings. The rest of the delta is correct.

## Findings

### [major] Step 2 (M3) — required input with no parent binding until step 3 breaks the build and the whole Karma run
- Problem: Step 2 makes `NotSubmittedOrderComponent.orderMode` an `input.required<CartOrderMode>()`. Its only user, `cart.component.html:44-50`, gets `[orderMode]="view.mode"` only in step 3. Between the two steps the Angular compiler reports NG8008 "Required input 'orderMode' ... must be specified" for the cart template. This breaks `npm run build`, and it also breaks `npm test`, because Karma compiles `cart.component.spec.ts` (which imports `CartComponent`) in the same bundle. So step 2 cannot be checked alone, and the test-writer's red run for step 2 fails for the wrong reason. The step 2 "Check" lists only the two `tsc` gates, and plain `tsc` does not check templates, so the check does not catch it.
- Evidence: `strictTemplates` is not set in `tsconfig.json:27-32`, and Angular 22 defaults it to on (`compiler-cli/bundles/chunk-VG44FWYM.js:4902`: `this.options.strictTemplates !== false`). `TcbDirectiveInputsOp.checkRequiredInputs` reports every unbound required input (`@angular/compiler/fesm2022/compiler.mjs:31966-31976`). Rule: checklist 5, "each step leaves the build green".
- Required change: Make step 2 leave the build green. For example: step 2 also adds an interim binding in `cart.component.html` (state the value and why; `'single'` keeps today's behaviour until step 3 replaces it), or move the parent binding into step 2, or swap the order so that the scaffold of step 3 lands first. Add `npm test` (or the build) to the step 2 check, because `tsc` does not see this error. The same rule applies to step 5: its scaffold adds a required input to `SubmittedOrderComponent`, so the scaffold must also add `[orderMode]` in `cart.component.html:68` (and therefore `submittedOrderViews`) in the same scaffold. Say so in step 5.

### [minor] Step 5 — the cart `editOrder` guard has no test for `'unknown'`
- Problem: The cart tests call `editOrder` directly only for `'group'` (guestCount 4) and `'single'`. A guard written as `mode === 'group'` passes all listed cart tests but lets an `'unknown'` order un-submit (for example through `editOrderModal`). The "reservations error → Edit not shown" test checks only the child's `canEdit`, not the parent guard. This is the same gap that M2 closed for `submitOrder`.
- Evidence: `02-plan-r3.md` step 5 cart tests. Contract "if the order's mode is not `'single'`, return with no API call".
- Required change: Add one cart test: `getReservationsForUser$` errors (or the `reservationId` is not in the list), a status 1 order exists, `editOrder(orderId)` called directly → `unsubmitPreOrderByReservationId` is not called.

### [minor] Step 5 — the existing child test "should emit edited when editOrder is called" changes meaning
- Problem: The plan keeps the 4 existing tests and sets `orderMode: 'single'` in `beforeEach`. The existing test (`submitted-order.component.spec.ts:179-185`) then still passes, which is correct. But the plan does not say that this test now depends on the `'single'` default of `beforeEach`. If the test-writer moves the mode setup, this test fails for the wrong reason.
- Evidence: `submitted-order.component.spec.ts:152-165,179-185`.
- Required change: State that the existing "emit edited" test runs in mode `'single'` (from `beforeEach`), or replace it with the new mode-`'single'` click test. No other change.

## Earlier findings (r2)
- M1 footer at the panel bottom with short content — fixed. Step 2: `:host` and `.not-submitted-order` are flex columns with `flex: 1 0 auto`, `.order-footer` has `margin-top: auto` plus `position: sticky; bottom: 0`. The horizontal margins are now `margin-left`/`margin-right`, so they do not reset `margin-top: auto`. Step 4: `.cart-body` is a flex column, the unsubmitted `.orders-list` is `flex: 1 0 auto; display: flex; flex-direction: column`. Manual check (a) short content, (b) scroll, (c) full-width border. The CSS chain is correct: sticky is limited by `.not-submitted-order`, which spans the content height, so the scroll case still works.
- M2 `submitOrder` in `'unknown'` mode — fixed. Two tests: reservations error, and `reservationId` not in the list. Both assert `submitPreOrderByReservationId` is not called.
- M3 `input.required` — applied as asked, but it causes the major finding above (build order). Partly fixed.
- M4 price format — fixed. Recorded under "Contracts and risks" as an accepted deviation. The step 2 hook text names `$20.00`.

## Diff check of steps 1-4 (r2 → r3)
Other changes, all acceptable and in scope:
- Step 1: no change.
- Step 2: wording only ("Only their styling", "`-` has a ... border").
- Step 3: the order → mode mapping goes into one private method for reuse in step 5 (one decision in one place). The "Submitted pre-orders" section note now points to step 5. Test list: only the two M2 tests added.
- Step 4: wording. "the step 2-3 tests".
- Header, F9, Open questions, risks and out-of-scope text now match Q5 (b). The "Style budget" line changed from "can pass" to "can grow past" the 4 kB warning budget. This is a clearer wording of the same risk.

## Confirmed (step 5 against the code)
- `SubmittedOrderComponent` is `submitted-order.component.ts:20-43`: input `order` (required), outputs `canceled`/`edited`, methods `cancelOrder()`/`editOrder()`. The buttons are at `submitted-order.component.html:52-68`. The plan line refs are correct.
- `app-submitted-order` has one user: `cart.component.html:68-72` (inside `:66-74`), bound with `(canceled)="openCancelModal(item.orderId)"` and `(edited)="editOrder(item.orderId)"`.
- `CartComponent.editOrder` is `cart.component.ts:127-145` and calls `unsubmitPreOrderByReservationId(order.reservationId)`. `editOrderModal` (`:89-99`) calls `editOrder`, so the new guard covers the submit-modal path too. The submit modal opens only after a successful `submitOrder` (`:154-160`, `cart.component.html:110-119`), which step 3 allows only for `'single'`.
- Fail closed: `canEdit = orderMode() === 'single'` hides Edit for `'group'` and `'unknown'`. The child `editOrder()` emits nothing unless `canEdit()`. The parent `editOrder` refuses non-`'single'`. Three layers, each tested (child button hidden for group/unknown, child method for group/unknown, parent for group).
- "Cancel Pre-order" stays in every mode, with a test for each mode. This matches the user decision (only Edit is hidden).
- `submittedOrderViews` reuses the step 3 mapping, so status 1 and status 3 use one rule. Cart tests cover status 1 single, status 1 group, status 3 group, reservations error.
- Test cases are concrete: exact button texts, the spied service method and argument (`reservationId`), and emitted outputs. Each can fail.
- The existing 5 cart tests do not depend on the Edit button (`cart.component.spec.ts:57-125`).
- No API, route, or infra change in step 5.
