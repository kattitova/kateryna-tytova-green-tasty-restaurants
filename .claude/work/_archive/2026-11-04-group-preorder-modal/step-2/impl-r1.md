# BE-2 implement r1

Mode: implement, step 2 (BE-2).

Log:
- Read tests-r1.md. Red: 7 failing.
- OrderService: `AddOrderAsync` and `AddOrderForWaiterAsync` set `CreatedAt = DateTime.UtcNow` before save.
- GuestSessionService: `SubmitCartAsync` sets `SubmittedAt`; ctor param `IOrderRepository` now required (private field removed); status computes OrderedCount and Bonus via `GroupPreOrderRules.OwnerOwnItems` and `GroupBonusCalculator.Calculate`; `BuildGuestSnapshotAsync` replaced by `BuildSnapshotAsync` (guests + Owner participant, owner dishes in the same `GetDishesByIdsAsync` call); `GetCurrentParticipantCountAsync` removed (inlined, same value); shared `ToItemResponses` helper.
- Removed temporary JsonIgnore on `GetAllSessionsResponse.Owner`.
- Calculator: deadlines now clamp at DateTime.MinValue. Reason: 3 existing status tests use a default (MinValue) reservation start, and `start - 24h` threw ArgumentOutOfRangeException. Real data is unaffected.

Files: Services/Orders/OrderService.cs, Services/GuestSessions/GuestSessionService.cs, Services/GroupReservations/GroupBonusCalculator.cs, Models/GuestSessions/GetAllResponse.cs.

Checks (--no-incremental): 0 errors, 97 warnings total, 0 in changed production files (3 listed warnings are in the existing Tests/OrderServiceTests.cs, not mine). Tests: 360 passed, 0 failed.
Test defects: none. Not checked: DynamoDB round trip of createdAt/submittedAt (needs deploy).
Follow-up: status handler now reads Orders on every call (public route; accepted in plan Q4).
