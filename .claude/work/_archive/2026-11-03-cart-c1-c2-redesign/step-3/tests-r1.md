# Test-writer report: step 3, round 1

## Tests written

File: `frontend/restaurant-frontend-app/src/app/components/cart/cart.component.spec.ts`.

Setup changes:

- Added `jasmine.SpyObj` mocks for `ReservationService` (`getReservationsForUser$`, default `of([])`) and `ToastService` (`success`). This fixes the NG0201 error in the 5 existing tests.
- `beforeEach` now only configures `TestBed`. A `createComponent()` helper creates the component and runs change detection. A `renderWith(orders, reservations$)` helper sets the two service results first.
- The 5 existing tests are unchanged in content. They moved into `describe('with an empty cart')`, which calls `createComponent()`. Prettier changed only their line breaks.
- The order fixture: `order-1`, status 0, `reservationId` `res-1`, 2 × "Caesar Salad" at 10.

New tests (13). The child component from step 2 renders the CTA, so the cart tests check the DOM through it.

| Describe | Test | Behaviour |
|---|---|---|
| order mode | renders an enabled "Submit Pre-Order" button for a reservation with 1 guest | list `[res-2: 4, res-1: 1]` |
| order mode | renders "Save Pre-Order" for a reservation with 4 guests | list `[res-2: 1, res-1: 4]` |
| order mode | renders a disabled button and the unavailable message when the reservation is not in the list | |
| order mode | still renders the order with a disabled button and the unavailable message when the reservations load fails | `throwError` |
| order mode | shows the "My Pre-Order" heading when there is an unsubmitted order | an `h4` with that text |
| loading | shows the spinner and no button while the reservations are pending | orders `of`, reservations a `Subject` |
| loading | hides the spinner and shows the button after the reservations emit and complete | |
| loading | loads the reservations one time and the orders again after a quantity update | `getPreOrders` 2 times, `getReservationsForUser$` 1 time |
| submit and save | submits by reservation ID when the Submit button is clicked for 1 guest | `submitPreOrderByReservationId('res-1')` once |
| submit and save | shows a toast, closes the cart and calls no order API when the Save button is clicked for 4 guests | toast text, `closeCartEvent` once, no submit, no edit |
| submit and save | does not submit a group order when submitOrder is called directly | |
| submit and save | does not submit when the reservations load failed | M2 |
| submit and save | does not submit when the reservation is not in the list | M2 |

## Red run

1. `npx tsc -p tsconfig.spec.json --noEmit` → exit 0, 0 errors.
2. `npm test -- --watch=false --browsers=ChromeHeadless` → exit 1, **TOTAL: 56 FAILED, 237 SUCCESS** (293 tests).
3. Without the `CartComponent` names, the failing names are identical to the 46 in the baseline. The 10 other failures are new cart tests.

Failure reason for each red test:

| Test | Reason |
|---|---|
| "Save Pre-Order" for 4 guests | `Expected 'Submit Pre-Order' to be 'Save Pre-Order'` (interim `'single'` binding) |
| not in list: disabled | `Expected false to be true` |
| load fails: disabled | `Expected false to be true` |
| "My Pre-Order" heading | `Expected [ 'Grand Total' ] to contain 'My Pre-Order'` |
| spinner while pending | `Expected null not to be null` (the spinner is gone, because the cart does not wait for the reservations) |
| reservations loaded once | `getReservationsForUser$ ... was called 0 times` |
| Save click | `ToastService.success` not called (the click submits instead) |
| submitOrder, group | `submitPreOrderByReservationId` was called |
| submitOrder, load failed (M2) | `submitPreOrderByReservationId` was called |
| submitOrder, not in list (M2) | `submitPreOrderByReservationId` was called |

Three new tests pass already, because the interim `[orderMode]="'single'"` binding and today's load give the same result:

- `renders an enabled "Submit Pre-Order" button for a reservation with 1 guest`
- `submits by reservation ID when the Submit button is clicked for 1 guest`
- `hides the spinner and shows the button after the reservations emit and complete`

I keep them, because the plan lists them and step 3 replaces the code under them. They are not vacuous. They catch these wrong implementations:

- Mode `'unknown'` for every order (map never set, or set after the render): the CTA is disabled, and the click submits nothing.
- A lookup of the first reservation instead of the one with `id === reservationId`: the list puts `res-2` (4 guests) first, so the result is `'group'` and "Save Pre-Order".
- A lookup by a wrong key that misses `res-1`: the mode is `'unknown'`.
- A join that never clears `isLoadingOrders` after the reservations complete: the spinner stays and `.cart-cta` is `null`.

## Existing tests

The 5 existing `CartComponent` tests pass now (before: 5 failed with NG0201). The 46 other pre-existing failures do not change. I did not change the `not-submitted-order` spec.

## Not tested

- The global interceptor toast for a failed reservations load (F5). The test mocks the service, so no interceptor runs. The plan accepts this toast and does not ask for a test.
- Behaviour with real data. It needs the deployed API.

## Notes for the implementer

- Toast text: exactly `Your pre-order is saved.`, one call, with no title argument.
- `saveOrder` emits `closeCartEvent` once and calls no `PreOrdersService` method.
- The heading must be an `<h4>` with the trimmed text `My Pre-Order`.
- The spinner must be an `app-loading-spinner` element, and it must stay until both `getPreOrders()` and `getReservationsForUser$()` complete.
- After `updateDishQuantity` succeeds, the cart calls `getPreOrders()` again but not `getReservationsForUser$()`.
- The reservation map key is `Reservation.id`. The order key is `Order.reservationId`.
