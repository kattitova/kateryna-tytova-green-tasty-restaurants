using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GroupReservation;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Services.GroupReservations;

public class GroupPreOrderService(
    IGroupReservationRepository groupReservationRepository,
    IReservationRepository reservationRepository,
    IGuestSessionRepository guestSessionRepository,
    IOrderRepository orderRepository) : IGroupPreOrderService
{
    public async Task<IReadOnlyList<GroupPreOrderDishResponse>> SubmitGroupPreOrderAsync(string userId, string groupReservationId, string reservationId)
    {
        Reservation reservation = await reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new ReservationNotFoundException("Reservation not found.");

        if (!string.Equals(reservation.CustomerId, userId, StringComparison.Ordinal))
        {
            throw new ActionForbiddenException("Only the reservation owner can perform this action.");
        }

        GroupReservation? group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId);
        if (group is null || !string.Equals(group.ReservationId, reservationId, StringComparison.Ordinal))
        {
            throw new GroupReservationNotFoundException();
        }

        EnsureGroupIsOpen(group.Status);

        List<GuestSession> sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);
        List<GuestSession> submittedSessions = sessions.Where(s => s.IsSubmitted).ToList();
        if (submittedSessions.Count == 0)
        {
            throw new ValidationException($"No submitted guest sessions for group reservation '{groupReservationId}'.");
        }

        IReadOnlyList<GroupPreOrderDishResponse> consolidated = Consolidate(submittedSessions);
        if (consolidated.Count == 0)
        {
            throw new ValidationException("Submitted guest sessions contain no dishes.");
        }

        Order order = await orderRepository.GetOrderByReservationIdAsync(reservationId)
            ?? throw new ValidationException($"Active pre-order for reservation '{reservationId}' not found.");

        order.Dishes = MergeIntoOrderItems(order.Dishes ?? [], consolidated);
        order.Status = OrderStatus.SubmittedByCustomer;

        if (!await groupReservationRepository.SubmitGroupPreOrderAsync(groupReservationId, order))
        {
            throw new GroupReservationSubmittedException();
        }

        return consolidated;
    }

    private static void EnsureGroupIsOpen(GroupReservationStatus status)
    {
        switch (status)
        {
            case GroupReservationStatus.Submitted:
                throw new GroupReservationSubmittedException();
            case GroupReservationStatus.Closed:
                throw new GroupReservationExpiredException();
        }
    }

    // Sums quantities by dish id. The result keeps the order of first appearance:
    // sessions in repository order, dishes in list order.
    private static IReadOnlyList<GroupPreOrderDishResponse> Consolidate(IEnumerable<GuestSession> sessions)
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

    // D5: an active item (Pending or Confirmed) with the same dish gets the guest quantity added.
    // D5b: a Canceled item with the same dish gets the guest quantity only.
    // Both become Pending. Dishes without an item are appended as Pending. Other items do not change.
    private static List<OrderItem> MergeIntoOrderItems(List<OrderItem> items, IEnumerable<GroupPreOrderDishResponse> consolidated)
    {
        foreach (GroupPreOrderDishResponse dish in consolidated)
        {
            OrderItem? item = items.FirstOrDefault(i => string.Equals(i.DishId, dish.DishId, StringComparison.Ordinal));
            if (item is null)
            {
                items.Add(new OrderItem { DishId = dish.DishId, Quantity = dish.Quantity, State = OrderItemState.Pending });
                continue;
            }

            item.Quantity = item.State == OrderItemState.Canceled ? dish.Quantity : item.Quantity + dish.Quantity;
            item.State = OrderItemState.Pending;
        }

        return items;
    }
}
