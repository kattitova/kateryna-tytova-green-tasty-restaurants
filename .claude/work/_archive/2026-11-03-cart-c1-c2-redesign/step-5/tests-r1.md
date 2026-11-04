# Test-writer report: step 5, round 1

## Tests written

### `submitted-order.component.spec.ts`

Setup: `beforeEach` now also sets `orderMode` `'single'` with `setInput`. The 4 existing tests are unchanged in content. Prettier changed only their line breaks. "should emit edited when editOrder is called" depends on the `'single'` mode in `beforeEach`. New tests set their mode with `setInput` + `detectChanges`. A helper finds a button by its trimmed text.

New tests (8), in `describe('edit and cancel by order mode')`:

- `shows "Edit Pre-order" in single mode and emits edited on click`
- `does not show "Edit Pre-order" in group mode` (also checks that "Cancel Pre-order" is shown)
- `does not show "Edit Pre-order" in unknown mode` (also checks that "Cancel Pre-order" is shown)
- `does not emit edited from editOrder in group mode`
- `does not emit edited from editOrder in unknown mode`
- `shows "Cancel Pre-order" in single / group / unknown mode and emits canceled on click` (3 tests)

### `cart.component.spec.ts`

New tests (8), in `describe('submitted orders')`. They use the step 3 mocks and `renderWith`. The order is `order-2`, `reservationId` `res-1`, and the item has `state: 1`. `unsubmitPreOrderByReservationId` returns `of({})`. The buttons are found inside `app-submitted-order`.

- `shows "Edit Pre-order" for a status 1 order with 1 guest` (list `[res-2: 4, res-1: 1]`)
- `hides "Edit Pre-order" and shows "Cancel Pre-order" for a status 1 order with 4 guests` (list `[res-2: 1, res-1: 4]`)
- `hides "Edit Pre-order" for a status 3 order with 4 guests`
- `hides "Edit Pre-order" and shows "Cancel Pre-order" when the reservations load fails`
- `does not unsubmit a group order when editOrder is called directly`
- `does not unsubmit when the reservations load failed and editOrder is called directly` (r4)
- `does not unsubmit when the reservation is not in the list and editOrder is called directly` (r4)
- `unsubmits a single order by reservation ID when editOrder is called directly` (list `[res-2: 4, res-1: 1]`, expects `'res-1'` once)

## Red run

1. `npx tsc -p tsconfig.spec.json --noEmit` → exit 0, 0 errors.
2. `npm test -- --watch=false --browsers=ChromeHeadless` → **TOTAL: 56 FAILED, 254 SUCCESS** (310 tests).
3. Compared with the 46 baseline failures, the only additions are 10 new tests. The other 46 failing names are identical.

Failure reasons of the 10 red tests:

| Tests | Reason |
|---|---|
| submitted-order: no Edit in group, no Edit in unknown (2) | `Expected <button ... button--primary ...> to be undefined` (the button always renders) |
| submitted-order: `editOrder()` in group, in unknown (2) | `Expected spy emit not to have been called` |
| cart: Edit hidden for status 1 + 4 guests, status 3 + 4 guests, load failed (3) | `Expected <button ...> to be undefined` |
| cart: `editOrder` for group, load failed, not in list (3) | `Expected spy PreOrdersService.unsubmitPreOrderByReservationId not to have been called` |

Six new tests pass already. Today the Edit button always renders, `editOrder` always emits or unsubmits, and Cancel does not change. The plan lists these tests to pin the single path and Cancel. They catch these wrong implementations:

- submitted-order "Edit in single mode": `canEdit` is `false` for every mode, or `editOrder` blocks every mode. The button is missing, or the click emits nothing.
- submitted-order "Cancel in each mode" (3 tests): the mode guard also hides Cancel or blocks `cancelOrder` in group or unknown mode.
- cart "Edit for 1 guest": the cart gives `'unknown'` to submitted orders (for example, it maps only `unsubmittedOrders`), or it looks up the first reservation. The list puts `res-2` (4 guests) first, so that mistake gives `'group'`.
- cart "editOrder for a single order": the guard blocks every mode, or the lookup uses the first reservation (same list order). The test also checks the `'res-1'` argument.

## Existing tests

- The 4 existing `SubmittedOrderComponent` tests pass.
- All 19 existing `CartComponent` tests pass.
- I did not change the `not-submitted-order` spec.

## Not tested

- The edit path through the submit modal (`editOrderModal()`). The plan says that it goes through `editOrder`. The direct `editOrder` tests cover the guard, and the plan does not list a modal test.
- Behaviour with real data. It needs the deployed API.

## Notes for the implementer

- The "Edit Pre-order" button must be absent from the DOM (not only disabled) in `'group'` and `'unknown'` mode.
- The "Cancel Pre-order" button text must stay exactly `Cancel Pre-order`, in every mode.
- The cart gives each submitted order (status 1 and 3) a mode from `Reservation.id === Order.reservationId`.
