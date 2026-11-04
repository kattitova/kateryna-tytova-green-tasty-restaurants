# BE-3 implement r1

Mode: implement, step 3 (BE-3).

Log:
- Read tests-r1.md. Red: 6 failing.
- `SubmitGroupPreOrderAsync`: guest dishes = `GroupPreOrderRules.Consolidate(submitted sessions)`; owner-has-items from `OwnerOwnItems` (computed before the merge mutates the order); both empty -> ValidationException "Nothing to submit: the group has no pre-orders."; order missing -> new `CreateOwnerOrderAsync` (location lookup only here, null -> ValidationException "Location not found for the reservation's table." before the transaction); order present -> merge guest dishes only (owner items never added twice); one `SubmitGroupPreOrderAsync` transaction call; returns guest dishes (may be empty).
- Removed the 3 old error paths. `ILocationService` ctor param is now required; scaffold field removed.

File: Services/GroupReservations/GroupPreOrderService.cs.

Checks (--no-incremental): 0 errors, 97 warnings total, 0 in changed files. Tests: 361 passed, 0 failed.
Test defects: none. Not checked: real DynamoDB transaction with the new order (needs deploy).
