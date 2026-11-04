# BE-2 review r1

Verdict: approve with minor.

Checks: `dotnet build --no-incremental` 0 errors, 97 warnings (0 in changed production files; the xUnit1006 warnings are in untouched OrderServiceTests lines). `dotnet test`: 360 passed, 0 failed.
Maker/checker: tests changed only under Tests/, production only under dnapp/. OK.

Verified against plan:
- CreatedAt set only in AddOrderAsync and AddOrderForWaiterAsync; SubmittedAt only in SubmitCartAsync. OK.
- Status: OrderedCount = owner (OwnerOwnItems from order, sessions, group.Status) + submitted guests; last time = Max of guest SubmittedAt and owner CreatedAt (only if owner ordered; nulls skipped; all null gives null). Bonus via GroupBonusCalculator with StartTime.ToUniversalTime(), UtcNow, Open flag. OK.
- Owner participant: OwnerOwnItems, same GetDishesByIdsAsync call, missing dishes skipped, IsSubmitted = own items not empty, id "owner-{groupId}". Set in both GetGroupSessions and Delete. OK.
- Ctor param IOrderRepository required, no default. OK.
- JSON: `owner`, `orderedCount`, `bonus` names correct. M1: Bonus is `required` (no CS8618). OK.

### [minor] [code] GroupBonusCalculator.cs:46-48 — MinValue clamp is test-driven
- Problem: the `Before` clamp exists only because 3 existing status tests use a default reservation start (DateTime.MinValue). Real reservations never hit it. It adds a branch to a pure function and is not covered by a test of its own.
- Fix (preferred, optional): give those 3 tests a real StartTime and remove the clamp. If you keep it, accept it as harmless: the logic is correct (start - MinValue < lead gives MinValue). Do not block the step on it.

### [minor] [tests] no test pins the clamp or the null-last-time-owner path
- Only relevant if the clamp stays. Add one calculator test with start = MinValue (no throw).

Note: status now reads Orders on every call of a public route (accepted in plan Q4). DynamoDB round trip of createdAt/submittedAt needs a deploy; not checked.
