# BE-3 review r1
Verdict: approve

Checks (backend, --no-incremental): build 0 errors, 97 warnings (same as baseline; 0 in changed files per implementer). Tests: 361 passed, 0 failed.
Maker/checker: tests changed only in GroupPreOrderServiceTests.cs; production files only in dnapp. OK.

- No duplication: existing order gets guest dishes only; owner-has-items computed before merge. OK.
- New owner order: OrderId, CustomerId=userId, ReservationId, address, ReservationTime (shared formatter), Pending items, SubmittedByCustomer, CreatedAt UtcNow. OK.
- Nothing-to-submit and null-location ValidationException (400) thrown before the transaction call. OK; location lookup only when order is null.
- Removed errors: NoSessions, OnlyUnsubmitted, WithoutDishes, OrderNotFound are now valid or merged into "Nothing to submit" (theory covers 3 rows incl. canceled-only order); OrderNotFound replaced by creation test. Other errors (group open, ownership, GroupReservationSubmittedException) untouched.
- CreateOrderHandler: only string moved to OrderMapper.FormatReservationTime, identical format.

Minor (no action required): CreatedAt is not set by CreateOrderHandler's mapper; confirm repository/DB layer treats it consistently (not verified, needs deploy). Real DynamoDB transaction with the new order untested.
