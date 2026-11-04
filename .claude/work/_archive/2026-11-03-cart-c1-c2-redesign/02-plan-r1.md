# Plan r1: Cart redesign, screens C1 and C2

Mode: `--lite`. No explorer report exists, so I collected the facts myself with Grep, Glob, Read and read-only Bash. The evidence is in the "Facts" section.

## Goal
Restyle the header cart (`app-cart`) to Figma C1/C2, and select the footer button from the reservation size: `guestCount < 2` gives C1 "Submit Pre-Order", `guestCount >= 2` gives C2 "Save Pre-Order".

## Facts (my own investigation, with evidence)

### F1. Which cart: the header cart `src/app/components/cart/`
- The Figma frame names are "C1 · Cart — **owner only** (1 guest)" and "C2 · Cart — group reservation". The owner is the authenticated customer. The header cart is the owner's cart: `shared/custom-components/header/header.component.html:63,250` renders `<app-cart>`, and `CartComponent` loads `GET /orders` (Cognito).
- The owner menu page shows the same banner: `pages/menu-page/menu-page.component.html:24` "You are making a pre-order for a reservation", when the query has `preOrder` and `reservationId`.
- `pages/guest-pre-order-page/ui/guest-cart-drawer/` is the cart of an **invited guest** (route `join/:token`, `app.routes.ts:99`). A guest is always in a group, and has no owner/group choice. The C2 footer line "Your guests: X of Y ordered · View group pre-order" is owner data: `GET /guest/groups/{id}/sessions` needs the owner's Cognito token (`ApiRoutes.md:1261-1300`). So C1/C2 do not target the guest drawer.
- The guest drawer already has a layout close to the design ("Grand Total", text `-`/`+` buttons). It is a style reference only. Do not change it.

### F2. Current cart structure
- `cart.component.ts` (OnPush, signal `output()`, `@app/...` imports) loads all orders of the user with `PreOrdersService.getPreOrders()`, and splits them: `unsubmittedOrders` (status 0) and `submittedOrders` (status 1, 3). It can show more than one order.
- Each unsubmitted order is one `app-not-submitted-order` (`not-submitted-order.component.ts`). It renders its own header "Pre-order #id (address, time)", dish rows, an "Add dish" button, "Total payment" and the "Submit Pre-order" button. Inputs/outputs: `order` (required), `submitted: string`, `addDish: void`, `removeDish: string`, `updateQuantity: {itemId, quantity}`.
- Submit flow today: `submitOrder(orderId)` → `POST /orders/{reservationId}/submit` → opens `app-submit-order-modal`.
- Quantity and remove changes already persist at once with `PUT /orders/{reservationId}` (`updateDishQuantity`, `removeDishFromOrder`).

### F3. How to know 1 guest vs group
- `Order` (`models/orders/order.model.ts`) and the API `GET /orders` (`ApiRoutes.md:847`, `GetOrderResponse.cs`) have **no** guest count and no group id. They have `reservationId`.
- `GET /reservations` returns `guestCount` per reservation (`ApiRoutes.md:469-486`). The frontend has it: `ReservationService.getReservationsForUser$(): Observable<Reservation[]>` (`services/reservation/reservation.service.ts:81`), `Reservation.guestCount: number` (`models/reservations/reservation.model.ts:10`).
- Backend rule for "group": a group reservation exists only when `GuestCount >= 2` (`Services/Reservations/ReservationService.cs:72`, `GroupReservationService.cs:132`). The frontend has the same threshold as a private constant `MIN_GUESTS_FOR_INVITE = 2` (`components/reservations/reservation-invite-modal/reservation-invite-modal.component.ts:20`).
- Conclusion: join orders to reservations by `reservationId`. `guestCount >= 2` → C2. No API change is necessary for the button.

### F4. Data for "Your guests: X of Y ordered"
- Group id for the owner: `GET /reservations/{reservationId}/guest-session-id` returns `{"guestSessionId": "owner-<groupReservationId>"}` (documented format, `ApiRoutes.md:557-583`). 404 when the reservation has no group. Side effect: the first call creates the owner session (documented, idempotent after that).
- Guests and their state: `GET /guest/groups/{groupReservationId}/sessions` → `{ participants: [{ id, type, name, items, total, isSubmitted }], total }`. The docs say the list has **guest sessions only** (`ApiRoutes.md:1302`). The dev mock (`shared/mocks/group-orders/group-orders.mock-store.ts:43`) also includes an `Owner` participant, so filter on `type === 'Guest'`.
- Group status: `RouteRegistry.cs:195` has `GET /guest/groups/{id}/status`, and the DTO `GroupReservationStatusResponse.cs` has `currentParticipants` (joined guests + 1), `maxCapacity` (= reservation `GuestCount`), `status`. `ApiRoutes.md:1191` documents a different path (`/group-reservations/{id}/status`) and different fields (`totalGuests`, `submittedGuests`). This is backend doc drift. The code wins.
- Frontend: models exist (`models/group-orders/*.ts`), but **no HTTP service** calls these endpoints. Only the dev mock interceptor uses them (`app.config.ts:25`).
- So X can be computed from data the API gives (guests with `isSubmitted: true`). Y is not decidable: see Q2.

### F5. "View group pre-order" target
- No route or page for an owner group pre-order view exists (`app.routes.ts`; grep for "group pre-order" in `src` finds only the model `submit-group-pre-order.model.ts`). The backend endpoint for the final group submit exists (`POST /guest/groups/{id}/submit`), but no frontend uses it. The link has no target today. See Q3.

### F6. "Save Pre-Order" semantics
- There is no "save" endpoint. The owner's dishes already persist with `PUT /orders/{reservationId}` on each change (F2). The order stays `Created` (status 0) until a submit.
- The final group submit `POST /guest/groups/{id}/submit` merges the submitted guest sessions into the owner's order and sets `SubmittedByCustomer` (`GroupPreOrderService.cs:53-57`). It accepts the order in any status.
- If "Save" calls `POST /orders/{id}/submit` (like C1), the order moves to the "Submitted pre-orders" section, and the C2 footer (guest counter) is not shown any more. That contradicts the word "Save". See Q1.

### F7. Chevron asset
- `assets/svg/arrow-right.svg` is a 16x16 chevron-right (`path d="M6 12L10 8L6 4"`, stroke `#232323`, width 1.17). It matches the Figma 16 px chevron. Reuse it. No new asset.

### F8. Baseline
- `node_modules` is not installed, so `npx tsc --noEmit` fails on every Angular import (TS2307). The implementer must run `npm ci` before the gates. I did not run it (it writes files). There is no baseline for test totals from this run.

## Open questions
- **Q1 (behaviour, blocks step 3).** What does C2 "Save Pre-Order" do?
  - (a) The same call as C1 (`POST /orders/{reservationId}/submit` + the submit modal). Only the label changes.
  - (b) **Recommended.** No API call: the dishes are already saved (F6). The order stays unsubmitted (status 0), so the owner can still edit it and the group submit can happen later. The cart shows the toast "Your pre-order is saved." and closes.
  - (c) Call the group submit `POST /guest/groups/{id}/submit`. Not recommended: that is the final submit for the whole group, and it belongs on the "group pre-order" view.
  The plan uses (b). The decision lives in one method (`CartComponent.saveOrder`), so (a) is a one-method change.
- **Q2 (data, blocks step 5).** In "Your guests: X of Y ordered", what is Y? Recommended: Y = `guestCount - 1` (the guests without the owner), X = guests with `isSubmitted: true`. Alternative: Y = `guestCount`. The Figma sample "3 of 5" does not decide it.
- **Q3 (scope, blocks step 5).** "View group pre-order" has no page and no route (F5). Options:
  - (a) **Recommended.** Show "Your guests: X of Y ordered" only. Do not show the link and chevron until a group pre-order page exists. Record the follow-up in `docs/tech-debt.md`.
  - (b) Skip step 5 completely (no guest line). C2 then differs from C1 only in the label.
  - (c) Build the group pre-order page. That is a separate task. Not in this plan.
- **Q4 (design gap, step 2).** The design shows no reservation label ("Pre-order #id (address, time)") and no "Add dish" button. The cart can hold more than one unsubmitted order (F2), so without the label two orders look the same. Recommended: keep the reservation label as a caption line (`address, time`, no order id) under each order, and remove the "Add dish" button (subtract, the design does not show it). Say if the "Add dish" button must stay.

## Steps

1. Pure helper for the group rule
   - Type: code
   - Area: frontend
   - Scaffold first: yes
   - Files: new `frontend/restaurant-frontend-app/src/app/shared/helpers/group-reservation.helpers.ts`
   - Contract:
     - `export const MIN_GROUP_GUEST_COUNT = 2;`
     - `export function isGroupReservation(guestCount: number): boolean` → `guestCount >= MIN_GROUP_GUEST_COUNT`.
   - Tests (for test-writer): `src/app/shared/helpers/group-reservation.helpers.spec.ts` — table-driven: 0 → false; 1 → false; 2 → true; 6 → true; `MIN_GROUP_GUEST_COUNT` is 2.
   - Change: add the file. Do not change `reservation-invite-modal` in this task (see Out of scope).
   - Check: `npx tsc -p tsconfig.spec.json --noEmit` → 0 errors; the new spec passes in `npm test -- --watch=false`.

2. Order card: C1/C2 footer button and card restyle
   - Type: code
   - Area: frontend
   - Scaffold first: yes (new input and output)
   - Files: `src/app/components/cart/components/not-submitted-order/not-submitted-order.component.ts:20-75`, `.html:1-108`, `.scss:1-105`
   - Contract (keep the existing signal-API style of this file):
     - New input `readonly isGroup = input(false);` (boolean).
     - New output `readonly saved = output<string>();` emits `order().orderId`.
     - New `readonly ctaLabel = computed(() => this.isGroup() ? 'Save Pre-Order' : 'Submit Pre-Order');`
     - New method `onCtaClick(): void` — `isGroup()` false → `submitted.emit(orderId)`; true → `saved.emit(orderId)`. Exactly one output fires per click.
     - Keep `submit()`, `submitted`, `removeDish`, `updateQuantity`, `incrementItemQuantity`, `decrementItemQuantity`, `totalPrice` with the same behaviour. Remove `addDish` output and `addDishToOrder()` (Q4 default). If the user keeps "Add dish" (Q4), keep them.
     - DOM hooks for tests: CTA is `<button type="button" class="cart-cta">` with the text `ctaLabel()`. Total row: label text "Grand Total", value in `<span class="grand-total-value">` with `totalPrice() | currency`. Quantity buttons are text buttons `-` / `+` with `aria-label="Decrease quantity"` / `"Increase quantity"`; the decrease button is `disabled` when quantity ≤ 1. Remove button keeps `aria-label="Remove <dish name>"`. Reservation caption `<span class="order-reservation">{{ address }}, {{ time }}</span>` (Q4).
   - Tests (for test-writer): `not-submitted-order.component.spec.ts` (replace and extend the current 3 tests) — default (`isGroup` not set): `.cart-cta` text is "Submit Pre-Order"; clicking it emits `submitted` with 'order-1' and does not emit `saved`; with `isGroup` true: text is "Save Pre-Order"; click emits `saved` with 'order-1' and does not emit `submitted`; `.grand-total-value` shows `$20.00` for 2 × 10 and the page has the text "Grand Total"; "Decrease quantity" is disabled at quantity 1 and enabled at 2; clicking "Increase quantity" emits `updateQuantity` `{ itemId: 'dish-1', quantity: 3 }` for quantity 2; clicking "Remove …" emits `removeDish` with 'dish-1'; `.order-reservation` contains the reservation address.
   - Change: template per Figma card (00-task.md "Pre-order card", "Footer"): 96 px round image, name + caption "Expected ready time - 10 minutes", quantity row with bordered `-` (grey `$neutral-400`, border `$neutral-200`) and `+` (`$green-600`), price `$green-600` 18/32, absolute trash button top 8 right 8. Footer block with "Grand Total" and the CTA (56 px, radius 10, gradient `linear-gradient(-83.73deg, #0F7A37 0%, #0B5B29 98.29%)`, Poppins 600 16/24 white). Footer: `position: sticky; bottom: 0; background: color.$neutral-0; border-top: 1px solid color.$neutral-100`, so it stays at the panel bottom while the order is in view. Use tokens from `shared/styles/abstracts/colors` (`@use`). Remove the order header "Pre-order #id" and the "Total payment" row.
   - Check: both `tsc` gates → 0 errors; spec passes; build prints the style-budget line for this file (4 kB warning budget, record the size).

3. Cart: load guest counts, pass `isGroup`, handle Save
   - Type: code
   - Area: frontend
   - Scaffold first: yes (new public members)
   - Files: `src/app/components/cart/cart.component.ts:38-263`, `cart.component.html:35-53`, new `src/app/components/cart/models.ts`
   - Contract:
     - `models.ts`: `export interface CartOrderView { order: Order; isGroup: boolean; }`
     - New injections: `ReservationService` (`getReservationsForUser$`), `ToastService` (`success`).
     - `readonly reservationGuestCounts = signal<ReadonlyMap<string, number>>(new Map())` (key: reservation `id`).
     - `readonly unsubmittedOrderViews = computed<CartOrderView[]>(...)` — one item per `unsubmittedOrders()` item, same order. `isGroup = isGroupReservation(count)` when the map has `order.reservationId`, else `false`.
     - Initial load (`ngOnInit`): one request to `getPreOrders()` and one to `getReservationsForUser$()`, joined (for example `forkJoin`). `isLoadingOrders` is true until both complete. A reservations error does not hide the orders: the map stays empty, so every order shows C1. An orders error keeps today's behaviour (`orders` = []).
     - Later reloads (after edit, remove, cancel, submit, unsubmit) call only `getPreOrders()`. `getReservationsForUser$` is called one time per cart open.
     - New `saveOrder(orderId: string): void` (Q1 default b): no `PreOrdersService` call; `toast.success('Your pre-order is saved.')`; `closeCart()`. Unknown `orderId` → no toast, no close.
     - Template: the unsubmitted section header becomes `<h4>My Pre-Order</h4>` (remove "Not submitted pre-orders" and "You have not submitted pre-orders"). `@for (view of unsubmittedOrderViews(); track view.order.orderId)` → `<app-not-submitted-order [order]="view.order" [isGroup]="view.isGroup" (submitted)="submitOrder($event)" (saved)="saveOrder($event)" .../>`. Remove the `(addDish)` binding if step 2 removed the output.
     - The "Submitted pre-orders" section and the empty state do not change.
   - Tests (for test-writer): `src/app/components/cart/cart.component.spec.ts` (keep the 5 existing tests; add mocks for `ReservationService` and `ToastService`) — an unsubmitted order whose reservation has `guestCount: 1` renders `.cart-cta` "Submit Pre-Order"; `guestCount: 4` renders "Save Pre-Order"; an order whose `reservationId` is not in the reservations list renders "Submit Pre-Order"; `getReservationsForUser$` returns an error → the order still renders, with "Submit Pre-Order"; the page shows the heading "My Pre-Order" when there is an unsubmitted order; clicking the Submit CTA (group size 1) calls `submitPreOrderByReservationId` with the order's `reservationId`; clicking the Save CTA (group size 4) calls `toast.success` with 'Your pre-order is saved.', emits `closeCartEvent`, and calls neither `submitPreOrderByReservationId` nor `editPreOrderByReservationId`; after a successful `updateDishQuantity`, `getPreOrders` was called 2 times and `getReservationsForUser$` 1 time.
   - Change: as in the contract. Import `isGroupReservation` from step 1.
   - Check: both `tsc` gates → 0 errors; cart spec passes.

4. Cart panel layout and styles
   - Type: code
   - Area: frontend
   - Scaffold first: no
   - Files: `src/app/components/cart/cart.component.scss:1-116`, `cart.component.html:10-30`
   - Contract: none new. Presentational only.
   - Tests (for test-writer): none: presentational SCSS/HTML change; the behaviour tests of steps 2-3 cover the DOM that this step keeps (`.cart-cta`, heading, close button `aria-label="Close cart"`).
   - Change: panel `width: min(640px, 100vw)`, full height, `box-shadow: 0 0 5px rgba(195,193,173,0.39)`, column layout, no padding on the panel. Header: padding 24px 40px, border-bottom 1px `$neutral-100`, title centered (H3 26/40 500 `$neutral-900`), close button `position: absolute; top: 16px; right: 16px; padding: 8px` (replace `right: 5%`). Body: `flex: 1; overflow-y: auto; padding: 24px 40px`, so the sticky order footer (step 2) sticks to the body bottom. Section title H4 18/32 500, gap 16 px to the cards; cards gap 16 px. Keep the mobile rule (≤ 650 px: full width, 24px 16px padding). Keep the backdrop. Remove the empty duplicate blank lines and unused `.list` rule only if nothing uses it (grep first).
   - Check: build passes; record the style size of `cart.component.scss` from the build output (budget 4 kB warning, 10 kB error). Manual check with `npm start` against the Figma PNG (`figma/c2-cart.png`): 640 px panel, sticky footer.

5. C2 guest counter (only if the user answers Q2 and chooses Q3 (a))
   - 5a. Group-orders HTTP service and summary helper
     - Type: code
     - Area: frontend
     - Scaffold first: yes
     - Files: new `src/app/services/group-orders/group-orders.service.ts`, new `src/app/shared/helpers/group-guests-summary.helpers.ts`, new `src/app/models/group-orders/group-guests-summary.model.ts`
     - Contract:
       - `GroupOrdersService` (`@Injectable({ providedIn: 'root' })`, `inject(HttpClient)`, `inject(API_BASE_URL)`):
         - `getGroupReservationId$(reservationId: string): Observable<string>` → `GET {base}/reservations/{encodeURIComponent(reservationId)}/guest-session-id`, typed `OwnerSessionResponse`; maps `guestSessionId` without the `owner-` prefix. A value without the prefix → the observable errors with `Error('Unexpected owner session id format')`. HTTP errors pass through.
         - `getGroupSessions$(groupReservationId: string): Observable<GroupSessionsResponse>` → `GET {base}/guest/groups/{encodeURIComponent(groupReservationId)}/sessions`.
       - `export interface GroupGuestsSummary { ordered: number; total: number; }`
       - `export function toGroupGuestsSummary(guestCount: number, participants: Participant[]): GroupGuestsSummary` → `ordered` = count of participants with `type === 'Guest' && isSubmitted`; `total` = `Math.max(guestCount - 1, 0)` (Q2 default; if the user picks Y = guestCount, `total = guestCount`).
     - Tests (for test-writer): `src/app/services/group-orders/group-orders.service.spec.ts` with `provideHttpClient()`, `provideHttpClientTesting()`, `{ provide: API_BASE_URL, useValue: 'https://api.test' }` — `getGroupReservationId$('res-1')` sends GET to `https://api.test/reservations/res-1/guest-session-id` and emits 'group-1' for `{ guestSessionId: 'owner-group-1' }`; emits an error for `{ guestSessionId: 'group-1' }`; passes a 404 through as an error; `getGroupSessions$('group-1')` sends GET to `https://api.test/guest/groups/group-1/sessions` and emits the body; `httpMock.verify()` in `afterEach`. `src/app/shared/helpers/group-guests-summary.helpers.spec.ts` — table: guestCount 5, 2 submitted guests + 1 not submitted → `{ ordered: 2, total: 4 }`; an `Owner` participant with `isSubmitted: true` is not counted; empty list → `{ ordered: 0, total: guestCount - 1 }`; guestCount 1 → total 0.
     - Change: add the files.
     - Check: both `tsc` gates → 0 errors; the two new specs pass.
   - 5b. Render the counter in C2
     - Type: code
     - Area: frontend
     - Scaffold first: yes
     - Files: `not-submitted-order.component.ts/.html/.scss`, `cart.component.ts/.html`, `components/cart/models.ts`
     - Contract:
       - `CartOrderView` gets `guestsSummary: GroupGuestsSummary | null`.
       - `NotSubmittedOrderComponent`: new input `readonly guestsSummary = input<GroupGuestsSummary | null>(null)`. When `isGroup()` and `guestsSummary()` is not null, it renders `<p class="guests-summary">Your guests: {ordered} of {total} ordered</p>` under the CTA (Caption 12/16 400 `$neutral-600`). No "View group pre-order" link (Q3 a). Otherwise the element is absent.
       - `CartComponent`: `readonly guestsSummaries = signal<ReadonlyMap<string, GroupGuestsSummary>>(new Map())` (key: `reservationId`). After the initial load, for each unsubmitted group order: `getGroupReservationId$` then `getGroupSessions$`, then `toGroupGuestsSummary(guestCount, participants)`. One fetch per group reservation per cart open. Any error for one reservation → no entry for it (line hidden), no toast, other orders not affected.
     - Tests (for test-writer): `not-submitted-order.component.spec.ts` — `isGroup` true and summary `{ ordered: 3, total: 5 }` → `.guests-summary` text "Your guests: 3 of 5 ordered"; `isGroup` true and summary null → no `.guests-summary`; `isGroup` false and a summary → no `.guests-summary`. `cart.component.spec.ts` (add a `GroupOrdersService` mock) — group order (guestCount 6), mocks return 'group-1' and 2 submitted guests → `.guests-summary` "Your guests: 2 of 5 ordered"; `getGroupReservationId$` errors → no `.guests-summary`, the CTA "Save Pre-Order" still renders; a single order (guestCount 1) → `GroupOrdersService` is not called.
     - Change: as in the contract.
     - Check: both `tsc` gates → 0 errors; all specs pass.

### Final verification (after the last step; implementer, then reviewer re-runs)
From `frontend/restaurant-frontend-app`, after `npm ci`:
```bash
npx tsc -p tsconfig.app.json  --noEmit
npx tsc -p tsconfig.spec.json --noEmit
BACKEND_URL=https://example.com npm run build   # must end with "Bundle check passed"
npm test -- --watch=false
```
Report: initial bundle size (limit 1 MB), style-budget lines for `cart.component.scss` and `not-submitted-order.component.scss`, test totals (executed / failed).

## Contracts and risks
- **API:** no backend or route change. Steps 1-4 use `GET /reservations` (existing service). Step 5 adds the first frontend callers of `GET /reservations/{id}/guest-session-id` and `GET /guest/groups/{id}/sessions` (both in `RouteRegistry.cs:198,200` and `ApiRoutes.md`). No `ApiRoutes.md` change is needed.
- **Side effect (step 5):** the first `guest-session-id` call creates the owner session in DynamoDB (documented, idempotent). Opening the cart then writes once per group reservation.
- **Fragile format (step 5):** the group id comes from stripping `owner-`. The format is documented (`ApiRoutes.md:566`), and the service fails loudly if it changes.
- **Extra request:** each cart open adds one `GET /reservations` (steps 1-4), plus 2 requests per unsubmitted group order (step 5).
- **Fallback:** if reservations fail to load, every order shows C1 "Submit". That is today's behaviour, so no regression, but a group owner could then submit through the individual flow. The group submit still works after that (it accepts any order status, F6).
- **Q1 (b) risk:** "Save" does no server call. If the user expects a server action, choose Q1 (a).
- **Testability:** the backend cannot run locally. Real behaviour of the group endpoints needs a deployed API. The dev mock interceptor serves only the `demo-reservation` ids.
- **Deviations from repo rules (follow the code, precedence 5):**
  - Frontend `CLAUDE.md` §5 says components use `ChangeDetectionStrategy.Eager` and decorator `@Input()`, and that `app/...` imports do not resolve. The cart files already use `OnPush`, signal `input()`/`output()` and `@app/...` imports (`tsconfig.json` has `paths: { "@app/*": ["src/app/*"] }`). The plan keeps the style of the files it changes.
  - The steps add `@app/...` imports only in files that already use them, and in new files next to them.
- **Style budget:** both SCSS files can grow past the 4 kB warning budget. The error budget is 10 kB. Report the sizes.
- **Baseline:** `node_modules` is missing (F8). Run `npm ci` before the gates.

## Out of scope
- `reservation-invite-modal.component.ts:20` duplicates the group threshold (`MIN_GUESTS_FOR_INVITE = 2`). It can use `MIN_GROUP_GUEST_COUNT` later (one decision, one place). Record in `docs/tech-debt.md`.
- Owner "group pre-order" page and the "View group pre-order" link (Q3).
- `ApiRoutes.md:1191` documents `GET /group-reservations/{id}/status` with `totalGuests`/`submittedGuests`; the code is `GET /guest/groups/{id}/status` with `currentParticipants`/`maxCapacity`/`status`. Backend doc drift.
- `ApiRoutes.md:1132,1147` document `/guest-sessions/{id}/dishes`; `RouteRegistry.cs:191-192` use `/guest/sessions/{id}/dishes`. Backend doc drift.
- `ApiRoutes.md` shows order `status` as a string ("Created"); the frontend `Order.status` is a number and the cart filters on 0/1/3. Not checked against a live API in this task.
- The dev mock `group-orders.mock-store.ts` lists the owner in `participants`; the API docs say it does not.
- Frontend `CLAUDE.md` §5 and §4 are out of date (Eager count, `@Input` count, `app/...` imports, `cart/` folder location). Record in `docs/tech-debt.md`.
- `console.error`-only error handling in `CartComponent` (CLAUDE.md §7 wants toasts). Existing debt, not this task.
- The guest cart drawer (`guest-cart-drawer`) styles.
