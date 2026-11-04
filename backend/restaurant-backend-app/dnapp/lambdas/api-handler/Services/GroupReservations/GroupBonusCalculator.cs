using APIHandler.Models.GuestSessions;
using System;

namespace APIHandler.Services.GroupReservations;

public static class GroupBonusCalculator
{
    public const int DiscountPercent = 10;
    public static readonly TimeSpan DiscountLeadTime = TimeSpan.FromHours(24);
    public static readonly TimeSpan DessertLeadTime = TimeSpan.FromHours(2);

    public static GroupBonusResponse Calculate(
        int seatCount,
        int orderedCount,
        DateTime? lastOrderedAtUtc,
        DateTime reservationStartUtc,
        DateTime nowUtc,
        bool groupIsOpen)
    {
        DateTime discountDeadline = Before(reservationStartUtc, DiscountLeadTime);
        DateTime dessertDeadline = Before(reservationStartUtc, DessertLeadTime);

        if (orderedCount >= seatCount)
        {
            if (lastOrderedAtUtc is null || lastOrderedAtUtc <= discountDeadline)
            {
                return Achieved(GroupBonusTier.TenPercentDiscount);
            }

            return lastOrderedAtUtc <= dessertDeadline ? Achieved(GroupBonusTier.FreeDessert) : Missed(dessertDeadline);
        }

        if (!groupIsOpen)
        {
            return Missed(dessertDeadline);
        }

        if (nowUtc <= discountDeadline)
        {
            return InProgress(GroupBonusTier.TenPercentDiscount, discountDeadline);
        }

        return nowUtc <= dessertDeadline ? InProgress(GroupBonusTier.FreeDessert, dessertDeadline) : Missed(dessertDeadline);
    }

    // Clamps at DateTime.MinValue so a start time near the minimum does not throw.
    private static DateTime Before(DateTime start, TimeSpan lead) =>
        DateTime.SpecifyKind(start - DateTime.MinValue < lead ? DateTime.MinValue : start - lead, DateTimeKind.Utc);

    private static int PercentFor(GroupBonusTier tier) => tier == GroupBonusTier.TenPercentDiscount ? DiscountPercent : 0;

    private static GroupBonusResponse Achieved(GroupBonusTier tier) =>
        new() { State = GroupBonusState.Achieved, Tier = tier, DiscountPercent = PercentFor(tier), Deadline = null };

    private static GroupBonusResponse InProgress(GroupBonusTier tier, DateTime deadline) =>
        new() { State = GroupBonusState.InProgress, Tier = tier, DiscountPercent = PercentFor(tier), Deadline = deadline };

    private static GroupBonusResponse Missed(DateTime deadline) =>
        new() { State = GroupBonusState.Missed, Tier = null, DiscountPercent = 0, Deadline = deadline };
}
