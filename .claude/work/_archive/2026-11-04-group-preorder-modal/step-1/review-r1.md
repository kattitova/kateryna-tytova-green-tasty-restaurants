# BE-1 review r1

Verdict: approve

- Calculator matches plan "Data shape" 6: inclusive boundaries (<=), all-ordered for any status, null last time -> TenPercent, Missed has null tier / 0 / deadline start-2h, Achieved has null deadline, 10% also while InProgress.
- OwnerOwnItems matches rule 2: null order -> empty, Canceled dropped, subtraction only for Submitted, qty <= 0 dropped, order kept, new OrderItem objects (order not mutated).
- Consolidate moved unchanged; service calls it. Maker/checker split ok (impl changed no tests; tests only in new test files).
- Tests: 20-ish cases cover all plan boundaries (24h, 24h-1min, 2h, 2h-1min, group not open, null last, JSON Z/null tier) and the 3 rules cases. Lean, behaviour-level.
- Findings: none blocker/major. Minor (optional): no test for an OwnerOwnItems case where guest qty is less than owner qty (partial subtraction); the Submitted test may cover it.
- Checks: `dotnet build --no-incremental` 0 errors, 97 warnings, 0 in changed files. `dotnet test` 354 passed, 0 failed.
