# Implementer report: step 3, implement, round 1

## 1. Mode and step

Mode: implement. Step 3 (plan r4): "Cart: load guest counts, pass the mode, handle Save, block group submit".

## 2. Log

1. Read `step-3/tests-r1.md` and the new cart spec. The spec agrees with the plan contract, so there is no test defect.
2. Red state: `tests-r1.md` records 10 red cart tests (full run `56 FAILED, 237 SUCCESS`, 293 tests). The scaffold had no change after that run, so I did not run it again.
3. `cart.component.ts`:
   - `orderMode(order)` (private, the one place for order → mode): `toCartOrderMode(this.reservationGuestCounts()?.get(order.reservationId))`. A `null` map or a missing reservation gives `undefined`, and `undefined` gives `'unknown'`.
   - `unsubmittedOrderViews`: `unsubmittedOrders().map((order) => ({ order, mode: this.orderMode(order) }))`. It keeps the same order.
   - First load (`ngOnInit` → `loadOrdersAndGuestCounts()`): `forkJoin({ orders: preOrders$(), guestCounts: guestCounts$() })`, with `takeUntilDestroyed` and `finalize(() => isLoadingOrders.set(false))`. The spinner stays until both sources complete.
   - `preOrders$()` holds the orders error handling that `loadOrders` had before: `console.error('Error loading orders:', ...)`, then `[]`. The first load and the reloads both use it, so this behaviour is in one place.
   - `guestCounts$()`: maps `Reservation[]` to `new Map(id → guestCount)`. On an error it logs and returns `null`. It adds no `SKIP_ERROR_TOAST`, so the global interceptor toast shows (F5, accepted).
   - `loadOrders()` (used by the reloads after edit, remove, cancel, submit and unsubmit) calls only `preOrders$()`. `getReservationsForUser$` runs one time for each cart open.
   - `submitOrder`: `if (!order || this.orderMode(order) !== 'single') return;` before the API call. Group orders and unknown orders make no call (M2).
   - `saveOrder`: if `findOrder(orderId)` finds the order, it calls `toast.success('Your pre-order is saved.')` and then `closeCart()`. It calls no `PreOrdersService` method. An unknown ID does nothing (`findOrder` logs it, as the other handlers do).
4. `cart.component.html`: the unsubmitted section header is now `<h4>My Pre-Order</h4>`. "Not submitted pre-orders" and "You have not submitted pre-orders" are removed. The loop is now `@for (view of unsubmittedOrderViews(); track view.order.orderId)` with `[order]="view.order"`, `[orderMode]="view.mode"` (this replaces the interim `'single'`) and `(saved)="saveOrder($event)"`. The other bindings use `view.order.orderId`. The submitted section, the modals and the empty state did not change.
5. I did not touch the `not-submitted-order` files.
6. Ran all gates and ESLint on the changed files.

## 3. Files changed

- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/cart.component.ts`
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/cart.component.html`
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/models.ts`: no change since the scaffold.

## 4. Check results

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | exit 0, 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | exit 0, 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, "Bundle check passed: no configuration placeholders left." |
| Initial bundle | 805.32 kB raw, 171.68 kB transfer (804.52 kB after the scaffold) |
| Build warnings | 13 `WARNING` lines, the same as before. None for `cart.component.scss` or `not-submitted-order.component.scss`. |
| ESLint on `cart.component.ts`, `cart.component.html`, `models.ts` | no output, 0 problems |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `Executed 293 of 293 (46 FAILED)`, `TOTAL: 46 FAILED, 247 SUCCESS`, exit 1 |

The failing names are identical to the 46 pre-existing ones (`diff` gives no output). All `CartComponent` tests pass: the 5 old tests and the 13 new ones.

## 5. Fix mode

Not applicable.

## 6. Test defects

None.

## 7. Not checked locally

- Behaviour with real data (`GET /reservations`, the guest counts, the interceptor toast on a failure). This needs the deployed API.
- The visual result of the new `<h4>` heading. The cart layout is step 4.

## 8. Follow-ups (not done)

- `.list-header` in `cart.component.scss` is now used only by the submitted section. Step 4 restyles the cart. It can review this.
- `addDishToOrder` keeps its existing `console.log`. Q4 says to keep it unchanged.
