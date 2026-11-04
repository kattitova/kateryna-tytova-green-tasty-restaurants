# ADR 0002: Owner session id and consolidated group pre-order submit

- **Status:** Accepted
- **Date:** 2026-11-02
- **Commit:** 7767a75
- **Area:** backend (`api-handler` Lambda), API Gateway config (Syndicate)

## Context

The owner of a group reservation needs two new functions:

1. Get the id of the owner's own `GuestSession` for a reservation.
2. Submit one consolidated pre-order. The request collects the dishes of the submitted guest sessions and adds them to the order of the reservation.

Facts that shaped the decisions:

- ADR 0001 says: "No code creates an owner `GuestSession` today." This ADR changes that fact. `GET /reservations/{reservationId}/guest-session-id` now creates the owner session.
- Before this change, `POST /guest/sessions`, `GET /guest/invites/{token}` and `GET /guest/groups/{groupReservationId}/status` counted every session as a joined guest. An owner session would take a guest place.
- A reservation has one `Order`. The submit must change two items: the `Orders` item and the `GroupReservations` item. Two separate saves can leave one item changed and the other not.
- `GuestSession` has one hash key, `guestSessionId`. `SaveAsync` overwrites an item with the same key.
- `GroupReservation.Status` has no converter, so the SDK stores `status` as a number (N): `Open` = 0, `Submitted` = 1, `Closed` = 2.

## User decisions

These come from the task discussion on 2026-11-02:

1. **The owner session can be missing.** `GET /reservations/{reservationId}/guest-session-id` creates it on the first call, then returns its id.
2. **Submit path.** The endpoint is `POST /guest/groups/{groupReservationId}/submit`, as in the code convention and Swagger. The task text said `POST /group-reservations/{groupReservationId}/submit`. The user approved this deviation.
3. **"Add to order".** The consolidated guest dishes merge into the existing order items by `dishId`. The response contains only the consolidated guest dishes.
4. **Re-submit.** A group reservation with status `Submitted` gives 400.
5. **Frontend.** No frontend change in this task. The frontend follow-up for both endpoints is in `docs/tech-debt.md`.
6. **Plan defaults.** The user approved D3, D5, D5b, D8, D9 and D11 as written below.
7. **Transaction.** The submit writes the order and the group status in one DynamoDB `TransactWriteItems` call. The group item has the condition `status = Open`.

## Decision

### Owner session id

1. **Only the owner can call both endpoints.** The owner is the Cognito user whose `sub` equals `Reservation.CustomerId` (ordinal compare). A reservation without a customer has no owner. Not the owner → 403 `{"error": "Only the reservation owner can perform this action."}`.
2. **Fixed id.** The owner session id is `owner-{groupReservationId}`, for example `owner-group-1`. `GuestSessionService.OwnerSessionIdFor` builds it in one place.
3. **Get or create.** The service checks the reservation (404), the owner (403) and the group reservation of the reservation (404). Then it loads `owner-{groupReservationId}`. If the session does not exist, the service saves it. Then it returns `{"guestSessionId": "owner-{groupReservationId}"}`.
4. **The new owner session** has `GuestName = "Owner"`, empty `Dishes`, `IsOwnerSession = true`, `OwnerUserId` = the caller's `sub`, `IsSubmitted = false` and no `SessionTokenHash`.
5. **Every group status.** The request reads and creates the owner session in `Open`, `Submitted` and `Closed` (D8).
6. **Parallel first calls.** Two parallel first calls write the same key with equal values, so one item stays. This is safe only because `guestSessionId` is the single hash key and `SaveAsync` overwrites. Unit tests cannot prove it. Only a deployed test can prove it.

### Owner sessions are not guests

7. **One rule.** `GuestSessionExtensions.GuestsOnly()` keeps the sessions with `IsOwnerSession == false`. These callers use it (D9): guest limit on `POST /guest/sessions` and `GET /guest/invites/{token}`, `currentParticipants` (guests + 1), and `GET /guest/groups/{groupReservationId}/sessions`. The exception is `DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}`. It does not use `GuestsOnly()` yet (see Consequences).
8. **One query.** This change deletes `IGuestSessionRepository.CountByGroupReservationId`. The callers use `GetAllByGroupReservationIdAsync`, which runs the same index query.

### Consolidated submit

9. **Service.** The submit logic is in the new `GroupPreOrderService` (D10). The checks run in this order:
   1. Reservation not found → 404 `{"error": "Reservation not found."}`.
   2. Not the owner → 403.
   3. Group reservation not found, or it belongs to a different reservation → 404 `{"error": "Group reservation was not found"}` (D3).
   4. Group status `Submitted` → 400 `GroupReservationSubmitted`. Status `Closed` → 400 `GroupReservationExpired` (D2).
   5. No session with `IsSubmitted == true` → 400 `errorCode: "Unknown"`. Submitted owner sessions also count (D4).
   6. No dishes in the submitted sessions → 400 `errorCode: "Unknown"`.
   7. No order for the reservation → 400 `errorCode: "Unknown"`.
10. **Consolidation.** The service adds the quantities of each `dishId`. The list keeps the order of first occurrence.
11. **Merge into the order** (D5, D5b):
    - An item with the same `dishId` and state `Pending` or `Confirmed` gets the quantity added and becomes `Pending`.
    - An item with the same `dishId` and state `Canceled` gets the guest quantity only and becomes `Pending`.
    - A dish without an item becomes a new `Pending` item at the end.
    - Other items do not change. One `dishId` has one item.
12. **No other checks** (D6). The submit has no reservation-status check, no 30-minute cut-off, no order-status precondition and no dish re-validation. `OrderService.SubmitPreOrderAsync` has none of them either.

### Transaction (D12, D13)

13. **One `TransactWriteItems` call.** `GroupReservationRepository.SubmitGroupPreOrderAsync` builds two parts through `IDynamoDBContext.CreateMultiTableTransactWrite`, in this order:
    1. **Order part:** `AddSaveItem(order)`. SDK 4.0.1.5 sends it as an `Update` that sets every non-key attribute, with no condition. It is not a `Put`. `SaveOrderAsync` sends the same `UpdateItem` write.
    2. **Group part:** an `Update` on key `groupReservationId` with `SET #status = :submitted` and the condition `#status = :open`. `#status` is `status`. `:submitted` is the number 1 and `:open` is the number 0.
14. **Condition failure.** DynamoDB returns the cancellation reasons in item order. If the reason for the second item (index 1, the group part) is `ConditionalCheckFailed`, the repository returns `false`. The service then gives 400 `GroupReservationSubmitted`. A second submit, a parallel submit and a group closed in parallel all get this response.
15. **Other failures.** Any other cancel reason (for example `TransactionConflict`) or other exception goes to `Function.cs` → 500 `{"error": "Internal Server Error"}`. DynamoDB writes nothing in that case, so a retry is safe. The retry gets 200 or 400.
16. **IAM.** Both items are `UpdateItem` writes. The transaction needs only `dynamodb:UpdateItem` on the orders table and on the group reservations table. The plan text said `dynamodb:PutItem` for the order part. That statement is wrong. The `lambda-basic-execution` policy already allows `dynamodb:UpdateItem`.
17. **Request shape.** The step 4 reviewer ran a local probe against SDK 4.0.1.5. The probe showed one `TransactWriteItems` request with the expected expressions, names and number values. So the low-level API is not necessary. The documented fallback stays: build the request with `IAmazonDynamoDB.TransactWriteItemsAsync` if a later SDK version sends a different request.

### API Gateway

18. **Two new resources** in `deployment_resources.json`: `GET /reservations/{reservationId}/guest-session-id` and `POST /guest/groups/{groupReservationId}/submit`. Both use `cognito_authorizer`. No new table, IAM statement or environment variable.

## Alternatives considered

| Alternative                                                            | Why we did not choose it                                                                                                                                                                                   |
| ---------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Two separate saves: `SaveOrderAsync`, then `SaveGroupReservationAsync` | A failure between the two writes leaves the order submitted and the group open, or the opposite. A parallel submit can also merge the guest dishes two times. The user chose the transaction (decision 7). |
| Random GUID as the owner session id                                    | Two parallel first calls create two owner sessions. The fixed id `owner-{groupReservationId}` makes the create idempotent.                                                                                 |
| Count owner sessions as guests                                         | An owner session takes a guest place, and the guest limit becomes wrong.                                                                                                                                   |
| `POST /group-reservations/{groupReservationId}/submit` (task text)     | Other group routes use `/guest/groups/...`. The user approved the change (decision 2).                                                                                                                     |
| Return the merged order in the submit response                         | The user chose the consolidated guest dishes only (decision 3).                                                                                                                                            |
| Low-level `IAmazonDynamoDB.TransactWriteItemsAsync`                    | The DataModel API sends the expected request (decision 17). The low-level API stays as the fallback.                                                                                                       |

## Consequences

- The task added 102 unit tests (172 before, 274 after). They cover `GuestsOnly()`, the guest limit and participant count, the owner session service and handler, the transaction repository, the submit service and the submit handler.
- `POST /guest/sessions`, `GET /guest/invites/{token}` and `GET /guest/groups/{groupReservationId}/status` no longer count owner sessions. The visible result changes only after an owner session exists.
- The owner session has no guest token. The `/guest/sessions/{guestSessionId}` endpoints do not accept it. The owner's own dishes stay in the `Order`.
- `DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}` does not use `GuestsOnly()`. It lists the owner session as a guest named "Owner", and the owner can delete it. The next GET creates it again. See `docs/tech-debt.md`, item 1.1.
- The order part writes the full order that the service read. A parallel `PUT /orders/{reservationId}` between the read and the transaction is lost. `OrderService` has the same pattern.
- The frontend has no model or call for the two endpoints yet.

## Deploy checks

Only a deployed stack can check these:

1. `GET /reservations/{reservationId}/guest-session-id` two times → both 200 with the same `owner-{groupReservationId}`. The table has one owner session item.
2. First `POST /guest/groups/{groupReservationId}/submit` → 200. The order has status `SubmittedByCustomer` (stored as 3), and the group has `status` 1.
3. Second submit with the same body → 400 `{"message": "Group reservation was submitted.", "errorCode": "GroupReservationSubmitted"}`.
4. `status` in the `GroupReservations` table is a number (N). If it is a string, the condition never matches, and every submit gives 400.
5. The Lambda role can run the transaction (`dynamodb:UpdateItem` on both tables).
6. API Gateway routing and the Cognito authorizer on both resources.

## Amendment 2026-11-04: owner own items and at least one pre-order

- **Owner own items.** One rule, `GroupPreOrderRules.OwnerOwnItems`, gives the owner's items: the order items that are not `Canceled`. When the group status is `Submitted`, the rule subtracts the quantities of the submitted guest dishes, because the submit merged them into the order. The status (`orderedCount`), the sessions snapshot (`owner`) and the submit use this rule.
- **Submit (changes checks 5, 6 and 7 of decision 9).** The submit needs at least one pre-order: a submitted guest dish, or owner own items. If there is none, it returns 400 `Nothing to submit: the group has no pre-orders.`. If the reservation has no order, the submit creates the order of the owner (the table location gives the address). If the location is missing, it returns 400 `Location not found for the reservation's table.`. The response can be `[]`.
- **New data.** `Order.createdAt` and `GuestSession.submittedAt` are optional. Records from before this change count as on time for the group bonus.
- **Status and sessions.** `GET /guest/groups/{groupReservationId}/status` adds `orderedCount` and `bonus`. The sessions and delete responses add `owner`. See `docs/ApiRoutes.md`.

## Amendment 2026-11-04 (2): stored group bonus and group sync on edit

- **Stored bonus.** The bonus no longer lives only in the status route. At group submit, the service computes the bonus from the order before the merge, with `GroupPreOrderRules.Progress` and `GroupBonusCalculator`. If the state is `Achieved`, the order stores `groupBonusTier` and `discountPercent`. Otherwise the order stores `null` and 0, and this replaces an old value. Later order changes do not compute the bonus again. The status route stays live, so the two values can be different after the submit.
- **Storage.** `TenPercentDiscount` is the enum value 0, so the order keeps the tier as its name through `NullableEnumAsStringConverter`. A null tier deletes the attribute on write (`REMOVE` in the submit transaction, `DELETE` in `SaveAsync`), and the SDK does not call the converter for null. A missing attribute reads as null, and the converter is not called. A stored `NULL` calls `FromEntry(DynamoDBNull)`, which returns null. A number gives `ArgumentOutOfRangeException`.
- **Responses.** `GET /orders`, `GET /orders/{reservationId}` and `POST /orders` add `groupBonusTier`, `discountPercent` and `orderItems[].state`. See `docs/ApiRoutes.md`.
- **Waiter UI.** The waiter pre-order page applies `discountPercent` to the current items that are not `Canceled`. After a waiter change, the waiter total can be different from the amount that the owner saw. The waiter card shows a badge for the bonus.
- **Edit sync.** `PUT /reservations` calls `IGroupReservationService.SyncGroupWithGuestCountAsync` after the save. This applies to a reservation with a customer and the status `Reserved`. With 2 guests or more, the sync creates a group or re-opens a `Closed` group. With fewer than 2 guests, it closes an `Open` group. A `Submitted` group does not change. Guest sessions stay. The threshold is `GroupReservationRules.MinGuestCount`, which `ReservationService` and `GroupReservationService` also use.

## References

- Contract: `docs/ApiRoutes.md`, entries `GET /reservations/{reservationId}/guest-session-id` and `POST /guest/groups/{groupReservationId}/submit`. Swagger: `swagger_src/swagger-ui/swagger-endpoints.yaml`, `getOwnerSessionId` and `submitGroupPreOrder`.
- Code: `Helpers/GuestSessionExtensions.cs`, `Services/GuestSessions/GuestSessionService.cs` (`GetOrCreateOwnerSessionIdAsync`), `Services/GroupReservations/GroupPreOrderService.cs`, `Repositories/GroupReservations/GroupReservationRepository.cs` (`SubmitGroupPreOrderAsync`), `Handlers/GuestSessions/GetOwnerSessionIdHandler.cs`, `Handlers/GroupReservations/SubmitGroupPreOrderHandler.cs`, `Routing/RouteRegistry.cs`
- Infra: `deployment_resources.json` (API Gateway resources)
- Tests: `Tests/Helpers/GuestSessionExtensionsTests.cs`, `Tests/Services/GuestSessionServiceTests.cs`, `Tests/Services/GroupReservationServiceTests.cs`, `Tests/Services/GroupPreOrderServiceTests.cs`, `Tests/Repositories/GroupReservationRepositoryTests.cs`, `Tests/Handlers/GetOwnerSessionIdHandlerTests.cs`, `Tests/Handlers/SubmitGroupPreOrderHandlerTests.cs`
- Previous decision: `docs/decisions/0001-group-sessions-endpoint.md`
- Tests (amendment 2): `Tests/Services/GroupPreOrderRulesTests.cs`, `Tests/Converters/NullableEnumAsStringConverterTests.cs`, `Tests/Entities/OrderStorageTests.cs`, `Tests/Helpers/OrderMapperTests.cs`, `Tests/Handlers/EditReservationHandlerTests.cs`
- Debt: `docs/tech-debt.md`, item 1.1
