# 02 — Plan r2: Group Pre-Order modal (owner)

Planner inputs: `00-task.md` (D1–D6 and the user answers after r1), `01-explore.md`, `03-plan-review-r1.md`, Figma PNGs M00–M11 and `figma/M02-design-context.tsx.txt`. I opened each file that the plan changes. Paths: `BE` = `backend/restaurant-backend-app/dnapp/lambdas/api-handler/`, `BT` = `backend/restaurant-backend-app/Tests/`, `FE` = `frontend/restaurant-frontend-app/src/app/`.

## Changes after review

- [major] BE-2/BE-3 scaffolds broke the test build → each scaffold now compiles against the current tests. A new constructor parameter is added as an **optional** parameter (`= null`) in the scaffold. The test-writer passes the mock. In the implement turn the implementer makes the parameter required. `Owner` is a non-positional `init` property of `GetAllSessionsResponse`, so the 3 record calls in the tests do not change. The scaffold "Check" lines now require a green `dotnet build` of the solution.
- [major] FE-2 broke `tsc` through the dev mock → FE-2 now changes `FE/shared/mocks/group-orders/group-orders.mock-store.ts` with the smallest type-complete values (`owner`, `orderedCount`, `bonus`). The larger mock clean-up stays out of scope.
- [minor] Two "owner has items" rules → BE-3 uses `GroupPreOrderRules.OwnerOwnItems(order, sessions, group.Status)`. The "nothing to submit" test gets a row for an order with only `Canceled` items.
- [minor] Null location in BE-3 → `?? throw new ValidationException("Location not found for the reservation's table.")` (400) before the transaction, as `CreateOrderHandler.cs:156`.
- [minor] BE-1 missing rows → added "all seats ordered, group not open → Achieved" and "not all ordered, now = start − 2h → InProgress + FreeDessert".
- [minor] FE-5 total test → the fixture now uses `Achieved` 10 % (136 → $122.40), so the test fails if the template shows the total before discount.
- [minor] Bonus texts → FE-5 has a table with the text for each state and tier, also for the footer, M09 and M10.
- [minor] US2 AC3 → the footer shows the total before discount, the discount amount and the final total when the 10 % bonus is achieved.
- [minor] Risks as decisions → the frontend-only discount and the public status data are now "Accepted user decisions" (user answers after r1).
- [minor] FE tech-debt 2.4 → BE-4 now also updates `frontend/restaurant-frontend-app/docs/tech-debt.md` 2.4 (status drift fixed) and 2.1 (baseline 0 failed of 364).
- Optional: the FE-2 `getStatus$` URL test is dropped.
- User answers: Q1, Q2, Q4, Q5 use the defaults; Q3, Q6 keep the defaults. The open questions are closed.

## Goal

The group owner opens a "Group Pre-Order" modal from the reservation card. The modal shows the seats, the owner's and the guests' pre-orders, the totals and the group bonus that the backend computes. The owner can copy the invite link, remove a guest, edit the owner's own dishes and submit the group pre-order. The backend accepts a submit that has at least one order.

## Open questions

None. The user decided Q1–Q6 (see "Accepted user decisions").

## Accepted user decisions (2026-11-04)

- Discount is **display only**. The frontend computes the discounted total. The backend `Order` has no discount field, so the restaurant order and the waiter see the full price while the modal says "10% off applied". Follow-up in tech debt: store the bonus on the order for the waiter.
- Q1: the owner pre-order time is the order **creation** time. A later edit does not move it.
- Q2: records saved before the deploy (no `createdAt` / `submittedAt`) count as **on time**.
- Q4: `GET /guest/groups/{id}/status` stays **public** (no authorizer) and returns `orderedCount` and `bonus`. Anyone with the group id can see the ordered count, the bonus and the deadline (which gives the reservation start time). The status handler reads the `Orders` table without authentication. No names are returned.
- Q5: `button--small` on all customer card buttons in every status. The waiter card does not change.
- Q3: no dialog when every seat has ordered. Q6: group submit keeps today's time and status rules.

## Data shape first (decisions that every step uses)

1. **Seats** = `Reservation.GuestCount` (owner included) = `maxCapacity` in the status response.
2. **Owner's own items** = `GroupPreOrderRules.OwnerOwnItems(order, sessions, groupStatus)`: active order items that are not `Canceled`. After a group submit (status `Submitted`) the order also holds the guests' dishes (`MergeIntoOrderItems`), so the function subtracts the consolidated quantities of the submitted guest sessions per dish id and drops items with quantity ≤ 0. **Owner has ordered / has items** = own items not empty. This is the only rule for the owner: the status, the sessions snapshot and the submit (BE-3) use it. An order that the group submit created (owner had no order) has no own items.
3. **Guest has ordered** = `GuestSession.IsSubmitted`. A guest with items but not submitted is "Waiting" and is not in the totals (US1 AC3).
4. **Ordered count** = (owner has ordered ? 1 : 0) + submitted guest sessions (owner session excluded).
5. **Last pre-order time** = latest of: owner `Order.CreatedAt` (only if the owner has ordered) and `GuestSession.SubmittedAt` of the submitted guests. Null values are skipped (Q2).
6. **Bonus** (pure function `GroupBonusCalculator.Calculate`, all times UTC, `start` = reservation start):
   - All seats ordered (`orderedCount >= seatCount`), for any group status: last time ≤ `start − 24h` → `Achieved` + `TenPercentDiscount`. Else last time ≤ `start − 2h` → `Achieved` + `FreeDessert`. Else → `Missed`. Last time null → `Achieved` + `TenPercentDiscount`.
   - Not all ordered and group not `Open` (submitted or closed) → `Missed`.
   - Not all ordered, group `Open`: `now ≤ start − 24h` → `InProgress` + `TenPercentDiscount`, deadline `start − 24h`. Else `now ≤ start − 2h` → `InProgress` + `FreeDessert`, deadline `start − 2h`. Else → `Missed`.
   - Boundaries are inclusive ("at least 24 h / 2 h before").
   - `Missed` → `tier: null`, `discountPercent: 0`, `deadline: start − 2h`. `Achieved` → `deadline: null`. `discountPercent` = 10 for `TenPercentDiscount` (also while `InProgress`), else 0.
7. **Money (frontend only)**: `grandTotal` = owner subtotal (if ordered) + submitted guests' subtotals. `discount` = round2(grandTotal × discountPercent / 100) only when the bonus is `Achieved`. `final` = grandTotal − discount.
8. **Clock**: the backend code uses `DateTime.UtcNow` (no `TimeProvider`). The plan follows the code: the calculator is pure and takes `nowUtc`; services pass `DateTime.UtcNow`. Boundary tests run on the pure calculator.

## Steps

### Backend

1. **BE-1 Pure group rules: bonus calculator, owner own items, shared consolidation**
   - Type: code
   - Area: backend
   - Scaffold first: yes (new types only; no existing call site changes)
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
         // "Data shape" 2. Returns new OrderItem objects; does not change `order`.
         public static IReadOnlyList<OrderItem> OwnerOwnItems(
             Order? order, IEnumerable<GuestSession> sessions, GroupReservationStatus groupStatus);
     }
     ```
     Rules: "Data shape" 2 and 6. `OwnerOwnItems`: `order` null → empty; `Canceled` items excluded; only for `Submitted` subtract `Consolidate(sessions.Where(s => s.IsSubmitted))`; item order kept. No exceptions.
   - Tests (for test-writer): new `BT/Services/GroupBonusCalculatorTests.cs`, `DateTimeKind.Utc` values. [Theory] all seats ordered: last = start−24h, open → Achieved / TenPercent / 10; last = start−24h, **group not open** → Achieved / TenPercent; last = start−24h+1 min → Achieved / FreeDessert / 0; last = start−2h → Achieved / FreeDessert; last = start−2h+1 min → Missed; last null → Achieved / TenPercent. [Theory] not all ordered, open: now = start−24h → InProgress / TenPercent, deadline start−24h; now = start−24h+1 min → InProgress / FreeDessert, deadline start−2h; **now = start−2h → InProgress / FreeDessert**; now = start−2h+1 min → Missed, tier null, deadline start−2h. [Fact] not all ordered, group not open, now = start−3 days → Missed. [Fact] JSON of a `GroupBonusResponse` (System.Text.Json default options) has `"state":"InProgress"`, `"tier":"TenPercentDiscount"`, `"discountPercent":10`, a `"deadline"` that ends with `Z`; and `"tier":null` for `Missed`. New `BT/Services/GroupPreOrderRulesTests.cs`: Open group returns the order items without `Canceled` items; Submitted group subtracts the submitted guests' quantities and drops a dish that only guests ordered; null order → empty. Existing `GroupPreOrderServiceTests` pin the move of `Consolidate`.
   - Change: add the types; move `Consolidate` to `GroupPreOrderRules` and call it from `GroupPreOrderService`; implement `Calculate` and `OwnerOwnItems`.
   - Check: `dotnet build restaurant-backend-app.sln --no-incremental` → 0 errors, 0 new warnings in changed files (after the scaffold too); `dotnet test Tests/APIHandler.Tests.csproj` → 0 failed.

2. **BE-2 Timestamps, group status with bonus, owner in the sessions snapshot**
   - Type: code
   - Area: backend
   - Scaffold first: yes. The scaffold must keep the solution build (incl. `Tests`) green:
     - `GuestSessionService` primary constructor gets a new **last, optional** parameter `IOrderRepository? orderRepository = null`. The current `new GuestSessionService(6 args)` at `BT/Services/GuestSessionServiceTests.cs:35` still compiles.
     - `GetAllSessionsResponse` gets a **non-positional** property `public Participant? Owner { get; init; }`. The positional parameters do not change, so `BT/Handlers/GetGroupSessionsHandlerTests.cs:58` and `BT/Handlers/DeleteGuestSessionHandlerTests.cs:67,262` still compile.
     - New nullable entity properties and new status response properties do not break any call site (the only `new GroupReservationStatusResponse` is in `GuestSessionService.cs:167`; the scaffold sets the new members there).
     - Test-writer turn: passes `_mockOrderRepository.Object` as the 7th constructor argument.
     - Implement turn: the implementer makes the parameter required (`IOrderRepository orderRepository`, no default), after the tests pass it.
   - Files: `BE/Entities/Order.cs`, `BE/Entities/GuestSession.cs`, `BE/Services/Orders/OrderService.cs:89-123`, `BE/Services/GuestSessions/GuestSessionService.cs:27-33,157-173,176-244,246-257`, `BE/Models/GuestSessions/GroupReservationStatusResponse.cs`, `BE/Models/GuestSessions/GetAllResponse.cs:7`. No change to `RouteRegistry.cs` (`IOrderRepository` is registered, `RouteRegistry.cs:93`).
   - Contract:
     - `Order.CreatedAt`: `DateTime?`, `[DynamoDBProperty("createdAt")]`, `[JsonPropertyName("createdAt")]`. `OrderService.AddOrderAsync` and `AddOrderForWaiterAsync` set it to `DateTime.UtcNow` before save. Edits, submit, unsubmit and cancel do not change it.
     - `GuestSession.SubmittedAt`: `DateTime?`, `[DynamoDBProperty("submittedAt")]`. `SubmitCartAsync` sets it to `DateTime.UtcNow` with `IsSubmitted = true`.
     - `GroupReservationStatusResponse` adds `[JsonPropertyName("orderedCount")] int OrderedCount` and `[JsonPropertyName("bonus")] GroupBonusResponse Bonus` (the service always sets it). Existing fields do not change. `GetGroupReservationStatusAsync(groupReservationId)`: loads the active order with `orderRepository.GetOrderByReservationIdAsync(group.ReservationId)`, computes "Data shape" 2, 4, 5 and calls `GroupBonusCalculator.Calculate(reservation.GuestCount, orderedCount, last, reservation.StartTime.ToUniversalTime(), DateTime.UtcNow, group.Status == Open)`. Errors unchanged (404 group not found).
     - `GetAllSessionsResponse.Owner` (JSON `owner`): the service always sets it: `Participant(Id: "owner-{groupReservationId}", Type: Owner, Name: "Owner", Items: OwnerOwnItems mapped to OrderItemResponse (dishes that do not exist are skipped, as for guests), Total: sum of price × quantity, IsSubmitted: own items not empty)`. `Participants` and `Total` keep today's meaning (guests only; total of all guests). `GetGroupSessionsAsync` and `DeleteGuestSessionAsync` both set it. Owner dish ids go into the same `GetDishesByIdsAsync` call. Errors unchanged.
   - Tests (for test-writer): `BT/Services/OrderServiceTests.cs`: `AddOrderAsync` saves the order with `CreatedAt` between the UTC time before and after the call. `BT/Services/GuestSessionServiceTests.cs`: pass the order repository mock to the constructor; `SubmitCartAsync` saves `IsSubmitted = true` and `SubmittedAt` in the same window; status: owner order with own items + 2 of 3 guests submitted, seats 4 → `OrderedCount = 3`, bonus `InProgress` (start = UtcNow + 3 days); status: all 4 seats ordered with times 2 days before a start 3 days ahead → `Achieved` + `TenPercentDiscount`; status: group `Submitted`, order holds only the guests' dishes → owner not counted; sessions: `Owner` has own items, total and `IsSubmitted = true`; sessions with no order → `Owner.Items` empty, `Total = 0`, `IsSubmitted = false`. No new handler tests.
   - Change: add the fields; set the times; add the owner order lookup and the owner participant; replace `BuildGuestSnapshotAsync` with one builder that returns the guests and the owner; make the constructor parameter required.
   - Check: as BE-1 (build green after the scaffold, after the tests and after the implementation).

3. **BE-3 Group submit accepts at least one order and creates the owner's order if missing (D6)**
   - Type: code
   - Area: backend
   - Scaffold first: yes. Build stays green: `GroupPreOrderService` gets a new **last, optional** parameter `ILocationService? locationService = null`, so `BT/Services/GroupPreOrderServiceTests.cs:30` still compiles. `OrderMapper.FormatReservationTime` is a new static method. Test-writer turn: passes a `Mock<ILocationService>`. Implement turn: the parameter becomes required.
   - Files: `BE/Services/GroupReservations/GroupPreOrderService.cs:16-65`, `BE/Mappers/OrderMapper.cs:30-42`, `BE/Handlers/Order/CreateOrderHandler.cs:159` (use the mapper method; no behaviour change).
   - Contract:
     - `ILocationService` is registered (`RouteRegistry.cs:107`).
     - `OrderMapper.FormatReservationTime(DateTime start, DateTime end)` returns `$"{start:MMM dd, yyyy, h:mm tt} - {end:h:mm tt}"` (the string from `CreateOrderHandler.cs:159`).
     - `SubmitGroupPreOrderAsync(userId, groupReservationId, reservationId)` → `IReadOnlyList<GroupPreOrderDishResponse>`:
       1. Unchanged checks and errors: reservation missing → `ReservationNotFoundException` (404); caller not owner → `ActionForbiddenException` (403); group missing or of another reservation → `GroupReservationNotFoundException` (404); group `Submitted` → `GroupReservationSubmittedException`, `Closed` → `GroupReservationExpiredException` (400).
       2. `guestDishes` = `GroupPreOrderRules.Consolidate(sessions.Where(s => s.IsSubmitted))` (can be empty).
       3. `order` = active order. Owner has items = `GroupPreOrderRules.OwnerOwnItems(order, sessions, group.Status).Count > 0` (the group is `Open` here). Same rule as the status.
       4. `guestDishes` empty and owner has no items → `ValidationException("Nothing to submit: the group has no pre-orders.")` (400). No transaction call.
       5. `order` null → location = `await locationService.GetLocationByTableIdAsync(reservation.TableId) ?? throw new ValidationException("Location not found for the reservation's table.")` (400, before the transaction). New `Order { OrderId = new Guid string, CustomerId = userId, ReservationId, ReservationAddress = location.Address, ReservationTime = OrderMapper.FormatReservationTime(reservation.StartTime, reservation.EndTime), Dishes = guestDishes as Pending items, Status = SubmittedByCustomer, CreatedAt = DateTime.UtcNow }`. Location lookup only in this case.
       6. `order` exists → today's merge (D5/D5b) and `Status = SubmittedByCustomer`. The owner's items are never added a second time.
       7. One `SubmitGroupPreOrderAsync(groupId, order)` call; `false` → `GroupReservationSubmittedException` (unchanged).
       8. Returns `guestDishes` (can be `[]`). HTTP 200.
     - Removed errors: "No submitted guest sessions…", "Submitted guest sessions contain no dishes." and "Active pre-order … not found".
   - Tests (for test-writer): `BT/Services/GroupPreOrderServiceTests.cs`: pass the location mock to the constructor; owner order with items and no submitted guest → one transaction with the same items, status `SubmittedByCustomer`, returns an empty list; no order and 2 submitted guests → one transaction with a new order (`CustomerId`, `ReservationId`, address from the location mock, `ReservationTime` = formatted string, consolidated `Pending` items, `SubmittedByCustomer`), returns the consolidated dishes; [Theory] nothing to submit → `ValidationException` with the new message and no transaction, rows: no order and no submitted guest; order with only `Canceled` items and no submitted guest. Rewrite the existing tests that expect the removed errors (`NoSessions`, `OnlyUnsubmittedSessions`, `SubmittedSessionsWithoutDishes`, `DishesOnlyInUnsubmittedSessions`, `OrderNotFound`) to the new rules; delete a test when a new test already covers its case.
   - Change: as in the contract; make the constructor parameter required.
   - Check: as BE-1. `SubmitGroupPreOrderHandlerTests` stay green (handler unchanged).

4. **BE-4 Docs: API contract, ADR note, frontend tech debt**
   - Type: docs
   - Area: backend (and one frontend doc)
   - Scaffold first: no
   - Files: `backend/restaurant-backend-app/docs/ApiRoutes.md:1198-1206` (status), `:1208-1267` (submit), `:1268-1313` (sessions), `:1314-` (delete); `backend/restaurant-backend-app/docs/decisions/0002-owner-session-and-group-submit.md` (short dated amendment: D6 and the owner own-items rule); `frontend/restaurant-frontend-app/docs/tech-debt.md` 2.1 and 2.4.
   - Contract: status section uses the code path `GET /guest/groups/{groupReservationId}/status`, fields `currentParticipants`, `maxCapacity`, `status`, `orderedCount`, `bonus {state, tier, discountPercent, deadline}`, the rules from "Data shape" 6, an example, and a note that the route is public. Sessions and delete: add `owner`. Submit: new preconditions, the 2 new 400 messages, the owner order creation, the response can be `[]`. Copy every key, status and message from the code. Tech debt: 2.4 — remove the status drift from the problem text (the `/guest-sessions/...` paths and the order `status` type stay); 2.1 — the baseline is now 0 failed of 364 (explorer run, 2026-11-04); mark it fixed or rewrite it to that fact.
   - Tests (for test-writer): none: docs step.
   - Change: edit only these sections.
   - Check: `python .claude/skills/asd-ste100/scripts/ste-lint.py <file>` for each changed file; the code-reviewer checks each fact against the code.

### Frontend (data layer)

5. **FE-1 Token interceptor: Cognito token for owner `/guest/groups/` routes (D3)**
   - Type: code
   - Area: frontend
   - Scaffold first: no
   - Files: `FE/auth/token.interceptor.ts:20-23`.
   - Contract: a URL is a guest route when it contains `/guest/` and does **not** contain `/guest/groups/`. Guest routes: unchanged (no Cognito header, the request's own `Authorization` header is kept, guest toasts). All other URLs, now including `/guest/groups/{id}/sessions`, `…/sessions/{sid}`, `…/submit` and `…/status`: `Authorization: Bearer <idToken>` when tokens exist, the normal error handling (incl. `SKIP_ERROR_TOAST`). Update the comment.
   - Tests (for test-writer): `FE/auth/token.interceptor.spec.ts` (tokens = `{ idToken: 'id-1' }` in the new tests): a GET to `…/guest/groups/g1/sessions` has `Authorization: Bearer id-1`; a POST to `…/guest/sessions/s1/dishes` with header `Authorization: Bearer guest-token` keeps `Bearer guest-token`; a GET to `…/guest/invites/t1` has no `Authorization` header.
   - Change: one condition and the comment.
   - Check: `npx tsc -p tsconfig.app.json --noEmit` and `npx tsc -p tsconfig.spec.json --noEmit` → exit 0; `npm test -- --watch=false --browsers=ChromeHeadless` → 0 failed.

6. **FE-2 Group models, `GroupOrdersService`, dev mock types**
   - Type: code
   - Area: frontend
   - Scaffold first: yes (the scaffold includes the model fields and the mock store values, so both `tsc` gates stay green)
   - Files: `FE/models/group-orders/group-status.model.ts`, `group-sessions-response.model.ts` (also English comment), `submit-group-pre-order.model.ts:1,6` (fix the path in the comments), new `FE/models/group-orders/group-bonus.model.ts`, new `FE/services/group-orders/group-orders.service.ts`, `FE/services/pre-orders/pre-orders.service.ts:73` (widen the parameter type only), `FE/shared/mocks/group-orders/group-orders.mock-store.ts:66-73` (smallest type-complete values).
   - Contract:
     ```ts
     // group-bonus.model.ts
     export type GroupBonusState = 'InProgress' | 'Achieved' | 'Missed';
     export type GroupBonusTier = 'TenPercentDiscount' | 'FreeDessert';
     export interface GroupBonus { state: GroupBonusState; tier: GroupBonusTier | null; discountPercent: number; deadline: string | null; }
     // group-status.model.ts: GroupStatusResponse adds (required)
     orderedCount: number; bonus: GroupBonus;
     // group-sessions-response.model.ts: GroupSessionsResponse adds (required)
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
     Mock store: `snapshot()` adds `owner` = the `Owner` participant of the mock data (with its total), or an empty owner participant if none; `participants` and `total` do not change. `groupStatus()` adds `orderedCount` = number of participants with `isSubmitted`, and `bonus` = `{ state: 'InProgress', tier: 'TenPercentDiscount', discountPercent: 10, deadline: null }`. `remove()` returns `snapshot()` as today. The existing mock spec stays green without changes.
   - Tests (for test-writer): new `FE/services/group-orders/group-orders.service.spec.ts` with `HttpTestingController`: `getOwnerGroupId$` calls the URL and emits `g1` for `owner-g1`; it errors for `g1`; `removeGuest$` uses DELETE and the URL; `submit$` uses POST, the URL and body `{ reservationId: 'r1' }`.
   - Change: as in the contract.
   - Check: as FE-1 (both `tsc` gates after the scaffold too).

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
     Rules: "Data shape" 7. `ownerOrdered` = `sessions.owner.isSubmitted`. `discountAmount` = round2(grandTotal × discountPercent / 100) only for `bonus.state === 'Achieved'`; `unlockDiscountAmount` = the same formula only for `'InProgress'`; else 0. Empty seats = max(0, seatCount − 1 − guests). `canSubmit` = status `'Open'` and `orderedCount >= 1`. Initials = first letters of the first two words, upper case. `formatTimeLeft`: ≤ 0 → `null`; ≥ 1 day → `'1 day 3 h left'` (plural `days`); ≥ 1 h → `'5 h left'`; else `'45 min left'`.
   - Tests (for test-writer): new `FE/shared/helpers/group-pre-order.helpers.spec.ts`: grand total = owner + submitted guests, a waiting guest with items is excluded; `Achieved` 10 % on 136 → discount 13.60, final 122.40; `InProgress` → discount 0 and unlock amount = 10 %; seats for 6 seats and 3 guests = owner, 3 guests, empty seats 5 and 6; `canSubmit` false when `orderedCount` is 0 and when status is `'Submitted'`; `formatTimeLeft` for 27 h and for 45 min; past deadline → `null`.
   - Change: pure functions only; no Angular imports.
   - Check: as FE-1.

### Frontend (UI)

8. **FE-4 Reservation card: trash icon for Cancel, Group Pre-Order button (D4, M00)**
   - Type: code
   - Area: frontend
   - Scaffold first: yes (new output only)
   - Files: `FE/components/reservations/card-reservation/card-reservation.component.ts:73-95`, `.html:1-35,125-231`, `.scss:1-110`.
   - Contract: new `readonly groupPreOrder = output<ReservationCustomer>()`. Customer role only: Cancel (statuses Reserved and InProgress) becomes an icon button with `assets/svg/trash.svg`, `aria-label="Cancel reservation"`, in the top-right of the address line (after the status badge); it emits `cancelReservation` with the id as today. Reserved row: Edit (`button--secondary`), Pre-order (`button--primary`), "Group Pre-Order" (`button--secondary`) in one row. All customer card buttons in every status use `button--small` (Q5). "Group Pre-Order" shows only when the role is customer, the status is Reserved and `isGroupReservation(guestCount)`. Waiter markup does not change.
   - Tests (for test-writer): `card-reservation.component.spec.ts` (replace or keep "should create"): a group Reserved card shows "Group Pre-Order" and a click emits `groupPreOrder` with the reservation; no button for `guestCount: 1` and for status InProgress; the "Cancel reservation" icon button emits `cancelReservation` with the id.
   - Change: template, output, styles (no layout change for the waiter role).
   - Check: as FE-1, plus `BACKEND_URL=https://example.com npm run build` → "Bundle check passed"; report the bundle size.

9. **FE-5 Group Pre-Order modal: load and show (read-only) + page wiring**
   - Type: code
   - Area: frontend
   - Scaffold first: yes
   - Files: new `FE/components/reservations/group-pre-order-modal/group-pre-order-modal.component.{ts,html,scss}` (presentational children allowed in the same folder, for example the seats row and the participant row), `FE/shared/custom-components/modal/modal.component.html` (additive: `<ng-content select="[modal-title-addon]" />` next to the title), `FE/pages/reservation-dashboard/reservation-dashboard.component.{ts,html}:15-27,94-102`.
   - Contract:
     - Component `app-group-pre-order-modal`, standalone, OnPush, signal API (as the card and page): `reservation = input.required<ReservationCustomer>()`, `ownerName = input('')`, `closed = output<{ changed: boolean }>()`.
     - On init: `getOwnerGroupId$(reservation.id)` → `forkJoin(getSessions$, getStatus$)` → `buildGroupPreOrderView`. A `reload()` method repeats the last two calls.
     - States: loading (spinner); error (inline text "Could not load the group pre-order." with `data-testid="gpo-error"`, no extra toast); content.
     - Content per M01/M05/M06/M07/M08/M11: `<app-modal width="800px" title="Group Pre-Order">`; header line with address, date, time, guests; "Table · {ordered} of {seats} ordered" (`data-testid="gpo-ordered-summary"`); seats row (horizontal scroll with ‹ › buttons when it overflows, M08); bonus box (texts below; progress bar = ordered / seats while `InProgress`); "My Pre-Order" row (collapsed / expanded, read-only in this step; M07 text "You haven't added your dishes yet" when not ordered); "Guests' Pre-Orders · N guests" with Expand all / Collapse all; each guest row is collapsible; a not-submitted guest shows "Waiting" and no subtotal; empty state box when no guest joined (M06); footer "Grand Total" (`data-testid="gpo-grand-total"`, shows `finalTotal` with the `currency` pipe), breakdown "My pre-order {owner | —} · Guests' pre-orders ({orderedGuestCount}) {guestsSubtotal}" and the bonus line (texts below).
     - Bonus texts (`{n}` = remaining count, singular "pre-order" for 1; `{deadline}` = `MMM d, HH:mm`; `{left}` = `formatTimeLeft`):

       | State + tier | Box heading / sub-line | Footer bonus line |
       |---|---|---|
       | InProgress + TenPercentDiscount | "{n} more pre-orders — 10% off for the whole table" / "Until {deadline} · {left}" | "Group discount −10% unlocks at {seats} of {seats} (−{unlockDiscountAmount})" |
       | InProgress + FreeDessert | "{n} more pre-orders — free dessert for the table" / "Until {deadline} · {left}" | "Free dessert unlocks at {seats} of {seats}" |
       | Achieved + TenPercentDiscount | green: "Everyone has ordered — 10% off applied" / "The discount is included in the group total" | "Total before discount {grandTotal} · Group discount −10% applied (−{discountAmount})" (US2 AC3: before, discount, final = Grand Total) |
       | Achieved + FreeDessert | green: "Everyone has ordered — free dessert for the table" / "Your reservation received a free dessert bonus for the table" | "Free dessert for the table" |
       | Missed | grey with clock: "The group bonus is no longer available" / "Not all seats ordered before {deadline}" when `orderedCount < seats`, else "The last pre-order came less than 2 h before the reservation" | "Group discount — deadline passed" |

     - Submitted (M10): badge "Sent to Restaurant" in `[modal-title-addon]`; the bonus box becomes green "Pre-order sent to the restaurant" / "{10% off applied | Free dessert for the table} · guests can no longer change their orders" (the bonus part only for `Achieved`); no footer button (D5).
     - Escape or close while no dialog is open → `closed.emit({ changed })`.
     - Page: `(groupPreOrder)` on the card opens the modal (signal `groupPreOrderReservation`); `ownerName` = first + last name; on `closed` with `changed: true` → `loadReservations()` and `cartState.refresh()`.
   - Tests (for test-writer): new `group-pre-order-modal.component.spec.ts` with a `GroupOrdersService` spy: loaded data (owner ordered 27; guests 36 and 73, both submitted; status seats 3, ordered 3, bonus `Achieved` + `TenPercentDiscount`, `discountPercent` 10) shows "3 of 3 ordered", a grand total of $122.40, and $136.00 as the total before discount; `getOwnerGroupId$` error shows `gpo-error`; status `Submitted` shows "Sent to Restaurant" and no "Submit Pre-Order" button.
   - Change: as in the contract. SCSS uses the Figma values (`M02-design-context.tsx.txt`) and the existing color tokens and button classes.
   - Check: as FE-4.

10. **FE-6 Modal actions: copy link, remove guest (M04), submit (M09, M10)**
    - Type: code
    - Area: frontend
    - Scaffold first: yes (`ConfirmDialogComponent` inputs and outputs, `buildInviteLink`)
    - Files: new `FE/shared/custom-components/confirm-dialog/confirm-dialog.component.{ts,html,scss}`, the modal files from FE-5, new `FE/shared/helpers/invite-link.ts` (`buildInviteLink(token: string): string` = `${window.location.origin}/join/${token}`), `FE/components/reservations/reservation-invite-modal/reservation-invite-modal.component.ts:51-54` (use the helper).
    - Contract:
      - `app-confirm-dialog`: inputs `title`, `message`, `cancelLabel`, `confirmLabel`, `confirmVariant: 'danger' | 'primary'`; outputs `confirmed`, `cancelled`. Overlay over the modal card (M04/M09). While a dialog is open, Escape or the modal close closes only the dialog.
      - "Copy pre-order link" (only when the group is Open): `GuestInviteService.getInviteToken$(reservation.id)` → `navigator.clipboard.writeText(buildInviteLink(token))` → the button text becomes "Link copied" for 3 s (M03b). Failure → `toast.error('Could not copy the link. Please try again.')`.
      - "Remove {name}" in an expanded guest row (only when Open) → dialog title "Remove {name} from the table?", message "{name}'s pre-order will be deleted and the seat becomes free.", buttons "Keep" / "Remove {name}" (danger). Confirm → `removeGuest$(groupId, participantId)` → `reload()`, `changed = true`. Keep → no call.
      - "Submit Pre-Order": disabled when `!canSubmit`. `remainingCount === 0` → `submit$` at once. `remainingCount > 0` → dialog "{remaining} seats haven't pre-ordered yet" (singular "seat hasn't" for 1), message "If you confirm now, the restaurant will receive {ordered} pre-orders{clause}. After confirmation, guests can't change their orders." where `{clause}` = " and the 10% group discount won't apply" (InProgress + TenPercentDiscount), " and the free dessert bonus won't apply" (InProgress + FreeDessert), "" (Missed). Buttons "Wait for guests" / "Confirm anyway". Success → `reload()` (shows M10), `changed = true`. Errors: the interceptor toast; the button is enabled again.
    - Tests (for test-writer): modal spec: copy link writes `…/join/t1` to the clipboard (spy on `navigator.clipboard.writeText`) and shows "Link copied"; "Remove Anna" → "Remove Anna" in the dialog calls `removeGuest$('g1', 'guest-anna')` and reloads; "Keep" makes no call; submit with all seats ordered calls `submit$('g1', 'r1')` without a dialog; submit with 2 empty seats opens the dialog, "Wait for guests" makes no call, "Confirm anyway" calls `submit$`; the submit button is disabled when `orderedCount` is 0. No tests for `ConfirmDialogComponent` (presentational).
    - Change: as in the contract.
    - Check: as FE-4.

11. **FE-7 Owner's dishes in the modal: edit (M02) and "Add my dishes" (M07)**
    - Type: code
    - Area: frontend
    - Scaffold first: no
    - Files: the modal files from FE-5.
    - Contract: expanded "My Pre-Order" (only when Open) shows each own dish with − qty +, price and trash (`aria-label`s "Decrease {dish}", "Increase {dish}", "Remove {dish}"). Each action builds the new item list from `sessions.owner.items` and calls `PreOrdersService.editPreOrderByReservationId(reservation.id, { orderItems })` (an empty list cancels the order, as today); success → `reload()`, `cartState.refresh()`, `changed = true`. "−" at quantity 1 is disabled. "+ Add my dishes" (owner not ordered, Open) → `closed.emit({ changed })` and `router.navigate(['/menu'], { queryParams: buildPreOrderQueryParams(reservation) })`.
    - Tests (for test-writer): modal spec: "Increase {dish}" calls `editPreOrderByReservationId('r1', …)` with that dish's quantity + 1 and the other items unchanged, then reloads; "Add my dishes" navigates to `/menu` with the pre-order query params and emits `closed`.
    - Change: as in the contract.
    - Check: as FE-4.

## Contracts and risks

- **API changes (backend wins, FE models in FE-2):** `GET /guest/groups/{id}/status` adds `orderedCount`, `bonus`. `GET /guest/groups/{id}/sessions` and `DELETE …/sessions/{sid}` add `owner`. `POST /guest/groups/{id}/submit` accepts an owner-only or a guests-only submit, creates the owner's order when it is missing, returns `[]` when no guest dish exists, and has 2 new 400 messages. No new route, so the `RouteRegistry.cs` route table does not change. `ApiRoutes.md` in BE-4.
- **Data:** new optional DynamoDB attributes `Orders.createdAt`, `GuestSessions.submittedAt`. No table, index or `deployment_resources.json` change, so no infra review. No migration; old records count as on time (Q2, accepted).
- **Security:** the interceptor sends the Cognito id token to `/guest/groups/*` only. Guest session calls keep their own token (FE-1 tests). The public status route shows the ordered count, the bonus and the deadline (Q4, accepted).
- **Money:** display-only discount (accepted). Tech-debt follow-up: store the bonus on the order for the waiter.
- **Time zones:** the bonus uses `Reservation.StartTime.ToUniversalTime()` like `OrderService.cs:296`. Tests must use `DateTimeKind.Utc` values. The DynamoDB SDK can load `DateTime` as local time; the Lambda runs in UTC.
- **Owner own items after submit** come from a subtraction. A later waiter edit of the order (status InProgress) can make them wrong. The card hides the button outside Reserved.
- **Scaffolds keep the build green** (optional constructor parameters, non-positional `Owner`, mock store values in the FE-2 scaffold). The implementer makes the 2 backend constructor parameters required in the implement turn.
- **No group for some reservations:** `PUT /reservations` that raises `guestCount` from 1 does not create a group, so `guest-session-id` returns 404 and the modal shows its error state. Tech debt.
- **Layout:** `grid-auto-rows: 1fr` makes every card as tall as the tallest one. Three small buttons fit one row at 1440 px (estimate ≈ 332 px of ≈ 370 px); they wrap on narrower cards. Check in a browser.
- **No new npm or NuGet packages.**

### Deviations from repo rules (show at the checkpoint)

- New components use signal `input()`/`output()`, OnPush and `@app/` imports, like the card and page that they extend (`CLAUDE.md` §5 describes the target differently; FE tech-debt 2.3).
- Shared `ModalComponent` gets one additive content slot (`[modal-title-addon]`).
- Design changes: no seats stepper (D2), no "Add to calendar" (D5), no remind action (D5: the copy link is the reminder), no orange dot on the card button. M04 text: "the seat becomes free" (DELETE does not reduce the seats). M11 footer: "Group discount — deadline passed". M05 footer adds "Total before discount" (US2 AC3). Free-dessert texts are not in Figma; the plan defines them (FE-5 table). Prices use the existing `currency` pipe (`$100.00`; design shows `$100`).
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
- Discount stored on the order, or shown to the waiter (tech debt).
- Larger dev mock clean-up `FE/shared/mocks/group-orders/*`: owner in `participants`, wrong submit path `/group-reservations/{id}/submit`, fixed `bonus`. Dev `?demo` mode differs from the real API. Tech debt.
- `ApiRoutes.md` drift for `/guest-sessions/{id}/dishes` (`:1139,1154`) and the order `status` type (stay in FE tech-debt 2.4).
- `PUT /reservations` does not create or close the group reservation (`EditReservationHandler.cs`).
- Unused FE helper `consolidate-pre-order.ts` and its duplicate interfaces; Ukrainian comments in mock and config files.
- Group submit 30-minute cutoff, reservation status check (Q6) and waiter notification.
- Two constants for the group size (FE tech-debt 1.5).
