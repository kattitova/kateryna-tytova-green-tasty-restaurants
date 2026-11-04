# Backend tech debt: error handling and group session DELETE

Status: open. Recorded on 2026-11-02 during the work on `GET /guest/groups/{groupReservationId}/sessions`. Every item existed before that work.

Paths below are relative to `backend/restaurant-backend-app/dnapp/lambdas/api-handler/` unless they start with `docs/` or `swagger_src/`. Line numbers are from commit `bb46d3f`, except in items 1.6 to 1.8. The DELETE task of 2026-11-02 added items 1.6 to 1.8, and their line numbers are from that change.

Priority:

- **P1**: a wrong status code or a 500 that a normal client can trigger.
- **P2**: wrong or missing documentation, or a code smell with no user-visible effect today.

## 1. `DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}`

The DELETE task of 2026-11-02 fixed items 1.1 (the DELETE part), 1.2, 1.3, 1.4 and 1.5. DELETE now accepts guest sessions only, in an `Open` group, and returns a camelCase `{ participants, total }` body of the remaining guests. A DELETE of the owner session returns 400 `OwnerSessionNotDeletable`. `docs/ApiRoutes.md` and Swagger document the route. Items 1.6 to 1.8 are still open.

### 1.6 `GetGuestSessionInfoAsync` throws on a deleted dish (P1)

- **Where:** `Services/GuestSessions/GuestSessionService.cs:396` (`GetGuestSessionInfoAsync`).
- **Problem:** the method reads `dishesById[d.DishId]`. If a dish in the session no longer exists, the indexer throws `KeyNotFoundException`, and the client gets 500. The DELETE task did not change this method.
- **Fix:** use `TryGetValue` and skip unknown dishes, as `ToGuestParticipant` does. Add a test with a dish id that the dish repository does not return.

### 1.7 DELETE is not atomic with the group status or with guest writes (P2)

- **Where:** `Services/GuestSessions/GuestSessionService.cs` (`DeleteGuestSessionAsync`), `Repositories/GuestSessions/GuestSessionRepository.cs` (`DeleteAsync`, `SaveAsync`).
- **Problem:** two races.
  - The service reads the group status, then deletes with no condition. A group submit between the two can make an order that has the dishes of a deleted session.
  - A guest who adds or deletes a dish at the same time can write the whole session item again after the delete. `SaveAsync` writes the full item, and `GuestSession` has no version field. The deleted session then comes back.
- **Fix:** use conditional writes. Delete in a DynamoDB transaction with a condition check on the `GroupReservations` item (`status = Open`). Add `[DynamoDBVersion]` to `GuestSession`, or an `attribute_exists` condition on the guest save.

### 1.8 Test cleanup in `ResponseBuilderTests` (P2)

- **Where:** `Tests/Helpers/ResponseBuilderTests.cs` (path relative to `backend/restaurant-backend-app/`).
- **Problem:** in the `OwnerSessionNotDeletable` `[Fact]`, the five asserts on the parsed JSON cannot fail after the exact-body assert passes. `ParseBody` does not dispose the `JsonDocument`.
- **Fix:** delete the redundant asserts, or keep only the parsed asserts. Dispose the `JsonDocument` with `using`.

## 2. Error status codes across the guest endpoints

### 2.1 "Not found" cases return 400 with `errorCode: Unknown` (P1)

- **Where:** the handlers send every `ValidationException` that they do not catch first to `ResponseBuilder.CreateErrorCodeResponse`, which always returns 400. The service throws `*NotFoundException` types, which are subclasses of `ValidationException`.
- **Cases:**
  - Guest session not found: add dish, remove dish, menu, submit cart.
  - Group reservation not found: add dish, remove dish, create session, submit cart.
  - Parent reservation not found: add dish, remove dish, menu.
- **Problem:** the client gets `400 {"message": "...", "errorCode": "Unknown"}` and cannot tell "not found" from bad input.
- **Fix:** catch the `*NotFoundException` types before `ValidationException` in each handler and return 404 with `CreateErrorResponse`. As an alternative, map them in one place in `CreateErrorCodeResponse`. Update `ApiRoutes.md` and Swagger in the same change.

### 2.2 A malformed guest token causes a 500 (P1)

- **Where:** `Services/GuestSessions/GuestSessionService.cs:36` (`IsAuthorizedAsync`).
- **Problem:** `Convert.FromBase64String(guestSessionToken)` throws `FormatException` for a token that is not base64. The call runs outside the handler `try` blocks, so the client gets 500. It must get 401 "Invalid guest session token".
- **Fix:** treat a token that is not base64 as not authorized. Add a test for it.

### 2.3 A missing body on add dish and remove dish causes a 500 (P1)

- **Where:** `Handlers/GuestSessions/AddDishToGuestSessionHandler.cs:73` and `Handlers/GuestSessions/RemoveDishFromGuestSessionHandler.cs:72`.
- **Problem:** `JsonSerializer.Deserialize(request.Body)` throws `ArgumentNullException` when `Body` is null. `TryDeserializeRequest` catches only `JsonException`, so the client gets 500. It must get 400.
- **Fix:** return false for a null or empty body before the call to `Deserialize`.

### 2.4 Some 500 bodies show internal exception messages (P1)

- **Where:**
  - `Handlers/GuestSessions/AddDishToGuestSessionHandler.cs` (`catch (Exception ex)`)
  - `Handlers/GuestSessions/RemoveDishFromGuestSessionHandler.cs` (`catch (Exception ex)`)
  - `Handlers/GuestSessions/GetGroupReservationStatusHandler.cs:33-36`
- **Problem:** these handlers return `500 {"error": "Internal server error: <ex.Message>"}`. An internal message can show table names, IDs or library details to the client.
- **Fix:** delete these catch-all blocks. `Function.cs` already sends unhandled exceptions to `Services/ExceptionHandler.cs`, which logs the exception and returns `{"error": "Internal Server Error"}`.

### 2.5 `ReservationNotFoundException` ignores its message (P2)

- **Where:** `Exceptions/ReservationNotFoundException.cs:5`.
- **Problem:** the constructor takes `message` but always passes `"Reservation not found."` to the base class. Messages such as "Parent reservation not found." never reach the client.
- **Fix:** pass `message` to the base class with a default value, as `GroupReservationExpiredException` does. Check the docs that quote the current text (`docs/ApiRoutes.md`, `GET .../sessions` entry).

## 3. Documentation drift

### 3.1 Swagger does not list some error codes (P2)

- **Where:** `swagger_src/swagger-ui/swagger-endpoints.yaml`.
- **Missing:**
  - `submitGuestCart`: 401 (from `GuestAuthorizationService`).
  - `getGuestSessionMenu`: 403 (`ActionForbiddenException`).
  - 500 (from `ExceptionHandler`) on `getGroupReservationByToken`, `getAllSessionsForGroup`, `submitGuestCart`, `getGuestSessionMenu`, `getInviteToken` and `deleteGuestSession`.
- **Fix:** add each code with the schema that the code sends. Do this after the fixes in section 2, because they change some codes.

### 3.3 `docs/ApiRoutes.md` uses old paths (P2)

- **Where:** `docs/ApiRoutes.md:1113`, `:1128`, `:1172`.
- **Problem:** these entries use `/guest-sessions/...` and `/group-reservations/...`. The code uses `/guest/sessions/...` and `/guest/groups/...`.
- **Fix:** change each path to the route in `Routing/RouteRegistry.cs`, and check each body against its handler.
- **Also:** `homework-backend.md:78` names the submit path as `/group-reservations/{groupReservationId}/submit`. The route is `POST /guest/groups/{groupReservationId}/submit` (user decision, ADR 0002).

### 3.4 The 401 row of `GET .../sessions` is not complete (P2)

- **Where:** `docs/ApiRoutes.md`, entry `GET /guest/groups/{groupReservationId}/sessions`.
- **Problem:** the 401 row does not say that a `sub` claim that is empty or contains only white space also gets 401. The handler uses `IsNullOrWhiteSpace`.
- **Fix:** use the same 401 text as the `guest-session-id` and `submit` entries.

## 4. Follow-ups from the owner session and group submit task (ADR 0002)

Recorded on 2026-11-02. Item 4.3 existed before that task.

### 4.1 The frontend does not call the new endpoints (fixed)

- **Fixed on 2026-11-04** by the Group Pre-Order modal task: `GroupOrdersService` calls the owner group id, sessions, status, DELETE session and group submit routes.

- **Where:** `frontend/restaurant-frontend-app`.
- **Problem:** no frontend model or call exists for `GET /reservations/{reservationId}/guest-session-id` (response `{"guestSessionId"}`) and `POST /guest/groups/{groupReservationId}/submit` (request `{"reservationId"}`, response `[{"dishId", "quantity"}]`). The user approved a separate frontend task.
- **Fix:** add the models and the service calls in one frontend task. Use `docs/ApiRoutes.md` as the contract.
- **Also (2026-11-02):** no frontend model or call exists for `DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}`. Add a participant model and a `{ participants, total }` response model, a `deleteGroupGuestSession(groupReservationId, guestSessionId)` call, and handling for the `OwnerSessionNotDeletable` error code. The user approved a separate frontend task.

### 4.2 The owner session cannot use the guest endpoints (P2)

- **Where:** `Services/GuestSessions/GuestSessionService.cs` (`IsAuthorizedAsync`, owner session creation).
- **Problem:** the owner session has no token hash, so every `/guest/sessions/{guestSessionId}/...` endpoint returns 401 for it. The owner's own dishes stay in the `Order`.
- **Fix:** decide with the frontend task (4.1) how the owner adds dishes in a group flow. Then give the owner session a token, or let the owner act on it through the Cognito `sub`.

### 4.3 The Lambda IAM policy is too broad (P1)

- **Where:** Syndicate policy `lambda-basic-execution` in `deployment_resources.json`, used by `api-handler-role`.
- **Problem:** the policy allows its DynamoDB, S3, SQS, Cognito and SES actions on `"Resource": "*"`. The role trust policy has no `aws:SourceAccount` condition. This task did not widen it.
- **Fix:** limit each action to the table, bucket, queue and user pool ARNs. Add `aws:SourceAccount` to the trust policy. The group submit transaction needs only `dynamodb:UpdateItem` on the `Orders` and `GroupReservations` tables. Ask `infra-reviewer` to review the change.

### 4.4 A handler namespace does not match its folder (P2)

- **Where:** `Handlers/GroupReservations/GetGuestReservationHandler.cs`.
- **Problem:** the file declares `APIHandler.Handlers.GuestReservations`. The other files in the folder use `APIHandler.Handlers.GroupReservations`.
- **Fix:** change the namespace and the `using` lines that refer to it.

## 5. Follow-ups from the Group Pre-Order modal task

Recorded on 2026-11-04 (task folder `.claude/work/2026-11-04-group-preorder-modal/`).

### 5.1 The group discount is not stored on the order (fixed)

- **Fixed on 2026-11-04** by the group bonus and edit group task: `SubmitGroupPreOrderAsync` stores `groupBonusTier` and `discountPercent` on the order. `GET /orders` and `GET /orders/{reservationId}` return them, and the waiter pre-order page and the waiter card show them. See `docs/decisions/0002-owner-session-and-group-submit.md`, the amendment of 2026-11-04 (2).

- **Where:** `Services/GroupReservations/GroupPreOrderService.cs` (`SubmitGroupPreOrderAsync`), `Entities/Order.cs`.
- **Problem:** only the owner's modal shows the 10% discount or the free dessert. The order has no bonus field, so the waiter does not see the bonus. The user accepted this for now.
- **Fix:** at group submit, save the bonus tier and the discount percent on the order. Show them in the waiter UI.

### 5.2 A waiter edit can break the owner's own items (P2)

- **Where:** `Services/GroupReservations/GroupPreOrderRules.cs` (`OwnerOwnItems`).
- **Problem:** after a group submit, the owner's own items are the order items minus the submitted guests' dishes. If a waiter changes the order, the result can be wrong.
- **Fix:** keep the owner's own items as a separate list on the order at submit time.

### 5.3 `PUT /reservations` does not create or close the group (fixed)

- **Fixed on 2026-11-04** by the group bonus and edit group task: `EditReservationHandler` calls `SyncGroupWithGuestCountAsync` after the save. See `docs/ApiRoutes.md`, `PUT /reservations`.

- **Where:** `Handlers/Reservation/EditReservationHandler.cs`.
- **Problem:** a change of `guestCount` from 1 to 2 or more creates no group reservation. `GET /reservations/{id}/guest-session-id` then returns 404, and the owner modal shows its error state.
- **Fix:** create or close the group reservation when the guest count crosses 2.

### 5.4 `GroupBonusCalculator` clamps the deadline at `DateTime.MinValue` (P2)

- **Where:** `Services/GroupReservations/GroupBonusCalculator.cs`.
- **Problem:** the clamp exists only because 3 status tests use a default reservation start time.
- **Fix:** give those tests a real start time and remove the clamp.

### 5.5 No partial-subtraction test for `OwnerOwnItems` (P2)

- **Where:** `Tests/Services/GroupPreOrderRulesTests.cs`.
- **Problem:** no test covers an owner dish that a guest also ordered, where the rule takes away only part of the quantity.
- **Fix:** add one test row.

## 6. Follow-ups from the group bonus and edit-group task

Recorded on 2026-11-04 (task folder `.claude/work/2026-11-04-group-bonus-and-edit-group/`).

### 6.1 `GET .../invite-token` returns 500 when the group does not exist (P1)

- **Where:** `Services/GroupReservations/GroupReservationService.cs` (`GetGroupReservationInviteTokenAsync`, the `!` on the group).
- **Problem:** a reservation with 2 or more guests and no group gives a null reference, so the client gets 500. Old reservations that were edited to 2 or more guests before this task have no group.
- **Fix:** return 404 when the group does not exist. Add a test.

### 6.2 Old reservations with 2 or more guests have no group (P2)

- **Where:** `Reservations` and `GroupReservations` tables.
- **Problem:** a reservation that was edited to 2 or more guests before this task gets a group only at its next edit.
- **Fix:** a one-time script that creates the missing groups, or create the group on the first `GET .../guest-session-id`.

### 6.3 The status route does not read the stored bonus (P2)

- **Where:** `Services/GuestSessions/GuestSessionService.cs` (`GetGroupReservationStatusAsync`).
- **Problem:** after a submit, the route computes the bonus again. The live bonus can differ from the bonus on the order. Related to item 5.2.
- **Fix:** after a submit, return `Order.GroupBonusTier` and `Order.DiscountPercent`.

### 6.4 Three group lifecycle rules (P2)

- **Where:** `Services/Reservations/ReservationService.cs` (create, `UpdateReservationAsync`) and `GroupReservationService.SyncGroupWithGuestCountAsync`.
- **Problem:** three code paths create or close the group. They share only `GroupReservationRules.MinGuestCount`.
- **Fix:** use `SyncGroupWithGuestCountAsync` in all three paths.

### 6.5 The 2-guest rule is not in one place (P2)

- **Where:** `Handlers/Reservations/GetReservationsForUserHandler.cs:157` (`> 1`), frontend `isGroupReservation`.
- **Problem:** these do not use `GroupReservationRules.MinGuestCount`.
- **Fix:** use the constant in the backend. Send the flag to the frontend, or document the rule.

### 6.6 The API accepts an edit below the number of joined guests (P2)

- **Where:** `Handlers/Reservations/EditReservationHandler.cs`.
- **Problem:** for example, 4 to 2 guests with 3 joined guests passes validation.
- **Fix:** reject with 400, or define what happens to the extra guests.

### 6.7 The reservation save and the group sync are two writes (P2)

- **Where:** `Handlers/Reservations/EditReservationHandler.cs`.
- **Problem:** if the sync fails after the save, the API returns 500, but the save already changed the reservation. If the user sends the same edit again, the sync runs again and sets the right group status.
- **Fix:** one DynamoDB transaction for both writes.

### 6.8 `EditReservationHandler` uses `IDynamoDBContext` directly (P2)

- **Where:** `Handlers/Reservations/EditReservationHandler.cs`.
- **Problem:** the handler does data access itself. The message "Location not foun.d" has a typo.
- **Fix:** move the logic into `ReservationService`. Fix the message.

### 6.9 `ApiRoutes.md` shows the order `status` as a string (P2)

- **Where:** `docs/ApiRoutes.md`, order responses.
- **Problem:** the API sends `status` as a number.
- **Fix:** show the number and its values.

### 6.10 Small code and test cleanups (P2)

- **Where:** `Converters/NullableEnumAsStringConverter.cs`, `Tests/Entities/OrderStorageTests.cs`, `Tests/Services/GroupPreOrderRulesTests.cs`.
- **Problem:** `Enum.TryParse` after the name check is not necessary. No test reads a stored `S = "TenPercentDiscount"` through the SDK. The "latest-first" guest order in a `Progress` test has no effect, because the code uses `Max()`.
- **Fix:** use `Enum.Parse`. Add the read test. Remove or change the guest order in the test.

## 7. Follow-ups from the guest re-submit task

Recorded on 2026-11-04 (task folder `.claude/work/2026-11-04-guest-resubmit-seat/`).

### 7.1 The guest section of `ApiRoutes.md` does not agree with the code (P2)

- **Where:** `docs/ApiRoutes.md` (guest routes).
- **Problem:** `POST /guest/sessions` returns 200 with `SessionId` and `SessionToken`, not 201 with `sessionToken`. The dish routes are `/guest/sessions/{id}/dishes`, not `/guest-sessions/{id}/dishes`. The `GET /guest/sessions/{id}` shape and the submit message are different. The 409 for a submitted session and the 400 `GuestLimitExceeded` are not documented.
- **Fix:** copy the routes, bodies and errors from the code.

### 7.2 `POST /guest/sessions` does not stop a duplicate guest (P2)

- **Where:** `Services/GuestSessions/GuestSessionService.cs` (`CreateGuestSessionAsync`).
- **Problem:** the only check is the seat limit. The same person can take a second seat with the same name (see frontend tech debt 5.1).
- **Fix:** reject a second session with the same name in the group (409), or reuse the session for a known token.

## Suggested order

1. Items 1.6, 2.2, 2.3 and 2.4: small fixes with a test each.
2. Item 2.1: changes the API contract. Agree on it with the frontend first.
3. Items 2.5 and section 3: docs, after the code changes above.
4. Item 1.7: conditional writes, as one `/orchestrate` task.
5. Item 4.3: infra, with `infra-reviewer`.
6. Items 4.1 and 4.2: one frontend task, with a backend change if 4.2 needs one.
7. Item 1.8: when you next change `ResponseBuilderTests`.
