# ADR 0001: Owner view of group pre-order sessions

- **Status:** Accepted
- **Date:** 2026-11-02
- **Commit:** `17bc2e5`
- **Area:** backend (`api-handler` Lambda), API Gateway config (Syndicate)

## Context

A group reservation lets guests join a shared pre-order. Each guest has a `GuestSession` with a list of dishes. The owner of the reservation must see who joined, what each guest added, a total for each guest, and a grand total.

Facts that shaped the decisions:

- The owner is the Cognito user whose `sub` equals `Reservation.CustomerId` of the parent reservation. Guests use a separate guest token.
- No code creates an owner `GuestSession` today. The owner's own dishes are in the reservation's `Order`. `CreateGroupOwnerSessionAsync` exists but has no caller.
- A session stores only `DishId` and `Quantity`. Dish name and price come from the Dishes table, and a dish can be deleted after a guest added it.
- `DeleteGuestSessionAsync` already built the same response shape (`GetAllSessionsResponse`), but it has no tests. It throws `KeyNotFoundException` (HTTP 500) for a deleted dish, and it returns PascalCase JSON.
- `deployment_resources.json` declared the API Gateway path as `/guest/group/...` (singular). All other group routes use `/guest/groups/...`.

## Decision

Add `GET /guest/groups/{groupReservationId}/sessions` (`GetGroupSessionsHandler` → `GuestSessionService.GetGroupSessionsAsync`).

1. **Only the owner can read the list.** API Gateway requires a Cognito token. The service then calls `ValidateReservationOwnerAsync` before it reads any session. Unknown group or reservation → 404. Caller is not the owner → 403. The response contains guests' names and orders, so the check runs first.
2. **Guests only.** The list has only sessions with `IsOwnerSession == false`, each with `type: "Guest"`. The grand total is the sum over guests. The owner's `Order` is not part of this response.
3. **A deleted dish does not fail the read.** An item whose dish no longer exists is left out of `items` and of all totals. The endpoint does not return 500 for it.
4. **The read works in every group status** (`Open`, `Submitted`, `Closed`). The owner can review the orders after submit.
5. **camelCase JSON, shared DTOs.** The handler uses `ResponseBuilder.CreateJsonResponse`. `dish` is the full `DishResponse`, the same DTO as in `GET /guest/sessions/{id}`.
6. **Order is stable.** Participants come in repository order. Items come in the order of `GuestSession.Dishes`.
7. **Dishes load in one batch.** The service collects the distinct dish IDs of all sessions and calls `GetDishesByIdsAsync` once.
8. **API Gateway path fixed** from `/guest/group/{groupReservationId}/sessions` to `/guest/groups/{groupReservationId}/sessions`. Deploy with `syndicate update`, so Syndicate deletes the old resource.

## Alternatives considered

| Alternative | Why we did not choose it |
|---|---|
| Include the owner as a participant with `type: "Owner"`, items from the `Order` | The user chose guests only. It needs a new dependency in `GuestSessionService` and changes the grand total. `ParticipantType.Owner` stays in the enum for this option. |
| Throw for a deleted dish, as `DeleteGuestSessionAsync` does | A read view must not fail because of a menu change. |
| Extract one helper for the item and total logic in `DeleteGuestSessionAsync`, `GetGuestSessionInfoAsync` and the new method | `DeleteGuestSessionAsync` has no tests, and a shared helper changes its deleted-dish behaviour. We deferred it (see `docs/tech-debt.md`, item 1.1). |
| A small DTO with only `id`, `name`, `price` for `dish` | The full `DishResponse` contains these fields. A second DTO adds code with no benefit to the client. |
| `CreateSuccessResponse` (PascalCase), as the DELETE endpoint does | The task contract is camelCase. The DELETE casing is listed as debt. |

## Consequences

- The owner view has 31 unit tests: 15 for the service and 16 for the handler, the first handler tests in the repo. They check the owner check order, the guest-only filter, the deleted-dish rule, the item order and the camelCase body.
- `DeleteGuestSessionAsync` and `GetGuestSessionInfoAsync` still have duplicated item logic and still return 500 for a deleted dish. Two endpoints now treat a deleted dish in different ways until that debt is fixed.
- The frontend has no model or call for this endpoint yet. It needs `getGroupSessions(groupReservationId)` in `guest-sessions.service.ts` and `GroupSessionsResponse` / `Participant` interfaces. The user moved this to a separate task.
- Only a deploy can check API Gateway routing, the Cognito `sub` claim and the `groupReservationId` index query.

## References

- Contract: `docs/ApiRoutes.md`, entry `GET /guest/groups/{groupReservationId}/sessions`
- Code: `Handlers/GuestSessions/GetGroupSessionsHandler.cs`, `Services/GuestSessions/GuestSessionService.cs` (`GetGroupSessionsAsync`, `ToGuestParticipant`), `Routing/RouteRegistry.cs`
- Tests: `Tests/Services/GuestSessionServiceTests.cs`, `Tests/Handlers/GetGroupSessionsHandlerTests.cs`
- Debt: `docs/tech-debt.md`, section 1
