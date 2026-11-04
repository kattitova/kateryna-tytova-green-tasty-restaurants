# BE-1 tests r1
Files: Tests/Services/GroupBonusCalculatorTests.cs, Tests/Services/GroupPreOrderRulesTests.cs
Tests: 17 test cases (6+1 all-ordered, 4 not-all-open, 1 not-open, 2 JSON, 3 OwnerOwnItems).
Red run (dotnet test Tests/APIHandler.Tests.csproj): Total 354, Passed 339, Failed 15. All 15 fail with NotImplementedException (13 calculator, 3 rules... see below). 2 JSON tests pass (expected).
Existing tests: 337 still pass.
Notes: Calculator Missed deadline = start-2h; Achieved deadline null; DateTimeKind.Utc used.
