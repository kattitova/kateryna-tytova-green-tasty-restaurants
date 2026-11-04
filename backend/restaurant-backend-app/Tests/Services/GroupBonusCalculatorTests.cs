using System.Text.Json;
using APIHandler.Models.GuestSessions;
using APIHandler.Services.GroupReservations;

namespace APIHandler.Tests.Services;

public class GroupBonusCalculatorTests
{
    private const int Seats = 4;
    private static readonly DateTime Start = new(2030, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    private static DateTime Before(int minutes) => Start.AddMinutes(-minutes);

    [Theory]
    [InlineData(1440, true, GroupBonusState.Achieved, GroupBonusTier.TenPercentDiscount, 10, null)]
    [InlineData(1440, false, GroupBonusState.Achieved, GroupBonusTier.TenPercentDiscount, 10, null)]
    [InlineData(1439, true, GroupBonusState.Achieved, GroupBonusTier.FreeDessert, 0, null)]
    [InlineData(120, true, GroupBonusState.Achieved, GroupBonusTier.FreeDessert, 0, null)]
    [InlineData(119, true, GroupBonusState.Missed, null, 0, 120)]
    [InlineData(119, false, GroupBonusState.Missed, null, 0, 120)]
    public void Calculate_AllSeatsOrdered_ReturnsBonusByLastOrderTime(
        int lastMinutesBeforeStart, bool groupIsOpen,
        GroupBonusState state, GroupBonusTier? tier, int percent, int? deadlineMinutesBeforeStart)
    {
        var result = GroupBonusCalculator.Calculate(
            Seats, Seats, Before(lastMinutesBeforeStart), Start, Before(60), groupIsOpen);

        AssertBonus(result, state, tier, percent, deadlineMinutesBeforeStart);
    }

    [Fact]
    public void Calculate_AllSeatsOrderedWithoutLastTime_ReturnsAchievedTenPercent()
    {
        var result = GroupBonusCalculator.Calculate(Seats, Seats, null, Start, Before(60), true);

        AssertBonus(result, GroupBonusState.Achieved, GroupBonusTier.TenPercentDiscount, 10, null);
    }

    [Theory]
    [InlineData(1440, GroupBonusState.InProgress, GroupBonusTier.TenPercentDiscount, 10, 1440)]
    [InlineData(1439, GroupBonusState.InProgress, GroupBonusTier.FreeDessert, 0, 120)]
    [InlineData(120, GroupBonusState.InProgress, GroupBonusTier.FreeDessert, 0, 120)]
    [InlineData(119, GroupBonusState.Missed, null, 0, 120)]
    public void Calculate_NotAllOrderedAndOpen_ReturnsBonusByNow(
        int nowMinutesBeforeStart,
        GroupBonusState state, GroupBonusTier? tier, int percent, int? deadlineMinutesBeforeStart)
    {
        var result = GroupBonusCalculator.Calculate(
            Seats, 2, Before(3000), Start, Before(nowMinutesBeforeStart), true);

        AssertBonus(result, state, tier, percent, deadlineMinutesBeforeStart);
    }

    [Fact]
    public void Calculate_NotAllOrderedAndGroupNotOpen_ReturnsMissed()
    {
        var result = GroupBonusCalculator.Calculate(
            Seats, 2, Before(3000), Start, Start.AddDays(-3), false);

        AssertBonus(result, GroupBonusState.Missed, null, 0, 120);
    }

    [Fact]
    public void Serialize_InProgressBonus_WritesStringEnumsAndUtcDeadline()
    {
        var bonus = new GroupBonusResponse
        {
            State = GroupBonusState.InProgress,
            Tier = GroupBonusTier.TenPercentDiscount,
            DiscountPercent = 10,
            Deadline = Before(1440)
        };

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(bonus));
        var root = doc.RootElement;

        Assert.Equal("InProgress", root.GetProperty("state").GetString());
        Assert.Equal("TenPercentDiscount", root.GetProperty("tier").GetString());
        Assert.Equal(10, root.GetProperty("discountPercent").GetInt32());
        Assert.EndsWith("Z", root.GetProperty("deadline").GetString());
    }

    [Fact]
    public void Serialize_MissedBonus_WritesNullTier()
    {
        var bonus = new GroupBonusResponse
        {
            State = GroupBonusState.Missed,
            Tier = null,
            DiscountPercent = 0,
            Deadline = Before(120)
        };

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(bonus));

        Assert.Equal("Missed", doc.RootElement.GetProperty("state").GetString());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("tier").ValueKind);
    }

    private static void AssertBonus(
        GroupBonusResponse result, GroupBonusState state, GroupBonusTier? tier, int percent, int? deadlineMinutesBeforeStart)
    {
        Assert.Equal(state, result.State);
        Assert.Equal(tier, result.Tier);
        Assert.Equal(percent, result.DiscountPercent);
        Assert.Equal(deadlineMinutesBeforeStart is null ? null : Before(deadlineMinutesBeforeStart.Value), result.Deadline);
    }
}
