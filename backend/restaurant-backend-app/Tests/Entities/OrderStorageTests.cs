using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using APIHandler.Entities;
using APIHandler.Models.GuestSessions;

namespace APIHandler.Tests.Entities;

// Round-trips Order through the real SDK object mapper. No network call is made.
public class OrderStorageTests
{
    private static readonly IDynamoDBContext Context = new DynamoDBContextBuilder()
        .WithDynamoDBClient(() => new AmazonDynamoDBClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonDynamoDBConfig { ServiceURL = "http://localhost:1" }))
        .ConfigureContext(c => c.DisableFetchingTableMetadata = true)
        .Build();

    [Fact]
    public void ToDocument_NullTier_DoesNotStoreATierValue()
    {
        var doc = Context.ToDocument(ArrangeOrder(null, 0));

        Assert.False(doc.ToAttributeMap().ContainsKey("groupBonusTier"));
    }

    [Fact]
    public void ToDocument_TenPercentDiscount_StoresName()
    {
        var map = Context.ToDocument(ArrangeOrder(GroupBonusTier.TenPercentDiscount, 10)).ToAttributeMap();

        Assert.Equal("TenPercentDiscount", map["groupBonusTier"].S);
        Assert.Equal("10", map["discountPercent"].N);
    }

    [Theory]
    [InlineData(GroupBonusTier.TenPercentDiscount, 10)]
    [InlineData(GroupBonusTier.FreeDessert, 0)]
    [InlineData(null, 0)]
    public void RoundTrip_EachTier_ReadsSameTier(GroupBonusTier? tier, int percent)
    {
        var doc = Context.ToDocument(ArrangeOrder(tier, percent));

        var result = Context.FromDocument<Order>(doc);

        Assert.Equal(tier, result.GroupBonusTier);
        Assert.Equal(percent, result.DiscountPercent);
    }

    [Fact]
    public void FromDocument_WithoutBonusAttributes_ReadsNullAndZero()
    {
        var doc = Document.FromAttributeMap(OldOrderMap());

        var result = Context.FromDocument<Order>(doc);

        Assert.Null(result.GroupBonusTier);
        Assert.Equal(0, result.DiscountPercent);
    }

    [Fact]
    public void FromDocument_TierIsDynamoDbNull_ReadsNull()
    {
        var map = OldOrderMap();
        map["groupBonusTier"] = new AttributeValue { NULL = true };

        var result = Context.FromDocument<Order>(Document.FromAttributeMap(map));

        Assert.Null(result.GroupBonusTier);
    }

    [Fact]
    public void FromDocument_TierIsNumericZero_Throws()
    {
        var map = OldOrderMap();
        map["groupBonusTier"] = new AttributeValue { N = "0" };

        var ex = Assert.ThrowsAny<Exception>(() => Context.FromDocument<Order>(Document.FromAttributeMap(map)));

        Assert.True(
            ex is ArgumentOutOfRangeException || ex.InnerException is ArgumentOutOfRangeException,
            $"Expected ArgumentOutOfRangeException, got {ex.GetType().Name} / {ex.InnerException?.GetType().Name}");
    }

    private static Dictionary<string, AttributeValue> OldOrderMap() => new()
    {
        ["orderId"] = new AttributeValue { S = "order-1" },
        ["customerId"] = new AttributeValue { S = "customer-1" },
        ["reservationId"] = new AttributeValue { S = "reservation-1" },
        ["reservationAddress"] = new AttributeValue { S = "Address" },
        ["reservationTime"] = new AttributeValue { S = "Time" },
        ["dishes"] = new AttributeValue { L = [] },
        ["status"] = new AttributeValue { N = "3" }
    };

    private static Order ArrangeOrder(GroupBonusTier? tier, int percent) => new()
    {
        OrderId = "order-1",
        CustomerId = "customer-1",
        ReservationId = "reservation-1",
        ReservationAddress = "Address",
        ReservationTime = "Time",
        Dishes = [],
        GroupBonusTier = tier,
        DiscountPercent = percent
    };
}
