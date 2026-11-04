# Plan r3: Cart redesign, screens C1 and C2

Mode: `--lite`. No explorer report exists, so I collected the facts myself with Grep, Glob, Read and read-only Bash.

## Changes after review
This section lists only the delta from r2 to r3. Its sources are the user checkpoint (Q5 option (b)) and the binding minors M1-M4 of `03-plan-review-r2.md`, as listed in `00-task.md` "Decisions".
- Q5 (b), user decision → new **step 5**: `app-submitted-order` hides "Edit Pre-order" for already-submitted group orders. If the mode is `'unknown'`, Edit is also hidden (fail closed). `CartComponent` passes the mode to submitted orders and refuses `editOrder` for a non-`'single'` order. Q5 has moved from "Open questions" to a decided item. The F9, risks and out-of-scope text now match it.
- M1 (steps 2 and 4) → the footer sits at the panel bottom with short content too. `.cart-body` becomes a flex column, the unsubmitted section and the `app-not-submitted-order` host grow, and `.order-footer` gets `margin-top: auto` together with `position: sticky`. The step 4 manual check now includes the short-content case.
- M2 (step 3) → new tests: `submitOrder` called directly for an order in `'unknown'` mode (reservations error, and a `reservationId` that is not in the list) does not call `submitPreOrderByReservationId`.
- M3 (step 2) → `orderMode` is `input.required<CartOrderMode>()`. The tests set it explicitly. The "default mode" test is gone, and an explicit `'single'` test replaces it.
- M4 (step 2) → the price format `$20.00` (currency pipe) stays. The Figma format is whole dollars, and the plan records this as an accepted deviation under "Contracts and risks".

## Goal
Restyle the header cart (`app-cart`) to Figma C1/C2, and select the footer button from the reservation size: `guestCount < 2` gives C1 "Submit Pre-Order", `guestCount >= 2` gives C2 "Save Pre-Order" (no API call, the order stays not submitted). A group owner cannot submit or un-submit an order alone from the cart.

## Facts (my own investigation, with evidence)

### F1. Which cart: the header cart `src/app/components/cart/`
- The Figma frame names are "C1 · Cart — **owner only** (1 guest)" and "C2 · Cart — group reservation". The header renders `<app-cart>` (`shared/custom-components/header/header.component.html:63,250`). `CartComponent` loads the owner's orders with `GET /orders` (Cognito).
- The owner menu page shows the banner "You are making a pre-order for a reservation" (`pages/menu-page/menu-page.component.html:24`).
- `pages/guest-pre-order-page/ui/guest-cart-drawer/` is the cart of an invited guest (route `join/:token`, `app.routes.ts:99`). A guest has no single/group choice. Do not change it.

### F2. Current cart structure
- `cart.component.ts` (OnPush, signal `output()`, `@app/...` imports) loads all orders of the user and splits them into `unsubmittedOrders` (status 0) and `submittedOrders` (status 1, 3). It can show more than one order.
- Each unsubmitted order is one `app-not-submitted-order`: caption "Pre-order #id (address, time)", dish rows, "Add dish" button, "Total payment", "Submit Pre-order". Inputs/outputs: `order` (required), `submitted: string`, `addDish: void`, `removeDish: string`, `updateQuantity: {itemId, quantity}`.
- Each submitted order is one `app-submitted-order` (`submitted-order.component.ts`): input `order` (required), outputs `canceled: void`, `edited: void`, methods `cancelOrder()`, `editOrder()`, buttons "Cancel Pre-order" and "Edit Pre-order" (`submitted-order.component.html:52-68`). The cart binds `(canceled)="openCancelModal(item.orderId)"` and `(edited)="editOrder(item.orderId)"` (`cart.component.html:67-73`).
- Submit today: `submitOrder(orderId)` → `POST /orders/{reservationId}/submit` → `app-submit-order-modal`.
- Quantity and remove changes persist at once with `PUT /orders/{reservationId}`.

### F3. How to know 1 guest vs group
- `Order` and `GET /orders` have no guest count and no group id, only `reservationId` (`models/orders/order.model.ts`, `GetOrderResponse.cs`).
- `GET /reservations` returns `guestCount` (`ApiRoutes.md:469-486`). Frontend: `ReservationService.getReservationsForUser$(): Observable<Reservation[]>` (`reservation.service.ts:81`), `Reservation.id`, `Reservation.guestCount: number`.
- Backend rule: a group reservation exists only when `GuestCount >= 2` (`ReservationService.cs:72`, `GroupReservationService.cs:132`).
- Conclusion: join orders to reservations by `reservationId`. No API change.

### F4. Group endpoints (for the future group pre-order task, not used here)
- `GET /reservations/{id}/guest-session-id` (`owner-<groupReservationId>`), `GET /guest/groups/{id}/sessions` (owner Cognito token). `tokenInterceptor` does not add the token to URLs that contain `/guest/` (`auth/token.interceptor.ts:23-63`), so a future owner call to `/guest/groups/...` needs an interceptor change.

### F5. Error toasts
- `tokenInterceptor` shows a global toast for every HTTP error, except `orders` 404 and requests with `SKIP_ERROR_TOAST` (`auth/token.interceptor.ts:83-121`). `getReservationsForUser$` does not set the flag, so a failure of `GET /reservations` shows the global toast.

### F6. "Save Pre-Order" semantics (user decision Q1)
- No "save" endpoint exists. Dishes already persist with `PUT /orders/{reservationId}`. Save = no API call, toast, close cart. The order stays status 0.
- The final group submit belongs to a future "View group pre-order" modal on the reservation card (separate task). The user accepts that a group owner has no submit path in the UI until then.

### F7. Baseline
- `node_modules` is not installed. `npx tsc --noEmit` fails on every Angular import (TS2307). Run `npm ci` before the gates. No test totals from this run.

### F8. Deviation from frontend `CLAUDE.md` §5
- §5 says `ChangeDetectionStrategy.Eager`, decorator `@Input()`, and that `app/...` imports do not resolve. The cart files use `OnPush`, signal `input()`/`output()` and `@app/...` (`tsconfig.json:7-9` has `paths`). Follow the code of the files you change (precedence 5).

### F9. Submitted-order and submit-order-modal path for group orders
- `app-submit-order-modal` opens only after a successful `submitOrder` (`cart.component.ts:147-166`, `cart.component.html:110-119`). After step 3 a group order never reaches the submit call, so the modal cannot open for a group order. Its "Edit" (`editOrderModal` → `editOrder`) therefore runs only for `'single'` orders. No change to the modal.
- A group order can be in the submitted list: status 1 (submitted alone before this change) or status 3 (`SubmittedByCustomer`, after the backend group submit, `GroupPreOrderService.cs:57`).
- "Edit Pre-order" calls `POST /orders/{id}/unsubmit`. The backend sets the order to `Created` from any status and does not change the group reservation status (`OrderService.cs:200-219`). For a group order this gives an order with no submit path. For status 3, the backend group submit also refuses it (400 `GroupReservationSubmitted`). User decision Q5 (b): hide "Edit Pre-order" for group orders (step 5).
- "Cancel Pre-order" (`DELETE /orders/{reservationId}`) is not part of the decision. It stays visible in every mode.

## Open questions
- None. Q1-Q5 are decided (`00-task.md` "Decisions" and the plan checkpoint).

## Steps

1. Data shape: order mode type and pure helper
   - Type: code
   - Area: frontend
   - Scaffold first: yes
   - Files: new `frontend/restaurant-frontend-app/src/app/models/cart/cart-order-mode.model.ts`, new `frontend/restaurant-frontend-app/src/app/shared/helpers/group-reservation.helpers.ts`
   - Contract:
     - `export type CartOrderMode = 'single' | 'group' | 'unknown';`
     - `export const MIN_GROUP_GUEST_COUNT = 2;`
     - `export function isGroupReservation(guestCount: number): boolean` → `guestCount >= MIN_GROUP_GUEST_COUNT`.
     - `export function toCartOrderMode(guestCount: number | undefined): CartOrderMode` → `undefined` → `'unknown'`; `isGroupReservation(guestCount)` → `'group'`; else `'single'`.
   - Tests (for test-writer): `src/app/shared/helpers/group-reservation.helpers.spec.ts` — table-driven `isGroupReservation`: 0 → false; 1 → false; 2 → true; 6 → true; `MIN_GROUP_GUEST_COUNT` is 2; `toCartOrderMode`: `undefined` → 'unknown'; 1 → 'single'; 2 → 'group'; 5 → 'group'; 0 → 'single'.
   - Change: add the two files.
   - Check: both `tsc` gates → 0 errors; the new spec passes.

2. Order card: mode-driven footer button and restyle
   - Type: code
   - Area: frontend
   - Scaffold first: yes (new input, output, computed)
   - Files: `src/app/components/cart/components/not-submitted-order/not-submitted-order.component.ts:20-75`, `.html:1-108`, `.scss:1-105`
   - Contract (keep the signal-API style of this file):
     - New input `readonly orderMode = input.required<CartOrderMode>();` (M3: no default, so a missing binding cannot give a working Submit).
     - New output `readonly saved = output<string>();` emits `order().orderId`.
     - New `readonly ctaLabel = computed(...)`: `'group'` → `'Save Pre-Order'`; `'single'` and `'unknown'` → `'Submit Pre-Order'`.
     - New `readonly ctaDisabled = computed(() => this.orderMode() === 'unknown');`
     - New method `onCtaClick(): void`: `'single'` → `submitted.emit(orderId)`; `'group'` → `saved.emit(orderId)`; `'unknown'` → emits nothing.
     - `'unknown'` renders `<p class="cta-unavailable" role="status">We could not load your reservation details. Reopen the cart to try again.</p>` above the CTA. Other modes do not render it.
     - Keep unchanged: `submit()`, `submitted`, `addDish` + `addDishToOrder()` + the "Add dish" button, `removeDish`, `updateQuantity`, increment/decrement, `totalPrice`, and the reservation caption text "Pre-order #{{ orderId }} ({{ address }}, {{ time }})" (user decision Q4). Only their styling can change.
     - DOM hooks for tests: CTA `<button type="button" class="cart-cta" [disabled]="ctaDisabled()">{{ ctaLabel() }}</button>`. Total row: text "Grand Total" and `<span class="grand-total-value">{{ totalPrice() | currency }}</span>` (replaces "Total payment"; price format stays `$20.00`, M4). Quantity buttons keep `aria-label="Decrease quantity"` / `"Increase quantity"`; decrease is `disabled` when quantity ≤ 1. Remove keeps `aria-label="Remove <dish name>"`.
   - Tests (for test-writer): `not-submitted-order.component.spec.ts` (replace and extend the current 3 tests; `beforeEach` sets `order` and `orderMode` with `setInput`, M3) — mode 'single': `.cart-cta` text "Submit Pre-Order", enabled; click emits `submitted` 'order-1', not `saved`; mode 'group': text "Save Pre-Order"; click emits `saved` 'order-1', not `submitted`; mode 'unknown': `.cart-cta` is disabled, `.cta-unavailable` is shown, `onCtaClick()` emits neither output; modes 'single'/'group': no `.cta-unavailable`; `.grand-total-value` shows `$20.00` for 2 × 10 and the text "Grand Total" is shown; "Decrease quantity" disabled at 1, enabled at 2; "Increase quantity" at quantity 2 emits `updateQuantity` `{ itemId: 'dish-1', quantity: 3 }`; "Remove …" emits `removeDish` 'dish-1'; the "Add dish" button click emits `addDish`; the caption contains 'Test address'.
   - Change: template and SCSS per Figma (00-task.md "Pre-order card", "Footer"): card white, radius 8, padding 16, gap 16, shadow `0 0 5px rgba(218,218,218,0.7)`, `position: relative`; 96 px round image with `object-fit: cover` and shadow `0 0 10px 4px rgba(218,218,218,0.7)`; name 14/24 500 + caption "Expected ready time - 10 minutes" 12/16 400; text `-` / `+` buttons (white, border 1.4px, radius 8, padding 4px 12px; `-` has a `$neutral-200` border and `$neutral-400` text, `+` uses `$green-600`); number 16 px wide, centred; price 18/32 500 `$green-600`; trash button absolute top 8 right 8, padding 8. Footer block (`.order-footer`): "Grand Total" H4 18/32 500 `$neutral-900` and the value 26/40 500 `$green-800`, space-between; CTA full width, height 56, radius 10, background `linear-gradient(-83.73deg, #0F7A37 0%, #0B5B29 98.29%)`, Poppins 600 16/24 white; disabled CTA gets `opacity: 0.5; cursor: not-allowed`. Footer layout: `position: sticky; bottom: 0; margin-top: auto; z-index: 1; background: color.$neutral-0; border-top: 1px solid color.$neutral-100; margin-left: -40px; margin-right: -40px; padding: 24px 40px; display: flex; flex-direction: column; gap: 16px`. At ≤ 650 px: horizontal margins -16px and `padding: 24px 16px` (matches the body padding of step 4). M1: `:host { display: flex; flex-direction: column; flex: 1 0 auto; }` and the root `.not-submitted-order` is `display: flex; flex-direction: column; flex: 1 0 auto`, so with short content `margin-top: auto` pushes the footer to the bottom of the body. Keep the "Add dish" button and the caption between the cards and the footer.
   - Check: both `tsc` gates → 0 errors; spec passes; record the style size of this file from the build (4 kB warning budget).

3. Cart: load guest counts, pass the mode, handle Save, block group submit
   - Type: code
   - Area: frontend
   - Scaffold first: yes (new public members)
   - Files: `src/app/components/cart/cart.component.ts:38-263`, `cart.component.html:31-53`, new `src/app/components/cart/models.ts`
   - Contract:
     - `models.ts`: `export interface CartOrderView { order: Order; mode: CartOrderMode; }`
     - New injections: `ReservationService` (`getReservationsForUser$`), `ToastService` (`success`).
     - `readonly reservationGuestCounts = signal<ReadonlyMap<string, number> | null>(null)` — key: reservation `id`. `null` = not loaded or load failed.
     - `readonly unsubmittedOrderViews = computed<CartOrderView[]>(...)` — one item per `unsubmittedOrders()` item, same order. `mode = toCartOrderMode(map?.get(order.reservationId))`. So: map `null` → `'unknown'`; reservation not in the map → `'unknown'`; else by `guestCount`. Put the order → mode mapping in one private method (for example `private orderMode(order: Order): CartOrderMode`), because step 5 reuses it.
     - Initial load (`ngOnInit`): `getPreOrders()` and `getReservationsForUser$()` joined (for example `forkJoin`). `isLoadingOrders` is true until both complete or fail. A reservations error → map stays `null`, the orders still render (mode `'unknown'`, CTA disabled, inline message). The global interceptor toast also shows (F5); this is accepted, no `SKIP_ERROR_TOAST`. An orders error keeps today's behaviour (`orders` = []).
     - Later reloads (after edit, remove, cancel, submit, unsubmit) call only `getPreOrders()`. `getReservationsForUser$` is called one time per cart open.
     - `submitOrder(orderId)`: if the order's mode is not `'single'` (`'group'` or `'unknown'`), return with no API call. Otherwise today's behaviour.
     - New `saveOrder(orderId: string): void`: no `PreOrdersService` call; if the order exists → `toast.success('Your pre-order is saved.')` and `closeCart()`. Unknown `orderId` → nothing.
     - Template: the unsubmitted section header becomes `<h4>My Pre-Order</h4>` (remove "Not submitted pre-orders" and "You have not submitted pre-orders"). `@for (view of unsubmittedOrderViews(); track view.order.orderId)` → `<app-not-submitted-order [order]="view.order" [orderMode]="view.mode" (submitted)="submitOrder($event)" (saved)="saveOrder($event)" (removeDish)=... (addDish)=... (updateQuantity)=... />` (other bindings unchanged).
     - The "Submitted pre-orders" section is changed only in step 5. The submit modal, the cancel modal and the empty state do not change.
   - Tests (for test-writer): `src/app/components/cart/cart.component.spec.ts` (keep the 5 existing tests; add mocks for `ReservationService` and `ToastService`) —
     - an unsubmitted order whose reservation has `guestCount: 1` renders `.cart-cta` "Submit Pre-Order", enabled;
     - `guestCount: 4` renders "Save Pre-Order";
     - an order whose `reservationId` is not in the list renders a disabled `.cart-cta` and `.cta-unavailable`;
     - `getReservationsForUser$` errors → the order still renders, `.cart-cta` disabled, `.cta-unavailable` shown;
     - loading: `getPreOrders` emits and `getReservationsForUser$` is still pending (a `Subject`) → the spinner (`app-loading-spinner`) shows and no `.cart-cta`; after the reservations emit and complete → the spinner is gone and `.cart-cta` shows;
     - heading "My Pre-Order" shows when there is an unsubmitted order;
     - Submit CTA click (guestCount 1) calls `submitPreOrderByReservationId` with the order's `reservationId`;
     - Save CTA click (guestCount 4) calls `toast.success` with 'Your pre-order is saved.', emits `closeCartEvent`, and calls neither `submitPreOrderByReservationId` nor `editPreOrderByReservationId`;
     - `submitOrder` called directly for a group order (guestCount 4) does not call `submitPreOrderByReservationId`;
     - M2: `getReservationsForUser$` errors, then `submitOrder(orderId)` called directly → `submitPreOrderByReservationId` is not called;
     - M2: the order's `reservationId` is not in the reservations list, then `submitOrder(orderId)` called directly → `submitPreOrderByReservationId` is not called;
     - after a successful `updateDishQuantity`, `getPreOrders` was called 2 times and `getReservationsForUser$` 1 time.
   - Change: as in the contract. Import `toCartOrderMode` from step 1.
   - Check: both `tsc` gates → 0 errors; cart spec passes.

4. Cart panel layout and styles
   - Type: code
   - Area: frontend
   - Scaffold first: no
   - Files: `src/app/components/cart/cart.component.scss:1-116`, `cart.component.html:10-30`
   - Contract: none new. Presentational only.
   - Tests (for test-writer): none: presentational SCSS/HTML change; the step 2-3 tests cover the DOM that this step keeps (`.cart-cta`, heading, close button `aria-label="Close cart"`).
   - Change: panel `width: min(640px, 100vw)`, full height, `box-shadow: 0 0 5px rgba(195,193,173,0.39)`, `display: flex; flex-direction: column`, no panel padding, no panel scroll. Header: padding 24px 40px, border-bottom 1px `$neutral-100`, title centred (H3 26/40 500 `$neutral-900`), close button `position: absolute; top: 16px; right: 16px; padding: 8px` (replace `right: 5%`). Body wrapper (a new element `.cart-body` around the loading state, the lists and the empty state): `flex: 1; overflow-y: auto; padding: 24px 40px 0; display: flex; flex-direction: column` (M1). There is no bottom padding, so the sticky footer of step 2 touches the panel bottom. The unsubmitted section (`.orders-list` that holds `app-not-submitted-order`) gets `flex: 1 0 auto; display: flex; flex-direction: column`, so with short content the order host can grow and the footer sits at the bottom (M1). The submitted section and the empty state get `padding-bottom: 24px` so they keep space at the end. Section title H4 18/32 500, gap 16 px to the cards; cards gap 16 px. Mobile (≤ 650 px): full width, body `padding: 24px 16px 0`. Keep the backdrop.
   - Check: build passes; record the style size of `cart.component.scss` (4 kB warning, 10 kB error). Manual check with `npm start` against `figma/c2-cart.png` for a single group order: (a) short content (2 dishes) → the footer is at the panel bottom, with empty space above "Grand Total" (M1); (b) long content → the footer stays at the bottom while the cards scroll; (c) the footer border spans the full 640 px width.

5. Submitted orders: hide "Edit Pre-order" for group orders (Q5 b)
   - Type: code
   - Area: frontend
   - Scaffold first: yes (new input and computed)
   - Files: `src/app/components/cart/components/submitted-order/submitted-order.component.ts:20-43`, `.html:52-68`; `src/app/components/cart/cart.component.ts` (new computed, `editOrder` at `:127-145`), `cart.component.html:66-74`
   - Contract:
     - `SubmittedOrderComponent`:
       - New input `readonly orderMode = input.required<CartOrderMode>();` Required, as in step 2 (M3): the parent owns the decision, and a missing binding is a compile error, not a silent "single".
       - New `readonly canEdit = computed(() => this.orderMode() === 'single');` Fail closed: `'group'` and `'unknown'` → `false`.
       - The template renders the "Edit Pre-order" button only when `canEdit()` is true. The "Cancel Pre-order" button renders in every mode, with no change.
       - `editOrder()`: emits `edited` only when `canEdit()` is true; otherwise emits nothing. `cancelOrder()` and `totalPrice` do not change.
     - `CartComponent`:
       - New `readonly submittedOrderViews = computed<CartOrderView[]>(...)`: one item per `submittedOrders()` item, same order, with the same private order → mode mapping as `unsubmittedOrderViews` (step 3). Status 1 and status 3 orders use the same rule.
       - Template: `@for (view of submittedOrderViews(); track view.order.orderId)` → `<app-submitted-order [order]="view.order" [orderMode]="view.mode" (canceled)="openCancelModal(view.order.orderId)" (edited)="editOrder(view.order.orderId)" />`.
       - `editOrder(orderId)`: if the order's mode is not `'single'`, return with no API call. Otherwise today's behaviour (`unsubmitPreOrderByReservationId`). `editOrderModal()` goes through `editOrder`, so it gets the same guard.
     - The submit modal, the cancel modal and `cancelOrder` do not change.
   - Tests (for test-writer):
     - `src/app/components/cart/components/submitted-order/submitted-order.component.spec.ts` (keep the 4 existing tests; `beforeEach` sets `order` and `orderMode: 'single'` with `setInput`; the tests below override the mode with `setInput` + `detectChanges`) — mode 'single': a button with the text "Edit Pre-order" is shown, and its click emits `edited`; mode 'group': no "Edit Pre-order" button; mode 'unknown': no "Edit Pre-order" button; mode 'group': `editOrder()` called directly does not emit `edited`; mode 'unknown': `editOrder()` called directly does not emit `edited`; modes 'single', 'group' and 'unknown': the "Cancel Pre-order" button is shown, and its click emits `canceled`; `totalPrice` is still 20 (existing test).
     - `src/app/components/cart/cart.component.spec.ts` (with the step 3 mocks) — a status 1 order with `guestCount: 1` → "Edit Pre-order" is shown in `app-submitted-order`; a status 1 order with `guestCount: 4` → "Edit Pre-order" is not shown, and "Cancel Pre-order" is shown; a status 3 order with `guestCount: 4` → "Edit Pre-order" is not shown; `getReservationsForUser$` errors and a status 1 order exists → "Edit Pre-order" is not shown, and "Cancel Pre-order" is shown; `editOrder(orderId)` called directly for a group order (status 1, guestCount 4) → `unsubmitPreOrderByReservationId` is not called; `editOrder(orderId)` called directly for a single order (guestCount 1) → `unsubmitPreOrderByReservationId` is called with the order's `reservationId`.
   - Change: as in the contract. Steps 1-4 do not change.
   - Check: both `tsc` gates → 0 errors; the submitted-order spec and the cart spec pass.

### Final verification (after step 5; implementer, then reviewer re-runs)
From `frontend/restaurant-frontend-app`, after `npm ci`:
```bash
npx tsc -p tsconfig.app.json  --noEmit
npx tsc -p tsconfig.spec.json --noEmit
BACKEND_URL=https://example.com npm run build   # must end with "Bundle check passed"
npm test -- --watch=false
```
Report: initial bundle size (1 MB limit), style-budget lines for `cart.component.scss` and `not-submitted-order.component.scss`, test totals (executed / failed).

## Contracts and risks
- **API:** no backend or route change. No `ApiRoutes.md` or `RouteRegistry.cs` change. The cart uses the existing `GET /reservations`.
- **No submit path for a group owner (accepted by the user):** after this change, an owner with `guestCount >= 2` can only Save from the cart. The order stays status 0 until the future "View group pre-order" modal task adds the group submit. Until then, these group pre-orders cannot reach the waiter.
- **Already submitted group orders (Q5 b, step 5):** "Edit Pre-order" is hidden, so a group owner cannot un-submit alone. "Cancel Pre-order" stays. An order in `'unknown'` mode (reservations failed or not matched) also loses Edit until the cart is reopened (fail closed).
- **Extra request:** each cart open adds one `GET /reservations`. For a customer the backend reads each reservation again plus its location, order and group (`GetReservationsForUserHandler.cs:98-148`), and the cart spinner waits for it. Accepted: waiting is safer than showing C1 and then switching to C2.
- **Reservations load fails:** the interceptor shows a global toast (F5). The cart shows the orders with a disabled CTA and the inline message, and with no Edit on submitted orders. The owner can still edit, add and remove dishes and cancel orders. Submit and Edit come back when the cart is reopened and the reservations load.
- **Accepted design deviation (M4):** prices keep the currency pipe format (`$20.00`). Figma shows whole dollars (`$17`, `$27`).
- **Testability:** the backend cannot run locally. Cart behaviour with real data needs the deployed API.
- **Deviations from repo rules:** F8 (follow the OnPush, signal API and `@app/...` style of the existing cart files).
- **Style budget:** both SCSS files can grow past the 4 kB warning budget (the error budget is 10 kB). Report the sizes.
- **Baseline:** `node_modules` is missing (F7). Run `npm ci` first.

## Out of scope
- `reservation-invite-modal.component.ts:20` duplicates the group threshold (`MIN_GUESTS_FOR_INVITE = 2`); it can use `MIN_GROUP_GUEST_COUNT` later.
- The owner "View group pre-order" modal, the group submit, the guest counter and the `tokenInterceptor` change for `/guest/groups/...` (user decision: not in this task, no tech-debt entry for the guest line).
- `ApiRoutes.md` drift: `:1191` documents a group status path and fields that differ from `RouteRegistry.cs:195` and `GroupReservationStatusResponse.cs`; `:1132,1147` document `/guest-sessions/...`, but the code uses `/guest/sessions/...`; the docs show order `status` as a string, but the frontend `Order.status` is a number.
- The dev mock lists the owner in `participants`; the API docs say that the list does not include the owner.
- Frontend `CLAUDE.md` §4-§5 are out of date (Eager, `@Input`, `app/...` imports, `cart/` location).
- `console.error`-only error handling in `CartComponent`.
- The guest cart drawer styles.
