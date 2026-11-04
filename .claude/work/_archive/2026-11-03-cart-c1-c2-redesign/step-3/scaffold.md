# Implementer report: step 3, scaffold

## 1. Mode and step

Mode: scaffold. Step 3 (plan r4): "Cart: load guest counts, pass the mode, handle Save, block group submit".

## 2. Log

1. Read plan r4 step 3, `cart.component.ts`, the head of `cart.component.spec.ts`, `ReservationService` (`getReservationsForUser$(): Observable<Reservation[]>`, `reservation.service.ts:81`), `ToastService.success(message, title?)` and `Reservation` (`id`, `guestCount`).
2. Created `src/app/components/cart/models.ts` with `export interface CartOrderView { order: Order; mode: CartOrderMode; }`. The contract defines it, and it is a declaration, so it is real code.
3. Added these members to `cart.component.ts`:
   - `private readonly reservationService = inject(ReservationService);`
   - `private readonly toast = inject(ToastService);`
   - `readonly reservationGuestCounts = signal<ReadonlyMap<string, number> | null>(null);` (real; it holds state, not logic)
   - `readonly unsubmittedOrderViews = computed<CartOrderView[]>(() => { throw new Error('Not implemented'); });`
   - `saveOrder(orderId: string): void { throw new Error('Not implemented'); }`
   - `private orderMode(order: Order): CartOrderMode { throw new Error('Not implemented'); }` (the plan names this method for reuse in step 5)
4. Kept today's behaviour: `ngOnInit`/`loadOrders` (no `forkJoin` yet), `submitOrder` (no mode guard yet) and the template. The template still uses `unsubmittedOrders()` with the interim `[orderMode]="'single'"` from step 2. These are new behaviour for the implement step. A stub there would break the working cart.
5. I did not touch the `not-submitted-order` files.
6. Ran the gates.

## 3. Files changed

- New: `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/models.ts`
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/cart.component.ts`

## 4. Check results

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | exit 0, 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | exit 0, 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, "Bundle check passed: no configuration placeholders left." |
| Initial bundle | 804.52 kB raw, 171.39 kB transfer |
| Build warnings | 13 `WARNING` lines, the same as before. None for `cart.component.scss` or `not-submitted-order.component.scss`. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `TOTAL: 51 FAILED, 229 SUCCESS` (280 tests), exit 1 |

The 51 failures are the 46 pre-existing ones (the names are identical) plus all 5 existing `CartComponent` tests:

- `should create`
- `should emit closeCartEvent when closeCart is called`
- `should navigate to reservations when goToReservations is called`
- `should return submitted orders with statuses 1 and 3`
- `should return unsubmitted orders with status 0`

Cause: `NG0201: No provider found for InjectionToken API_BASE_URL. Path: ReservationService -> InjectionToken API_BASE_URL`. The contract requires the new `ReservationService` injection. The current cart spec provides only `PreOrdersService` and `Router` mocks, so `TestBed` cannot create the component. I could not prevent this without dropping a contract member. The plan already tells the test-writer to "add mocks for `ReservationService` and `ToastService`" in this spec, and that change fixes these 5 tests. This is not a test defect. It is the expected red state for this step.

The test run also logged proxy refusals for `test.api`, `www.google.com` and `redirector.gvt1.com`. They come from existing specs and Chrome, and they do not affect the totals.

## 5. Fix mode

Not applicable.

## 6. Test defects

None.

## 7. Not checked locally

Nothing for the scaffold.

## 8. Follow-ups (not done)

- Test-writer: the cart spec `beforeEach` must provide `ReservationService` (a `getReservationsForUser$` spy) and `ToastService` (a `success` spy). Without them, all cart tests fail with NG0201.
