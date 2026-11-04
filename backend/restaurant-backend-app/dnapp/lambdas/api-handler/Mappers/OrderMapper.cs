using APIHandler.Entities;
using APIHandler.Models.Orders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APIHandler.Mappers;

public static class OrderMapper
{
    public static GetOrderResponse ToOrderResponse(Order order, IEnumerable<Dish> dishes)
    {
        return new GetOrderResponse
        {
            OrderId = order.OrderId,
            ReservationId = order.ReservationId,
            ReservationAddress = order.ReservationAddress,
            ReservationTime = order.ReservationTime,
            Status = order.Status,
            GroupBonusTier = order.GroupBonusTier,
            DiscountPercent = order.DiscountPercent,
            OrderItems = order.Dishes
                .Where(d => dishes.Any(dish => dish.Id == d.DishId))
                .Select(d => new OrderItemResponse
                {
                    Dish = DishMapper.ToDishResponse(dishes.First(dish => dish.Id == d.DishId)),
                    Quantity = d.Quantity,
                    State = d.State
                }).ToList(),
        };
    }

    public static Order ToOrderEntity(CreateOrderRequest request, string customerId, string reservationAddress, string reservationTime)
    {
        return new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            ReservationId = request.ReservationId,
            ReservationAddress = reservationAddress,
            ReservationTime = reservationTime,
            Dishes = request.OrderItems ?? [],
            Status = OrderStatus.Created
        };
    }

    public static string FormatReservationTime(DateTime start, DateTime end) =>
        $"{start:MMM dd, yyyy, h:mm tt} - {end:h:mm tt}";
}