using System;
using System.Text.Json.Serialization;

namespace APIHandler.Models.GuestSessions;

public enum GroupBonusState
{
    InProgress,
    Achieved,
    Missed
}

public enum GroupBonusTier
{
    TenPercentDiscount,
    FreeDessert
}

public class GroupBonusResponse
{
    [JsonPropertyName("state")]
    [JsonConverter(typeof(JsonStringEnumConverter<GroupBonusState>))]
    public GroupBonusState State { get; init; }

    [JsonPropertyName("tier")]
    [JsonConverter(typeof(JsonStringEnumConverter<GroupBonusTier>))]
    public GroupBonusTier? Tier { get; init; }

    [JsonPropertyName("discountPercent")]
    public int DiscountPercent { get; init; }

    [JsonPropertyName("deadline")]
    public DateTime? Deadline { get; init; }
}
