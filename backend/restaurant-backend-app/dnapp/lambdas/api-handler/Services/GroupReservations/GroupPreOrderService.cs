using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Mappers;
using APIHandler.Models.GroupReservation;
using APIHandler.Models.GuestSessions;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Services.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Services.GroupReservations;

public class GroupPreOrderService(
    IGroupReservationRepository groupReservationRepository,
    IReservationRepository reservationRepository,
    IGuestSessionRepository guestSessionRepository,
    IOrderRepository orderRepository,
    ILocationService locationService) : IGroupPreOrderService
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
        IReadOnlyList<GroupPreOrderDishResponse> guestDishes =
            GroupPreOrderRules.Consolidate(sessions.Where(s => s.IsSubmitted));

        Order? order = await orderRepository.GetOrderByReservationIdAsync(reservationId);
        bool ownerHasItems = GroupPreOrderRules.OwnerOwnItems(order, sessions, group.Status).Count > 0;

        if (guestDishes.Count == 0 && !ownerHasItems)
        {
            throw new ValidationException("Nothing to submit: the group has no pre-orders.");
        }

        // The bonus is computed from the order before the merge, because the merge adds the guest dishes.
        GroupOrderProgress progress = GroupPreOrderRules.Progress(order, sessions, group.Status);
        GroupBonusResponse bonus = GroupBonusCalculator.Calculate(
            reservation.GuestCount,
            progress.OrderedCount,
            progress.LastOrderedAtUtc,
            reservation.StartTime.ToUniversalTime(),
            DateTime.UtcNow,
            groupIsOpen: true);

        if (order is null)
        {
            order = await CreateOwnerOrderAsync(userId, reservation, guestDishes);
        }
        else
        {
            // The owner's items are already in the order; only the guest dishes are merged.
            order.Dishes = MergeIntoOrderItems(order.Dishes ?? [], guestDishes);
            order.Status = OrderStatus.SubmittedByCustomer;
        }

        // A bonus that is not achieved replaces any value that an existing order has.
        bool achieved = bonus.State == GroupBonusState.Achieved;
        order.GroupBonusTier = achieved ? bonus.Tier : null;
        order.DiscountPercent = achieved ? bonus.DiscountPercent : 0;

        if (!await groupReservationRepository.SubmitGroupPreOrderAsync(groupReservationId, order))
        {
            throw new GroupReservationSubmittedException();
        }

        return guestDishes;
    }

    private async Task<Order> CreateOwnerOrderAsync(
        string userId,
        Reservation reservation,
        IReadOnlyList<GroupPreOrderDishResponse> guestDishes)
    {
        Location location = await locationService.GetLocationByTableIdAsync(reservation.TableId)
            ?? throw new ValidationException("Location not found for the reservation's table.");

        return new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            CustomerId = userId,
            ReservationId = reservation.Id,
            ReservationAddress = location.Address,
            ReservationTime = OrderMapper.FormatReservationTime(reservation.StartTime, reservation.EndTime),
            Dishes = guestDishes
                .Select(d => new OrderItem { DishId = d.DishId, Quantity = d.Quantity, State = OrderItemState.Pending })
                .ToList(),
            Status = OrderStatus.SubmittedByCustomer,
            CreatedAt = DateTime.UtcNow
        };
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
