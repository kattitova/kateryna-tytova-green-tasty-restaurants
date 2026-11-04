# BE-3 tests r1
File: Tests/Services/GroupPreOrderServiceTests.cs
Ctor: Mock<ILocationService> passed as last arg.
Deleted (5, removed errors): NoSessions, OnlyUnsubmittedSessions, SubmittedSessionsWithoutDishes, DishesOnlyInUnsubmittedSessions, OrderNotFound. Replaced by:
- [Theory] NothingToSubmit (3 rows: no order+no guests; only Canceled items; submitted guest without dishes + unsubmitted guest with dishes) -> ValidationException "Nothing to submit: the group has no pre-orders.", no writes.
- OwnerOrderAndNoSubmittedGuests: one transaction, same items, SubmittedByCustomer, returns [], no location lookup.
- NoOrderAndSubmittedGuests: new order (CustomerId, ReservationId, address from location, ReservationTime = OrderMapper.FormatReservationTime, Pending consolidated items, SubmittedByCustomer, OrderId set), returns consolidated dishes.
- NoOrderAndLocationMissing: ValidationException "Location not found for the reservation's table." before transaction.
Red run: Total 361, Passed 355, Failed 6. Reasons: old messages thrown ("No submitted guest sessions...", "Active pre-order ... not found") -> message differs / validation thrown (6 of 6).
Existing tests in the class: all pass.
