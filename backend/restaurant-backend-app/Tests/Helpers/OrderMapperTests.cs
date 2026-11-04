using System.Text.Json;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Mappers;
using APIHandler.Models.GuestSessions;

namespace APIHandler.Tests.Helpers;

public class OrderMapperTests
{
    private static readonly Dish[] Dishes =
    [
        new() { Id = "dish-1", Name = "Soup", Price = 10m, LocationId = "loc-1" },
        new() { Id = "dish-2", Name = "Salad", Price = 12m, LocationId = "loc-1" },
        new() { Id = "dish-3", Name = "Steak", Price = 30m, LocationId = "loc-1" }
    ];

    [Fact]
    public void ToOrderResponse_OrderWithTenPercentBonus_CopiesTierAndPercent()
    {
        var order = ArrangeOrder(GroupBonusTier.TenPercentDiscount, 10);

        var result = OrderMapper.ToOrderResponse(order, Dishes);

        Assert.Equal(GroupBonusTier.TenPercentDiscount, result.GroupBonusTier);
        Assert.Equal(10, result.DiscountPercent);
    }

    [Fact]
    public void ToOrderResponse_OrderWithFreeDessert_CopiesTierAndZeroPercent()
    {
        var order = ArrangeOrder(GroupBonusTier.FreeDessert, 0);

        var result = OrderMapper.ToOrderResponse(order, Dishes);

        Assert.Equal(GroupBonusTier.FreeDessert, result.GroupBonusTier);
        Assert.Equal(0, result.DiscountPercent);
    }

    [Fact]
    public void ToOrderResponse_OrderWithoutBonus_ReturnsNullTierAndZero()
    {
        var order = ArrangeOrder(null, 0);

        var result = OrderMapper.ToOrderResponse(order, Dishes);

        Assert.Null(result.GroupBonusTier);
        Assert.Equal(0, result.DiscountPercent);
    }

    [Fact]
    public void ToOrderResponse_CopiesItemState()
    {
        var order = ArrangeOrder(null, 0,
            Item("dish-1", 1, OrderItemState.Pending),
            Item("dish-2", 2, OrderItemState.Confirmed),
            Item("dish-3", 3, OrderItemState.Canceled));

        var result = OrderMapper.ToOrderResponse(order, Dishes);

        Assert.Equal(
            [OrderItemState.Pending, OrderItemState.Confirmed, OrderItemState.Canceled],
            result.OrderItems.Select(i => i.State));
        Assert.Equal(["dish-1", "dish-2", "dish-3"], result.OrderItems.Select(i => i.Dish.Id));
    }

    [Fact]
    public void ToOrderResponse_Serialized_UsesStringTierAndNumberState()
    {
        var order = ArrangeOrder(GroupBonusTier.FreeDessert, 0, Item("dish-1", 1, OrderItemState.Canceled));

        var json = JsonSerializer.Serialize(OrderMapper.ToOrderResponse(order, Dishes));

        Assert.Contains("\"groupBonusTier\":\"FreeDessert\"", json);
        Assert.Contains("\"discountPercent\":0", json);
        Assert.Contains("\"state\":2", json);
    }

    [Fact]
    public void ToOrderResponse_SerializedTenPercent_WritesPercentTen()
    {
        var order = ArrangeOrder(GroupBonusTier.TenPercentDiscount, 10);

        var json = JsonSerializer.Serialize(OrderMapper.ToOrderResponse(order, Dishes));

        Assert.Contains("\"groupBonusTier\":\"TenPercentDiscount\"", json);
        Assert.Contains("\"discountPercent\":10", json);
    }

    [Fact]
    public void ToOrderResponse_SerializedWithoutBonus_WritesNullTier()
    {
        var order = ArrangeOrder(null, 0);

        var json = JsonSerializer.Serialize(OrderMapper.ToOrderResponse(order, Dishes));

        Assert.Contains("\"groupBonusTier\":null", json);
    }

    private static OrderItem Item(string dishId, int quantity, OrderItemState state) =>
        new() { DishId = dishId, Quantity = quantity, State = state };

    private static Order ArrangeOrder(GroupBonusTier? tier, int percent, params OrderItem[] items) => new()
    {
        OrderId = "order-1",
        CustomerId = "customer-1",
        ReservationId = "reservation-1",
        ReservationAddress = "Address",
        ReservationTime = "Time",
        Dishes = items.Length == 0 ? [Item("dish-1", 1, OrderItemState.Pending)] : items.ToList(),
        GroupBonusTier = tier,
        DiscountPercent = percent
    };
}
