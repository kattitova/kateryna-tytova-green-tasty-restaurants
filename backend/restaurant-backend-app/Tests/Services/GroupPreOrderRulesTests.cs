using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Services.GroupReservations;

namespace APIHandler.Tests.Services;

public class GroupPreOrderRulesTests
{
    [Fact]
    public void OwnerOwnItems_OpenGroup_ReturnsOrderItemsWithoutCanceled()
    {
        var order = ArrangeOrder(
            Item("dish-1", 2),
            Item("dish-2", 1, OrderItemState.Canceled),
            Item("dish-3", 3, OrderItemState.Confirmed));

        var result = GroupPreOrderRules.OwnerOwnItems(order, [], GroupReservationStatus.Open);

        Assert.Equal(["dish-1", "dish-3"], result.Select(i => i.DishId));
        Assert.Equal([2, 3], result.Select(i => i.Quantity));
    }

    [Fact]
    public void OwnerOwnItems_SubmittedGroup_SubtractsSubmittedGuestQuantitiesAndDropsGuestOnlyDishes()
    {
        var order = ArrangeOrder(Item("dish-1", 5), Item("dish-2", 2), Item("dish-3", 3));
        var sessions = new[]
        {
            Session(true, Item("dish-1", 1), Item("dish-3", 3)),
            Session(true, Item("dish-1", 1)),
            Session(false, Item("dish-2", 9))
        };

        var result = GroupPreOrderRules.OwnerOwnItems(order, sessions, GroupReservationStatus.Submitted);

        Assert.Equal(["dish-1", "dish-2"], result.Select(i => i.DishId));
        Assert.Equal([3, 2], result.Select(i => i.Quantity));
    }

    [Fact]
    public void OwnerOwnItems_NullOrder_ReturnsEmpty()
    {
        var result = GroupPreOrderRules.OwnerOwnItems(null, [Session(true, Item("dish-1", 1))], GroupReservationStatus.Submitted);

        Assert.Empty(result);
    }

    private static readonly DateTime T = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Progress_NoOrderAndNoSessions_ReturnsZeroAndNull()
    {
        var result = GroupPreOrderRules.Progress(null, [], GroupReservationStatus.Open);

        Assert.Equal(new GroupOrderProgress(0, null), result);
    }

    [Fact]
    public void Progress_OwnerHasActiveItemsAndTwoSubmittedGuests_CountsThreeAndTakesLatestTime()
    {
        var order = ArrangeOrder(Item("dish-1", 1));
        order.CreatedAt = T.AddHours(-3);
        var sessions = new[]
        {
            Guest(true, T.AddHours(-1)),
            Guest(true, T.AddHours(-5))
        };

        var result = GroupPreOrderRules.Progress(order, sessions, GroupReservationStatus.Open);

        Assert.Equal(new GroupOrderProgress(3, T.AddHours(-1)), result);
    }

    [Fact]
    public void Progress_OwnerOrderIsLatest_ReturnsOrderCreatedAt()
    {
        var order = ArrangeOrder(Item("dish-1", 1));
        order.CreatedAt = T.AddHours(-1);

        var result = GroupPreOrderRules.Progress(order, [Guest(true, T.AddHours(-5))], GroupReservationStatus.Open);

        Assert.Equal(new GroupOrderProgress(2, T.AddHours(-1)), result);
    }

    [Fact]
    public void Progress_OwnerOnlyHasCanceledItems_DoesNotCountOwnerOrItsCreatedAt()
    {
        var order = ArrangeOrder(Item("dish-1", 1, OrderItemState.Canceled));
        order.CreatedAt = T.AddHours(-1);

        var result = GroupPreOrderRules.Progress(order, [Guest(true, T.AddHours(-5))], GroupReservationStatus.Open);

        Assert.Equal(new GroupOrderProgress(1, T.AddHours(-5)), result);
    }

    [Fact]
    public void Progress_UnsubmittedGuest_IsNotCounted()
    {
        var sessions = new[]
        {
            Guest(true, T.AddHours(-5)),
            Guest(false, T.AddHours(-1))
        };

        var result = GroupPreOrderRules.Progress(null, sessions, GroupReservationStatus.Open);

        Assert.Equal(new GroupOrderProgress(1, T.AddHours(-5)), result);
    }

    [Fact]
    public void Progress_SubmittedOwnerSession_IsNotCountedAsGuest()
    {
        var ownerSession = Guest(true, T.AddHours(-1));
        ownerSession.IsOwnerSession = true;

        var result = GroupPreOrderRules.Progress(null, [ownerSession], GroupReservationStatus.Open);

        Assert.Equal(new GroupOrderProgress(0, null), result);
    }

    [Fact]
    public void Progress_SubmittedGroup_SubtractsGuestDishesFromOwnerItems()
    {
        var order = ArrangeOrder(Item("dish-1", 2));
        order.CreatedAt = T.AddHours(-1);
        var guest = Guest(true, T.AddHours(-5));
        guest.Dishes = [Item("dish-1", 2)];

        var result = GroupPreOrderRules.Progress(order, [guest], GroupReservationStatus.Submitted);

        Assert.Equal(new GroupOrderProgress(1, T.AddHours(-5)), result);
    }

    [Fact]
    public void Progress_TimesAreNull_ReturnsNullLastOrderedAt()
    {
        var order = ArrangeOrder(Item("dish-1", 1));
        order.CreatedAt = null;

        var result = GroupPreOrderRules.Progress(order, [Guest(true, null)], GroupReservationStatus.Open);

        Assert.Equal(new GroupOrderProgress(2, null), result);
    }

    private static GuestSession Guest(bool submitted, DateTime? submittedAt) =>
        new() { Id = Guid.NewGuid().ToString(), GuestName = "Guest", Dishes = [], IsSubmitted = submitted, SubmittedAt = submittedAt };

    private static OrderItem Item(string dishId, int quantity, OrderItemState state = OrderItemState.Pending) =>
        new() { DishId = dishId, Quantity = quantity, State = state };

    private static GuestSession Session(bool submitted, params OrderItem[] dishes) =>
        new() { Id = Guid.NewGuid().ToString(), GuestName = "Guest", Dishes = dishes.ToList(), IsSubmitted = submitted };

    private static Order ArrangeOrder(params OrderItem[] items) =>
        new()
        {
            OrderId = "order-1",
            CustomerId = "owner-user-1",
            ReservationId = "reservation-1",
            ReservationAddress = "Address",
            ReservationTime = "Time",
            Dishes = items.ToList()
        };
}
