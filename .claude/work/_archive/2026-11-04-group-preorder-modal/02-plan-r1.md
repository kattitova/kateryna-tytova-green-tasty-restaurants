# 02 — Plan r1: Group Pre-Order modal (owner)

Planner inputs: `00-task.md` (D1–D6), `01-explore.md`, Figma PNGs M00–M11 and `figma/M02-design-context.tsx.txt`. I opened each file that the plan changes. Paths: `BE` = `backend/restaurant-backend-app/dnapp/lambdas/api-handler/`, `BT` = `backend/restaurant-backend-app/Tests/`, `FE` = `frontend/restaurant-frontend-app/src/app/`.

## Goal

The group owner opens a "Group Pre-Order" modal from the reservation card. The modal shows the seats, the owner's and the guests' pre-orders, the totals, and the group bonus that the backend computes. The owner can copy the invite link, remove a guest, edit the owner's own dishes and submit the group pre-order. The backend accepts a submit that has at least one order.

## Open questions

All are non-blocking. The plan uses the default in brackets.

- Q1. Owner pre-order time for the bonus: the time when the owner's order was **created**. A later edit does not move it. [default: creation time. Reason: an edit by the owner must not lose the bonus for the whole table.]
- Q2. Records saved before the deploy have no `createdAt` / `submittedAt`. [default: a missing time counts as "on time" (it does not move the last pre-order time). Only old test data has this.]
- Q3. Submit when every seat has ordered: no confirm dialog, submit at once. [default: yes. M09 shows the dialog only for empty seats.]
- Q4. `GET /guest/groups/{id}/status` is public (no authorizer). It will also return `orderedCount` and `bonus` (no personal data). [default: yes. Guests can use it later.]
- Q5. Card buttons for the customer role become `button--small` in every status (M00 shows small buttons, also "Leave Feedback"). The waiter card does not change. [default: yes.]
- Q6. Group submit keeps today's rules for time and reservation status (no 30-minute cutoff, no `Reserved` check). [default: no change; out of scope.]

## Data shape first (decisions that every step uses)

1. **Seats** = `Reservation.GuestCount` (owner included) = `maxCapacity` in the status response.
2. **Owner has ordered** = the owner's *own* items are not empty. Own items = active order items that are not `Canceled`. After a group submit, the order also holds the guests' dishes (`MergeIntoOrderItems`), so own items = order items minus the consolidated quantities of the submitted guest sessions, per dish id (items with quantity ≤ 0 are dropped). One pure function owns this rule (`GroupPreOrderRules.OwnerOwnItems`). It makes "no duplication" true for the totals, the ordered count and the bonus. An order that the group submit created (owner had no order) has no own items, so the owner does not count as ordered.
3. **Guest has ordered** = `GuestSession.IsSubmitted` (same rule as the submit). A guest with items but not submitted is "Waiting" and is not in the totals (US1 AC3).
4. **Ordered count** = (owner has ordered ? 1 : 0) + submitted guest sessions (owner session excluded).
5. **Last pre-order time** = latest of: owner `Order.CreatedAt` (only if the owner has ordered) and `GuestSession.SubmittedAt` of the submitted guests. Null values are skipped (Q2).
6. **Bonus** (pure function `GroupBonusCalculator.Calculate`, all times UTC, `start` = reservation start):
   - All seats ordered (`orderedCount >= seatCount`): last time ≤ `start − 24h` → `Achieved` + `TenPercentDiscount` (10 %). Else last time ≤ `start − 2h` → `Achieved` + `FreeDessert`. Else → `Missed`. Last time null → treat as on time (`TenPercentDiscount`).
   - Not all ordered and group is not `Open` (submitted or closed) → `Missed`.
   - Not all ordered, group `Open`: `now ≤ start − 24h` → `InProgress` + `TenPercentDiscount`, deadline `start − 24h`. Else `now ≤ start − 2h` → `InProgress` + `FreeDessert`, deadline `start − 2h`. Else → `Missed`.
   - Boundaries are inclusive: exactly 24 h before gives 10 %; exactly 2 h before gives the dessert.
   - `Missed` → `tier: null`, `discountPercent: 0`, `deadline: start − 2h`. `Achieved` → `deadline: null`. `discountPercent` = 10 for `TenPercentDiscount`, else 0 (also while `InProgress`, for "−10 % unlocks at 6 of 6").
7. **Money**: the backend does not store a discount on the order (no field, D1 says the frontend shows the totals). The frontend computes `grandTotal` = owner subtotal (if ordered) + submitted guests' subtotals; `discount` = round2(grandTotal × discountPercent / 100) only when the bonus is `Achieved`; `final` = grandTotal − discount.
8. **Clock**: the backend code uses `DateTime.UtcNow` everywhere (no `TimeProvider`). The plan follows the code: the calculator is pure and takes `nowUtc`; services pass `DateTime.UtcNow`. Boundary tests run on the pure calculator.

## Steps

### Backend

1. **BE-1 Pure group rules: bonus calculator, owner own items, shared consolidation**
   - Type: code
   - Area: backend
   - Scaffold first: yes
   - Files: new `BE/Services/GroupReservations/GroupBonusCalculator.cs`, new `BE/Services/GroupReservations/GroupPreOrderRules.cs`, new `BE/Models/GuestSessions/GroupBonusResponse.cs`, `BE/Services/GroupReservations/GroupPreOrderService.cs:78-99` (move `Consolidate` out).
   - Contract:
     ```csharp
     namespace APIHandler.Models.GuestSessions;
     public enum GroupBonusState { InProgress, Achieved, Missed }
     public enum GroupBonusTier { TenPercentDiscount, FreeDessert }
     public class GroupBonusResponse
     {
         [JsonPropertyName("state")] /* string enum */ public GroupBonusState State { get; init; }
         [JsonPropertyName("tier")] /* string enum or null */ public GroupBonusTier? Tier { get; init; }
         [JsonPropertyName("discountPercent")] public int DiscountPercent { get; init; }
         [JsonPropertyName("deadline")] public DateTime? Deadline { get; init; } // Kind = Utc, serialized with "Z"
     }

     namespace APIHandler.Services.GroupReservations;
     public static class GroupBonusCalculator
     {
         public const int DiscountPercent = 10;
         public static readonly TimeSpan DiscountLeadTime = TimeSpan.FromHours(24);
         public static readonly TimeSpan DessertLeadTime = TimeSpan.FromHours(2);
         public static GroupBonusResponse Calculate(
             int seatCount, int orderedCount, DateTime? lastOrderedAtUtc,
             DateTime reservationStartUtc, DateTime nowUtc, bool groupIsOpen);
     }
     public static class GroupPreOrderRules
     {
         // Moved from GroupPreOrderService. Sums quantities by dish id, keeps first-appearance order.
         public static IReadOnlyList<GroupPreOrderDishResponse> Consolidate(IEnumerable<GuestSession> sessions);
         // Rule "Data shape" 2. Returns new OrderItem objects; does not change `order`.
         public static IReadOnlyList<OrderItem> OwnerOwnItems(
             Order? order, IEnumerable<GuestSession> sessions, GroupReservationStatus groupStatus);
     }
     ```
     Rules: see "Data shape first" 2 and 6. `OwnerOwnItems`: `order` null → empty; `Canceled` items excluded; only for `groupStatus == Submitted` subtract `Consolidate(sessions.Where(s => s.IsSubmitted))`; item order kept. No exceptions.
   - Tests (for test-writer): new `BT/Services/GroupBonusCalculatorTests.cs` — use `DateTimeKind.Utc` values; [Theory] rows: all ordered, last = start−24h → Achieved/TenPercent/10; last = start−24h+1 min → Achieved/FreeDessert/0; last = start−2h → Achieved/FreeDessert; last = start−2h+1 min → Missed; last null → Achieved/TenPercent. [Theory] rows not all ordered, open: now = start−24h → InProgress/TenPercent, deadline start−24h; now = start−24h+1 min → InProgress/FreeDessert, deadline start−2h; now = start−2h+1 min → Missed, tier null, deadline start−2h. One [Fact]: not all ordered, group not open, now = start−3 days → Missed. One [Fact]: JSON of a `GroupBonusResponse` (System.Text.Json default options) has `"state":"InProgress"`, `"tier":"TenPercentDiscount"`, `"discountPercent":10`, a `"deadline"` ending with `Z`; and `"tier":null` for `Missed`. New `BT/Services/GroupPreOrderRulesTests.cs` — Open group returns order items as they are, without `Canceled` items; Submitted group subtracts submitted guests' quantities and drops a dish that only guests ordered; null order → empty. Existing `GroupPreOrderServiceTests` pin the move of `Consolidate` (no change).
   - Change: add the types; move `Consolidate` to `GroupPreOrderRules` and call it from `GroupPreOrderService`; implement `Calculate` and `OwnerOwnItems`.
   - Check: `dotnet build restaurant-backend-app.sln --no-incremental` → 0 errors, 0 new warnings in changed files; `dotnet test Tests/APIHandler.Tests.csproj` → 0 failed.

2. **BE-2 Timestamps, group status with bonus, owner in the sessions snapshot**
   - Type: code
   - Area: backend
   - Scaffold first: yes (entity properties, response fields, constructor parameter). Planned break: after the scaffold, `BT/Services/GuestSessionServiceTests.cs:35` and the 3 `new GetAllSessionsResponse(...)` calls (`BT/Handlers/GetGroupSessionsHandlerTests.cs:58`, `BT/Handlers/DeleteGuestSessionHandlerTests.cs:67,262`) do not compile. The test-writer fixes them first.
   - Files: `BE/Entities/Order.cs`, `BE/Entities/GuestSession.cs`, `BE/Services/Orders/OrderService.cs:89-123`, `BE/Services/GuestSessions/GuestSessionService.cs:27-33,157-173,176-244,246-257`, `BE/Models/GuestSessions/GroupReservationStatusResponse.cs`, `BE/Models/GuestSessions/GetAllResponse.cs:7`. No change to `RouteRegistry.cs` (DI resolves the new constructor parameter; `IOrderRepository` is already registered).
   - Contract:
     - `Order.CreatedAt`: `DateTime?`, `[DynamoDBProperty("createdAt")]`, `[JsonPropertyName("createdAt")]`. `OrderService.AddOrderAsync` and `AddOrderForWaiterAsync` set it to `DateTime.UtcNow` before save. Edits, submit, unsubmit and cancel do not change it.
     - `GuestSession.SubmittedAt`: `DateTime?`, `[DynamoDBProperty("submittedAt")]`. `GuestSessionService.SubmitCartAsync` sets it to `DateTime.UtcNow` together with `IsSubmitted = true`.
     - `GuestSessionService` constructor gets a new last parameter `IOrderRepository orderRepository`.
     - `GroupReservationStatusResponse` adds `[JsonPropertyName("orderedCount")] int OrderedCount` and `[JsonPropertyName("bonus")] GroupBonusResponse Bonus` (never null). Existing fields do not change. `GetGroupReservationStatusAsync(groupReservationId)`: loads the active order with `orderRepository.GetOrderByReservationIdAsync(group.ReservationId)`, computes "Data shape" 4–6, calls `GroupBonusCalculator.Calculate(reservation.GuestCount, orderedCount, last, reservation.StartTime.ToUniversalTime(), DateTime.UtcNow, group.Status == Open)`. Errors unchanged (404 group not found).
     - `GetAllSessionsResponse(IEnumerable<Participant> Participants, decimal Total, Participant Owner)`. `Participants` and `Total` keep today's meaning (guests only; total of all guests). `Owner` = `Participant(Id: "owner-{groupReservationId}", Type: Owner, Name: "Owner", Items: OwnerOwnItems mapped to `OrderItemResponse` (dishes that do not exist are skipped, as for guests), Total: sum of price × quantity, IsSubmitted: own items not empty)`. Both `GetGroupSessionsAsync` and `DeleteGuestSessionAsync` return it. Owner dish ids go into the same `GetDishesByIdsAsync` call. Errors unchanged (404, 403, 400 for delete).
   - Tests (for test-writer): `BT/Services/OrderServiceTests.cs` — `AddOrderAsync` saves the order with `CreatedAt` between the UTC time before and after the call. `BT/Services/GuestSessionServiceTests.cs` — `SubmitCartAsync` saves `IsSubmitted = true` and `SubmittedAt` in the same window; status: owner order with own items + 2 of 3 guests submitted, seats 4 → `OrderedCount = 3`, bonus `InProgress` (reservation start = UtcNow + 3 days); status: all 4 seats ordered with times 2 days before a start 3 days ahead → `Achieved` + `TenPercentDiscount`; status: group `Submitted`, order holds only the guests' dishes (owner had no order) → owner not counted; sessions: `Owner` has own items, total and `IsSubmitted = true`; sessions with no order → `Owner.Items` empty, `Total = 0`, `IsSubmitted = false`. Update the constructor call and the 3 `GetAllSessionsResponse` calls. No new handler tests.
   - Change: add the fields; set the times; add the owner order lookup and the owner participant; replace `BuildGuestSnapshotAsync` with one builder that returns guests and owner.
   - Check: as BE-1.

3. **BE-3 Group submit accepts at least one order and creates the owner's order if missing (D6)**
   - Type: code
   - Area: backend
   - Scaffold first: yes (constructor parameter, mapper method). Planned break: `BT/Services/GroupPreOrderServiceTests.cs:30` does not compile until the test-writer adds the new mock.
   - Files: `BE/Services/GroupReservations/GroupPreOrderService.cs:16-65`, `BE/Mappers/OrderMapper.cs:30-42`, `BE/Handlers/Order/CreateOrderHandler.cs:159` (use the mapper method; no behaviour change).
   - Contract:
     - Constructor: new last parameter `ILocationService locationService` (already registered, `RouteRegistry.cs:107`).
     - `OrderMapper.FormatReservationTime(DateTime start, DateTime end)` returns `$"{start:MMM dd, yyyy, h:mm tt} - {end:h:mm tt}"` (the string from `CreateOrderHandler.cs:159`).
     - `SubmitGroupPreOrderAsync(userId, groupReservationId, reservationId)` → `IReadOnlyList<GroupPreOrderDishResponse>`:
       1. Unchanged checks and errors: reservation missing → `ReservationNotFoundException` (404); caller not owner → `ActionForbiddenException` (403); group missing or of another reservation → `GroupReservationNotFoundException` (404); group `Submitted` → `GroupReservationSubmittedException`, `Closed` → `GroupReservationExpiredException` (400).
       2. `guestDishes` = `GroupPreOrderRules.Consolidate(sessions.Where(s => s.IsSubmitted))` (can be empty).
       3. `order` = active order. Owner has items = `order?.Dishes` is not empty.
       4. `guestDishes` empty and owner has no items → `ValidationException("Nothing to submit: the group has no pre-orders.")` (400). No transaction call.
       5. `order` null → new `Order { OrderId = new Guid string, CustomerId = userId, ReservationId, ReservationAddress = (await locationService.GetLocationByTableIdAsync(reservation.TableId)).Address, ReservationTime = OrderMapper.FormatReservationTime(reservation.StartTime, reservation.EndTime), Dishes = guestDishes as Pending items, Status = SubmittedByCustomer, CreatedAt = DateTime.UtcNow }`. Location lookup only in this case.
       6. `order` exists → today's merge (D5/D5b) and `Status = SubmittedByCustomer`. The owner's items are never added a second time.
       7. One `SubmitGroupPreOrderAsync(groupId, order)` call; `false` → `GroupReservationSubmittedException` (unchanged).
       8. Returns `guestDishes` (can be `[]`). HTTP 200.
     - Removed: the errors "No submitted guest sessions…", "Submitted guest sessions contain no dishes." and "Active pre-order … not found".
   - Tests (for test-writer): `BT/Services/GroupPreOrderServiceTests.cs` — owner order with items and no submitted guest → one transaction with the same items, status `SubmittedByCustomer`, returns empty list; no order and 2 submitted guests → one transaction with a new order (`CustomerId`, `ReservationId`, address from the location mock, `ReservationTime` = formatted string, consolidated `Pending` items, `SubmittedByCustomer`), returns the consolidated dishes; no order and no submitted guest dishes → `ValidationException` with the new message, no transaction. Rewrite the existing tests that expect the removed errors (`NoSessions`, `OnlyUnsubmittedSessions`, `SubmittedSessionsWithoutDishes`, `DishesOnlyInUnsubmittedSessions`, `OrderNotFound`) to the new rules; delete a test when a new test already covers its case.
   - Change: as in the contract.
   - Check: as BE-1. Also `SubmitGroupPreOrderHandlerTests` stay green (handler unchanged).

4. **BE-4 API contract docs**
   - Type: docs
   - Area: backend
   - Scaffold first: no
   - Files: `backend/restaurant-backend-app/docs/ApiRoutes.md:1198-1206` (status), `:1208-1267` (submit), `:1268-1313` (sessions), `:1314-` (delete); `backend/restaurant-backend-app/docs/decisions/0002-owner-session-and-group-submit.md` (short dated amendment for D6 and the owner own-items rule).
   - Contract: fix the status section to the code path `GET /guest/groups/{groupReservationId}/status`, fields `currentParticipants`, `maxCapacity`, `status`, `orderedCount`, `bonus {state, tier, discountPercent, deadline}` with the rules from "Data shape" 6 and an example. Sessions and delete: add `owner`. Submit: new preconditions, the new 400 message, owner order creation, response may be `[]`. Copy every key, status and message from the code.
   - Tests (for test-writer): none: docs step.
   - Change: edit only these sections.
   - Check: `python .claude/skills/asd-ste100/scripts/ste-lint.py backend/restaurant-backend-app/docs/ApiRoutes.md` and the ADR file; the code-reviewer checks each fact against the code.

### Frontend (data layer)

5. **FE-1 Token interceptor: Cognito token for owner `/guest/groups/` routes (D3)**
   - Type: code
   - Area: frontend
   - Scaffold first: no
   - Files: `FE/auth/token.interceptor.ts:20-23`.
   - Contract: a URL is a guest route when it contains `/guest/` and does **not** contain `/guest/groups/`. Guest routes: unchanged (no Cognito header, the request's own `Authorization` header is kept, guest toasts). All other URLs, now including `/guest/groups/{id}/sessions`, `/guest/groups/{id}/sessions/{sid}`, `/guest/groups/{id}/submit` and `/guest/groups/{id}/status`: `Authorization: Bearer <idToken>` when tokens exist, the normal error handling (incl. `SKIP_ERROR_TOAST`). Update the comment.
   - Tests (for test-writer): `FE/auth/token.interceptor.spec.ts` (tokens = `{ idToken: 'id-1' }` in the new tests) — a GET to `…/guest/groups/g1/sessions` has `Authorization: Bearer id-1`; a POST to `…/guest/sessions/s1/dishes` with header `Authorization: Bearer guest-token` keeps `Bearer guest-token`; a GET to `…/guest/invites/t1` has no `Authorization` header.
   - Change: one condition and the comment.
   - Check: `npx tsc -p tsconfig.app.json --noEmit` and `npx tsc -p tsconfig.spec.json --noEmit` → exit 0; `npm test -- --watch=false --browsers=ChromeHeadless` → 0 failed.

6. **FE-2 Group models and `GroupOrdersService`**
   - Type: code
   - Area: frontend
   - Scaffold first: yes
   - Files: `FE/models/group-orders/group-status.model.ts`, `group-sessions-response.model.ts` (also English comment), `submit-group-pre-order.model.ts:1,6` (fix the path in the comments), new `FE/models/group-orders/group-bonus.model.ts`, new `FE/services/group-orders/group-orders.service.ts`, `FE/services/pre-orders/pre-orders.service.ts:73` (widen the parameter type only).
   - Contract:
     ```ts
     // group-bonus.model.ts
     export type GroupBonusState = 'InProgress' | 'Achieved' | 'Missed';
     export type GroupBonusTier = 'TenPercentDiscount' | 'FreeDessert';
     export interface GroupBonus { state: GroupBonusState; tier: GroupBonusTier | null; discountPercent: number; deadline: string | null; }
     // group-status.model.ts: GroupStatusResponse adds
     orderedCount: number; bonus: GroupBonus;
     // group-sessions-response.model.ts: GroupSessionsResponse adds
     owner: Participant;
     // group-orders.service.ts (@Injectable providedIn root, inject(API_BASE_URL), inject(HttpClient))
     getOwnerGroupId$(reservationId: string): Observable<string>;
       // GET {base}/reservations/{reservationId}/guest-session-id → strips the "owner-" prefix of guestSessionId.
       // A value without the prefix → error Observable (Error 'Unexpected owner session id').
     getSessions$(groupId: string): Observable<GroupSessionsResponse>;      // GET {base}/guest/groups/{groupId}/sessions
     getStatus$(groupId: string): Observable<GroupStatusResponse>;          // GET {base}/guest/groups/{groupId}/status
     removeGuest$(groupId: string, guestSessionId: string): Observable<GroupSessionsResponse>; // DELETE {base}/guest/groups/{groupId}/sessions/{guestSessionId}
     submit$(groupId: string, reservationId: string): Observable<SubmittedDish[]>;           // POST {base}/guest/groups/{groupId}/submit, body { reservationId }
     // pre-orders.service.ts
     editPreOrderByReservationId(reservationId: string, order: { orderItems: { dish: { id: string }; quantity: number }[] })
       // behaviour unchanged; Order still fits the type.
     ```
   - Tests (for test-writer): new `FE/services/group-orders/group-orders.service.spec.ts` with `HttpTestingController` — `getOwnerGroupId$` calls the URL and emits `g1` for `owner-g1`; it errors for `g1`; `removeGuest$` uses DELETE and the URL; `submit$` uses POST, the URL and body `{ reservationId: 'r1' }`. (`getSessions$` / `getStatus$` are plain GETs; one test for `getStatus$` URL is enough.)
   - Change: as in the contract.
   - Check: as FE-1.

7. **FE-3 Pure view helpers for the modal**
   - Type: code
   - Area: frontend
   - Scaffold first: yes
   - Files: new `FE/shared/helpers/group-pre-order.helpers.ts`, new model `FE/models/group-orders/group-pre-order-view.model.ts`.
   - Contract:
     ```ts
     export type GroupSeat =
       | { kind: 'owner'; ordered: boolean; initials: string }
       | { kind: 'guest'; ordered: boolean; initials: string; name: string; participantId: string }
       | { kind: 'empty'; seatNumber: number };
     export interface GroupPreOrderView {
       seatCount: number; orderedCount: number; remainingCount: number; // from status; remaining = max(0, seat − ordered)
       seats: GroupSeat[];            // owner, then guests in API order, then empty seats numbered up to seatCount
       ownerOrdered: boolean; ownerSubtotal: number;          // subtotal 0 when not ordered
       orderedGuestCount: number; guestsSubtotal: number;     // submitted guests only
       grandTotal: number; discountAmount: number; finalTotal: number; unlockDiscountAmount: number;
       isOpen: boolean; isSubmitted: boolean; canSubmit: boolean;
       bonus: GroupBonus;
     }
     export function buildGroupPreOrderView(sessions: GroupSessionsResponse, status: GroupStatusResponse, ownerName: string): GroupPreOrderView;
     export function formatTimeLeft(deadline: Date, now: Date): string | null;
     ```
     Rules: "Data shape" 7. `discountAmount` = round2(grandTotal × discountPercent / 100) only for `bonus.state === 'Achieved'`; `unlockDiscountAmount` = the same formula only for `'InProgress'`; else 0. Empty seats = max(0, seatCount − 1 − guests). `canSubmit` = status `'Open'` and `orderedCount >= 1`. Initials = first letters of the first two words, upper case. `formatTimeLeft`: ≤ 0 → `null`; ≥ 1 day → `'1 day 3 h left'` (plural `days`); ≥ 1 h → `'5 h left'`; else `'45 min left'`.
   - Tests (for test-writer): new `FE/shared/helpers/group-pre-order.helpers.spec.ts` — grand total = owner + submitted guests, a waiting guest with items is excluded; `Achieved` 10 % on 136 → discount 13.60, final 122.40; `InProgress` → discount 0 and unlock amount = 10 %; seats for 6 seats and 3 guests = owner, 3 guests, empty seats 5 and 6; `canSubmit` false when `orderedCount` is 0 and when status is `'Submitted'`; `formatTimeLeft` for 27 h and for 45 min; past deadline → `null`.
   - Change: pure functions only; no Angular imports.
   - Check: as FE-1.

### Frontend (UI)

8. **FE-4 Reservation card: trash icon for Cancel, Group Pre-Order button (D4, M00)**
   - Type: code
   - Area: frontend
   - Scaffold first: yes (new output only)
   - Files: `FE/components/reservations/card-reservation/card-reservation.component.ts:73-95`, `.html:1-35,125-231`, `.scss:1-110`.
   - Contract: new `readonly groupPreOrder = output<ReservationCustomer>()`. Customer role only: Cancel (statuses Reserved and InProgress) becomes an icon button with `assets/svg/trash.svg`, `aria-label="Cancel reservation"`, in the top-right of the address line (after the status badge); it emits `cancelReservation` with the id as today. Reserved row: Edit (`button--secondary`), Pre-order (`button--primary`), "Group Pre-Order" (`button--secondary`) in one row; all customer card buttons use `button--small` (Q5). "Group Pre-Order" shows only when role is customer, status is Reserved and `isGroupReservation(guestCount)`. Waiter markup does not change.
   - Tests (for test-writer): `FE/components/reservations/card-reservation/card-reservation.component.spec.ts` (replace or keep "should create") — group Reserved card shows the "Group Pre-Order" button and a click emits `groupPreOrder` with the reservation; no button for `guestCount: 1` and for status InProgress; the "Cancel reservation" icon button emits `cancelReservation` with the id.
   - Change: template, output, styles (no layout change for the waiter role).
   - Check: as FE-1, plus `BACKEND_URL=https://example.com npm run build` → "Bundle check passed"; report the bundle size.

9. **FE-5 Group Pre-Order modal: load and show (read-only) + page wiring**
   - Type: code
   - Area: frontend
   - Scaffold first: yes
   - Files: new `FE/components/reservations/group-pre-order-modal/group-pre-order-modal.component.{ts,html,scss}` (presentational children allowed in the same folder, for example seats row and participant row), `FE/shared/custom-components/modal/modal.component.html` (additive: `<ng-content select="[modal-title-addon]" />` next to the title), `FE/pages/reservation-dashboard/reservation-dashboard.component.{ts,html}:15-27,94-102`.
   - Contract:
     - Component `app-group-pre-order-modal`, standalone, OnPush, signal API (as the card and page): `reservation = input.required<ReservationCustomer>()`, `ownerName = input('')`, `closed = output<{ changed: boolean }>()`.
     - On init: `getOwnerGroupId$(reservation.id)` → `forkJoin(getSessions$, getStatus$)` → `buildGroupPreOrderView`. A `reload()` method repeats the last two calls.
     - States: loading (spinner); error (inline text "Could not load the group pre-order." with `data-testid="gpo-error"`, no extra toast); content.
     - Content per M01/M05/M06/M07/M08/M11: uses `<app-modal width="800px" title="Group Pre-Order">`; header line address, date, time, guests; "Table · {ordered} of {seats} ordered" (`data-testid="gpo-ordered-summary"`, text contains `"{ordered} of {seats} ordered"`); seats row (horizontal scroll with ‹ › buttons when it overflows, M08); bonus box (InProgress text with remaining count, tier text, "Until {deadline} · {timeLeft}", progress bar; Achieved green box; Missed grey box); "My Pre-Order" row (collapsed/expanded, read-only in this step; M07 text when not ordered); "Guests' Pre-Orders · N guests" with Expand all / Collapse all, each guest row collapsible, a not-submitted guest shows "Waiting" and no subtotal; empty state box when no guest joined (M06); footer "Grand Total" (`data-testid="gpo-grand-total"`, shows `finalTotal` with the `currency` pipe), breakdown and discount line.
     - Submitted (M10): badge "Sent to Restaurant" in `[modal-title-addon]`, green box "Pre-order sent to the restaurant" + bonus text + "guests can no longer change their orders"; no footer button (D5).
     - Escape or close while no dialog is open → `closed.emit({ changed })`.
     - Page: `(groupPreOrder)` on the card opens the modal (signal `groupPreOrderReservation`); `ownerName` = first + last name; on `closed` with `changed: true` → `loadReservations()` and `cartState.refresh()`.
   - Tests (for test-writer): new `group-pre-order-modal.component.spec.ts` with a `GroupOrdersService` spy — loaded data (owner ordered 27, guests: 36 submitted, 20 submitted, 17 not submitted; status seats 6, ordered 3, `InProgress`) shows "3 of 6 ordered" and a grand total of $83.00; `getOwnerGroupId$` error shows `gpo-error`; status `Submitted` shows "Sent to Restaurant" and no "Submit Pre-Order" button.
   - Change: as in the contract. SCSS uses the Figma values (`M02-design-context.tsx.txt`) and the existing color tokens and button classes.
   - Check: as FE-4.

10. **FE-6 Modal actions: copy link, remove guest (M04), submit (M09, M10)**
    - Type: code
    - Area: frontend
    - Scaffold first: yes (`ConfirmDialogComponent` inputs and outputs)
    - Files: new `FE/shared/custom-components/confirm-dialog/confirm-dialog.component.{ts,html,scss}`, the modal files from FE-5, new `FE/shared/helpers/invite-link.ts` (`buildInviteLink(token: string): string` = `${window.location.origin}/join/${token}`), `FE/components/reservations/reservation-invite-modal/reservation-invite-modal.component.ts:51-54` (use the helper).
    - Contract:
      - `app-confirm-dialog`: inputs `title`, `message`, `cancelLabel`, `confirmLabel`, `confirmVariant: 'danger' | 'primary'`; outputs `confirmed`, `cancelled`. Overlay over the modal card (M04/M09). While a dialog is open, Escape or the modal close closes only the dialog.
      - "Copy pre-order link" (only when the group is Open): `GuestInviteService.getInviteToken$(reservation.id)` → `navigator.clipboard.writeText(buildInviteLink(token))` → the button text becomes "Link copied" for 3 s (M03b). Failure → `toast.error('Could not copy the link. Please try again.')`.
      - "Remove {name}" in an expanded guest row (only when Open) → confirm dialog title "Remove {name} from the table?", message "{name}'s pre-order will be deleted and the seat becomes free.", buttons "Keep" / "Remove {name}" (danger). Confirm → `removeGuest$(groupId, participantId)` → `reload()`, `changed = true`. Keep → no call.
      - "Submit Pre-Order": disabled when `!canSubmit`. `remainingCount === 0` → `submit$` at once. `remainingCount > 0` → confirm dialog "{remaining} seat(s) haven't pre-ordered yet", message "If you confirm now, the restaurant will receive {ordered} pre-orders[ and the {10% group discount | free dessert bonus} won't apply]. After confirmation, guests can't change their orders." (the bracket part only for an `InProgress` bonus), buttons "Wait for guests" / "Confirm anyway". Success → `reload()` (shows M10), `changed = true`. Errors: the interceptor toast; the button is enabled again.
    - Tests (for test-writer): modal spec — copy link writes `…/join/t1` to the clipboard (spy on `navigator.clipboard.writeText`) and shows "Link copied"; "Remove Anna" → "Remove Anna" in the dialog calls `removeGuest$('g1', 'guest-anna')` and reloads; "Keep" makes no call; submit with all seats ordered calls `submit$('g1', 'r1')` without a dialog; submit with 2 empty seats opens the dialog, "Wait for guests" makes no call, "Confirm anyway" calls `submit$`; submit button is disabled when `orderedCount` is 0. No tests for `ConfirmDialogComponent` (presentational).
    - Change: as in the contract.
    - Check: as FE-4.

11. **FE-7 Owner's dishes in the modal: edit (M02) and "Add my dishes" (M07)**
    - Type: code
    - Area: frontend
    - Scaffold first: no
    - Files: the modal files from FE-5.
    - Contract: expanded "My Pre-Order" (only when Open) shows each own dish with − qty +, price and trash (`aria-label`s "Decrease {dish}", "Increase {dish}", "Remove {dish}"). Each action builds the new item list from `sessions.owner.items` and calls `PreOrdersService.editPreOrderByReservationId(reservation.id, { orderItems })` (an empty list cancels the order, as today); success → `reload()`, `cartState.refresh()`, `changed = true`. "−" at quantity 1 is disabled. "+ Add my dishes" (owner not ordered, Open) → `closed.emit({ changed })` and `router.navigate(['/menu'], { queryParams: buildPreOrderQueryParams(reservation) })`.
    - Tests (for test-writer): modal spec — "Increase {dish}" calls `editPreOrderByReservationId('r1', …)` with that dish's quantity + 1 and the other items unchanged, then reloads; "Add my dishes" navigates to `/menu` with the pre-order query params and emits `closed`.
    - Change: as in the contract.
    - Check: as FE-4.

## Contracts and risks

- **API changes (backend wins, FE models in FE-2):** `GET /guest/groups/{id}/status` adds `orderedCount`, `bonus`. `GET /guest/groups/{id}/sessions` and `DELETE …/sessions/{sid}` add `owner`. `POST /guest/groups/{id}/submit` accepts an owner-only or a guests-only submit, creates the owner's order when it is missing, returns `[]` when no guest dish exists, and has a new 400 message. No new route, so `RouteRegistry.cs` route table does not change. `ApiRoutes.md` in BE-4.
- **Data:** new optional DynamoDB attributes `Orders.createdAt`, `GuestSessions.submittedAt`. No table, index or `deployment_resources.json` change, so no infra review. No migration; old records have null values (Q2).
- **Security:** the interceptor change sends the Cognito id token to `/guest/groups/*` only. Guest session calls keep their own token (FE-1 tests). The public status route now shows the ordered count and the bonus, no names.
- **Money:** the discount is shown only in the frontend. The backend order has no discount field, so the waiter and the restaurant do not see the bonus. Record as tech debt.
- **Time zones:** the bonus uses `Reservation.StartTime.ToUniversalTime()` like `OrderService.cs:296`. Tests must use `DateTimeKind.Utc` values. The DynamoDB SDK can load `DateTime` as local time; the Lambda runs in UTC.
- **Owner own items after submit** come from a subtraction. A later waiter edit of the order (status InProgress) can make them wrong. The card hides the button outside Reserved.
- **Planned compile breaks** between scaffold and test-writer in BE-2 and BE-3 (constructor parameters). The test-writer fixes the existing constructor calls first.
- **No group for some reservations:** `PUT /reservations` that raises `guestCount` from 1 does not create a group, so `guest-session-id` returns 404 and the modal shows its error state. Record as tech debt.
- **Layout:** `grid-auto-rows: 1fr` makes every card as tall as the tallest one. Three small buttons fit one row at 1440 px (estimate ≈ 332 px of ≈ 370 px); they wrap on narrower cards. Check in a browser.
- **No new npm or NuGet packages.**

### Deviations from repo rules (show at the checkpoint)

- New components use signal `input()`/`output()`, OnPush and `@app/` imports, like the card and page that they extend (`CLAUDE.md` §5 describes the target differently; FE tech-debt 2.3).
- Shared `ModalComponent` gets one additive content slot (`[modal-title-addon]`).
- Design changes: no seats stepper (D2), no "Add to calendar" (D5), no remind action (D5: the copy link is the reminder), no orange dot on the card button. M04 text changes to "the seat becomes free" (DELETE does not reduce the seats). M11 footer reads "Group discount — deadline passed" (no percent when the bonus is missed). Prices use the existing `currency` pipe (`$100.00`, design shows `$100`).
- The waiter card keeps the text "Cancel" button.

### Check after the user runs `syndicate deploy` (nobody deploys in this task)

1. `GET /guest/groups/{id}/status` returns `orderedCount` and `bonus`; `deadline` ends with `Z` and is 24 h / 2 h before the start.
2. Owner calls from the browser to `GET …/sessions`, `DELETE …/sessions/{sid}` and `POST …/submit` pass the Cognito authorizer (no 401).
3. Guest flow (`/join/{token}`, add dishes, submit cart) still works with the guest token.
4. `POST /orders` writes `createdAt`; guest cart submit writes `submittedAt` (check the items in DynamoDB).
5. Group submit: owner-only, guests-only (a new order with the address and time text appears in `GET /orders/{reservationId}` and for the waiter), and nothing to submit (400 with the new message).
6. After a submit, `GET …/sessions` returns the owner's own items without the guests' dishes.

## Out of scope

- Seats stepper and "seat removed" (D2), "Add to calendar" and reminders (D5), the notification dot on the card button.
- Discount stored on the order, or shown to the waiter.
- Dev mock `FE/shared/mocks/group-orders/*`: owner in `participants`, wrong submit path `/group-reservations/{id}/submit`, no `owner`/`bonus` fields. Dev `?demo` mode will differ from the real API. Tech debt.
- `ApiRoutes.md` drift for `/guest-sessions/{id}/dishes` (`:1139,1154`) and the order `status` type (FE tech-debt 2.4, other parts).
- `PUT /reservations` does not create or close the group reservation (`EditReservationHandler.cs`).
- Unused FE helper `consolidate-pre-order.ts` and its duplicate interfaces; Ukrainian comments in mock and config files.
- Group submit 30-minute cutoff and reservation status check (Q6). Group submit does not notify the waiter.
- Two constants for the group size (FE tech-debt 1.5).
