# Code review: step 3, round 1

Step 3 (plan r4): "Cart: load guest counts, pass the mode, handle Save, block group submit". Also the carried-over item from step 2 (review-r1 minor 1).

## Scope

Commits:
- `bdc76ec` scaffold (implementer): `cart.component.ts`, new `cart/models.ts`
- `affe578` tests (test-writer): `cart.component.spec.ts`
- `1a821c4` implementation (implementer): `cart.component.ts`, `cart.component.html`
- `7993bb6` carried-over fix (test-writer): `not-submitted-order.component.spec.ts`

The working tree is clean (`git status --porcelain` gives no output). There are no `infra/` or `*.tf` changes.

**Maker/checker split:** OK. The implementer commits change only production files. The test-writer commits change only `*.spec.ts` files.

## Plan conformance

All the step 3 contract items are present:
- `CartOrderView` in `cart/models.ts`. This matches app `CLAUDE.md` §4: "interface used by one component only → `models.ts`".
- `ReservationService` and `ToastService` injections.
- `reservationGuestCounts` signal. `null` means "not loaded" or "load failed".
- `unsubmittedOrderViews`.
- A private `orderMode(order)`. It is the one place for the mapping, and step 5 can reuse it.
- First load with `forkJoin` + `finalize`. The spinner waits for both requests.
- The reloads call only `getPreOrders()`.
- The `submitOrder` guard `!== 'single'` fails closed for `'group'` and for `'unknown'` (user decision, M2).
- `saveOrder` makes no API call. It shows the toast and closes the cart.
- In the template: `<h4>My Pre-Order</h4>`, `[orderMode]="view.mode"` (replaces the interim `'single'`) and `(saved)`.

There are no changes outside the step. The submitted section, the modals and the empty state did not change.

User decisions:
- Save makes no API call: OK. The test checks that submit and edit are not called.
- A group owner cannot submit alone: OK. The CTA routes to Save. The direct `submitOrder` call is guarded and has a test.
- `'unknown'` fails closed: OK. Both the "load failed" path and the "reservation not in the list" path are covered for rendering and for `submitOrder`.

## Correctness notes (checked, no finding)

- `forkJoin` needs every source to emit. Both sources are single-emission `HttpClient` GETs (`pre-orders.service.ts:16-18`, `reservation.service.ts:81-84`). Both sources have a `catchError` that emits a fallback value, so `forkJoin` always emits once and `finalize` always clears the spinner.
- `takeUntilDestroyed` is used on both subscriptions. No leak.
- The header mounts `<app-cart>` inside `@if (showCart())` (`header.component.html:61-65, 249-251`). So "one `GET /reservations` per cart open" is true, and a reservation that the user makes while the cart is closed is loaded on the next open.
- `GET /reservations` for a customer returns all the customer's reservations, with `id` and `guestCount` (`ApiRoutes.md` "GET /reservations", `GetReservationsForUserHandler.cs` customer path). The handler skips a corrupted reservation. That order then gets `'unknown'` (fail closed), which is the intended result.
- `computed` reads `reservationGuestCounts()` through `orderMode()`, so the views recompute when the map arrives.

## Findings

### [minor] [tests] cart.component.spec.ts:245-296 — the "unknown orderId" guard in `saveOrder` is not tested
- Problem: the contract says "Unknown `orderId` → nothing". No test checks this.
- Scenario: remove the `if (!this.findOrder(orderId)) return;` guard in `cart.component.ts:186-188`. All 18 cart tests still pass, and `saveOrder('missing')` then shows the toast and closes the cart.
- Fix: add one test: render with `[unsubmittedOrder]` and guest count 4. Call `component.saveOrder('missing-id')`. Expect that `toastMock.success` is not called and that `closeCartEvent.emit` is not called.

## Notes (no action in this step)

- The orders error path on first load is not pinned. `preOrders$()` moved the old `error:` handler into `catchError` inside `forkJoin`. The visible result is the same as before (`orders = []`, empty state). The plan asks for no test. Without that `catchError`, the cart would still look the same, but the error would go to the global `ErrorHandler`. This has low value, so I do not raise a finding.
- `header.component.html` has two `<app-cart>` instances (line 63 in the mobile actions, line 250). If both render together, each one sends its own `GET /orders` and its own `GET /reservations`. This was already true for the orders before this change. It is not in the diff.
- `.list-header` styles are now used only by the submitted section. The implementer has already sent this to step 4.

## Carried-over item from step 2

- step-2/review-r1 minor 1 (unknown-mode label not tested): **fixed**. `7993bb6` adds exactly `expect(cta()?.textContent?.trim()).toBe('Submit Pre-Order');` to "unknown mode disables the button" (`not-submitted-order.component.spec.ts:125`). The commit changes nothing else (1 insertion). It kills the mutation that I described: `'unknown'` → "Save Pre-Order".
- step-2/review-r1 minor 2 ("-" button looks grey when enabled): the orchestrator deferred it to `docs/tech-debt.md`. I did not review it here.

## Check results (run by the reviewer, `frontend/restaurant-frontend-app`)

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | exit 0, 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | exit 0, 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, "Bundle check passed: no configuration placeholders left." Initial total 805.32 kB raw / 171.68 kB transfer. 13 `WARNING` lines, none for cart files. |
| `npx eslint` on `cart.component.{ts,html,spec.ts}`, `cart/models.ts`, `not-submitted-order.component.spec.ts` | exit 0, 0 problems |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `Executed 293 of 293 (46 FAILED)`, `TOTAL: 46 FAILED, 247 SUCCESS`. No failure in `CartComponent` or `NotSubmittedOrderComponent`. The failures by suite: GeneralInformationComponent 32, and 1 each in AppComponent, DishModal, DishSelectCard, MakeReservation, MenuPage, PlaceOrderWaiter, Register, ReportingDashboard, ReservationConfirmed, ReservationDashboard, TimeSlot, TimeSlotsModal, WaiterDashboard, WaiterPreOrderPage. These are the 46 pre-existing failures. |

## Verdict

**approve with minor**: 0 blocker, 0 major, 1 minor (tests). The carried-over step 2 minor 1 is fixed.
