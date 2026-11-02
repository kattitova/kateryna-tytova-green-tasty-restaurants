# Backend tech debt: error handling and group session DELETE

Status: open. Recorded on 2026-11-02 during the work on `GET /guest/groups/{groupReservationId}/sessions`. Every item existed before that work.

Paths below are relative to `backend/restaurant-backend-app/dnapp/lambdas/api-handler/` unless they start with `docs/` or `swagger_src/`. Line numbers are from commit `bb46d3f`.

Priority:

- **P1**: a wrong status code or a 500 that a normal client can trigger.
- **P2**: wrong or missing documentation, or a code smell with no user-visible effect today.

## 1. `DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}`

Fix these items together in one task. The response shape decision (1.2) changes the docs (1.4). The frontend does not call this endpoint yet, so now is the cheapest time to change its response.

### 1.1 A deleted dish causes a 500 (P1)

- **Where:** `Services/GuestSessions/GuestSessionService.cs:206` (`DeleteGuestSessionAsync`) and `:385` (`GetGuestSessionInfoAsync`).
- **Problem:** both methods read `dishesById[d.DishId]`. If a dish in a session no longer exists, the indexer throws `KeyNotFoundException`, and the client gets 500.
- **Fix:** use the private helper `ToGuestParticipant`, which `GetGroupSessionsAsync` already uses. It skips unknown dishes with `TryGetValue`. This also deletes the duplicated item and total code in the three methods.
- **Before the fix:** write tests for `DeleteGuestSessionAsync` that pin its current behaviour. It has no tests today.
- **Decision needed:** `DeleteGuestSessionAsync` does not filter out owner sessions (`IsOwnerSession`). `GetGroupSessionsAsync` calls `ToGuestParticipant` after that filter. Decide whether DELETE must also return guests only.

### 1.2 The response is PascalCase (P2)

- **Where:** `Handlers/GuestSessions/DeleteGuestSessionHandler.cs:41`.
- **Problem:** the handler uses `ResponseBuilder.CreateSuccessResponse(object)`, which serializes `GetAllSessionsResponse` with `Participants`, `Total`, `Id` and so on. `GET .../sessions` returns the same type in camelCase through `CreateJsonResponse`.
- **Fix:** use `ResponseBuilder.CreateJsonResponse(200, updatedSnapshot)`.

### 1.3 The handler accepts an empty `sub` claim (P2)

- **Where:** `Handlers/GuestSessions/DeleteGuestSessionHandler.cs:32-36`.
- **Problem:** the handler returns 401 only when the `sub` claim is missing. An empty or whitespace `sub` reaches the service. `GetGroupSessionsHandler` rejects it.
- **Fix:** use the same null-safe check as `GetGroupSessionsHandler`.

### 1.4 `docs/ApiRoutes.md` is wrong for this route (P2)

- **Where:** `docs/ApiRoutes.md:1236`.
- **Problem:** the entry documents `{"message": "Guest session removed from group successfully."}`. The handler returns the updated participant list (`GetAllSessionsResponse`).
- **Fix:** after 1.2, document the real response and the error codes, in the same format as the `GET .../sessions` entry above it.

### 1.5 Swagger does not document 403 (P2)

- **Where:** `swagger_src/swagger-ui/swagger-endpoints.yaml`, operation `deleteGuestSession`.
- **Problem:** the handler returns 403 when the caller is not the owner. Swagger lists only 400, 401 and 404.
- **Fix:** add `'403'` with `ErrorResponse`.

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
  - `deleteGuestSession`: 403 (see 1.5).
  - 500 (from `ExceptionHandler`) on `getGroupReservationByToken`, `getAllSessionsForGroup`, `submitGuestCart`, `getGuestSessionMenu`, `getInviteToken` and `deleteGuestSession`.
- **Fix:** add each code with the schema that the code sends. Do this after the fixes in section 2, because they change some codes.

### 3.2 Swagger documents paths that have no route (P2)

- **Where:** `swagger_src/swagger-ui/swagger-endpoints.yaml`: `POST /guest/groups/{groupReservationId}/submit` and `GET /reservations/{reservationId}/guest-session-id`.
- **Problem:** `Routing/RouteRegistry.cs` has no route for these paths. A call gets 404 "Not Found" from `Function.cs`.
- **Fix:** delete them from Swagger, or implement them if the product still needs them.

### 3.3 `docs/ApiRoutes.md` uses old paths (P2)

- **Where:** `docs/ApiRoutes.md:1113`, `:1128`, `:1172`, `:1182`.
- **Problem:** these entries use `/guest-sessions/...` and `/group-reservations/...`. The code uses `/guest/sessions/...` and `/guest/groups/...`.
- **Fix:** change each path to the route in `Routing/RouteRegistry.cs`, and check each body against its handler.

## Suggested order

1. Section 1 (DELETE), as one `/orchestrate` task.
2. Items 2.2, 2.3 and 2.4: small fixes with a test each.
3. Item 2.1: changes the API contract. Agree on it with the frontend first.
4. Items 2.5 and section 3: docs, after the code changes above.
