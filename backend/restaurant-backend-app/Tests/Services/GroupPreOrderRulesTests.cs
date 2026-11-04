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
