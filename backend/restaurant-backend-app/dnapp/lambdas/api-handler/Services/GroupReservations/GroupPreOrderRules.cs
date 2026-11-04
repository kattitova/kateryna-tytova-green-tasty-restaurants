using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Helpers;
using APIHandler.Models.GroupReservation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APIHandler.Services.GroupReservations;

public static class GroupPreOrderRules
{
    // Sums quantities by dish id. The result keeps the order of first appearance:
    // sessions in repository order, dishes in list order.
    public static IReadOnlyList<GroupPreOrderDishResponse> Consolidate(IEnumerable<GuestSession> sessions)
    {
        var quantities = new Dictionary<string, int>(StringComparer.Ordinal);
        var dishOrder = new List<string>();

        foreach (OrderItem dish in sessions.SelectMany(s => s.Dishes ?? []))
        {
            if (quantities.TryGetValue(dish.DishId, out int quantity))
            {
                quantities[dish.DishId] = quantity + dish.Quantity;
            }
            else
            {
                quantities[dish.DishId] = dish.Quantity;
                dishOrder.Add(dish.DishId);
            }
        }

        return dishOrder.Select(id => new GroupPreOrderDishResponse(id, quantities[id])).ToList();
    }

    public static IReadOnlyList<OrderItem> OwnerOwnItems(
        Order? order,
        IEnumerable<GuestSession> sessions,
        GroupReservationStatus groupStatus)
    {
        if (order?.Dishes is null)
        {
            return [];
        }

        var guestQuantities = groupStatus == GroupReservationStatus.Submitted
            ? Consolidate(sessions.Where(s => s.IsSubmitted)).ToDictionary(d => d.DishId, d => d.Quantity, StringComparer.Ordinal)
            : new Dictionary<string, int>(StringComparer.Ordinal);

        var result = new List<OrderItem>();
        foreach (OrderItem item in order.Dishes.Where(i => i.State != OrderItemState.Canceled))
        {
            int quantity = item.Quantity - guestQuantities.GetValueOrDefault(item.DishId);
            if (quantity > 0)
            {
                result.Add(new OrderItem { DishId = item.DishId, Quantity = quantity, State = item.State });
            }
        }

        return result;
    }

    public static GroupOrderProgress Progress(
        Order? order,
        IEnumerable<GuestSession> sessions,
        GroupReservationStatus groupStatus)
    {
        var sessionList = sessions.ToList();
        bool ownerOrdered = OwnerOwnItems(order, sessionList, groupStatus).Count > 0;
        var submittedGuests = sessionList.GuestsOnly().Where(s => s.IsSubmitted).ToList();

        DateTime? lastOrderedAt = submittedGuests
            .Select(g => g.SubmittedAt)
            .Append(ownerOrdered ? order?.CreatedAt : null)
            .Max();

        return new GroupOrderProgress((ownerOrdered ? 1 : 0) + submittedGuests.Count, lastOrderedAt);
    }
}
