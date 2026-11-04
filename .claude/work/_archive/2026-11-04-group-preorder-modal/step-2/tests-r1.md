# BE-2 tests r1
Files: Tests/Services/OrderServiceTests.cs, Tests/Services/GuestSessionServiceTests.cs, Tests/Handlers/DeleteGuestSessionHandlerTests.cs
New tests (7): AddOrderAsync CreatedAt; SubmitCartAsync SubmittedAt (existing test extended); status x3 (in-progress count 3; all seats Achieved/TenPercent; Submitted group owner not counted); sessions owner x2 (with order; no order).
Ctor: _mockOrderRepository.Object passed as 7th arg. Arrange helper got optional startTime/groupStatus.
Handler test NoGuestsRemain now sets Owner and expects exact JSON with "owner" (passes now; will still pass after JsonIgnore is removed).
Red run: Total 360, Passed 353, Failed 7. Reasons: CreatedAt/SubmittedAt null (NotNull failure); Owner null (2); OrderedCount/Bonus values differ (3, scaffold placeholders).
Existing tests: all pass.
