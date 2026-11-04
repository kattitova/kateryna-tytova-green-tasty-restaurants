# BE-1 implement r1

Mode: implement, step 1 (BE-1).

Log:
- Read tests-r1.md and GroupPreOrderRulesTests. Red state: 15 failing (NotImplementedException).
- Implemented `GroupBonusCalculator.Calculate` per Data shape 6 (inclusive boundaries, Utc kind on deadlines, Missed: tier null, 0 %, deadline start-2h) with small private factory helpers.
- Implemented `GroupPreOrderRules.OwnerOwnItems` per Data shape 2 (null order -> empty, drop Canceled, subtract consolidated submitted-guest quantities only for Submitted groups, drop qty <= 0, return new OrderItem objects).
- First build had a stray brace (my edit); fixed.

Files: Services/GroupReservations/GroupBonusCalculator.cs, GroupPreOrderRules.cs.

Checks (--no-incremental): 0 errors, 97 warnings total, 0 in changed files. Tests: 354 passed, 0 failed, total 354.
Test defects: none. Not checked: none for this step (pure functions).
