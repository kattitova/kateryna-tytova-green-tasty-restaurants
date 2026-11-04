using Amazon.DynamoDBv2.DocumentModel;
using APIHandler.Converters;
using APIHandler.Models.GuestSessions;

namespace APIHandler.Tests.Converters;

public class NullableEnumAsStringConverterTests
{
    private readonly NullableEnumAsStringConverter<GroupBonusTier> _sut = new();

    // Defensive only: the SDK never calls ToEntry for a null property.
    [Fact]
    public void ToEntry_Null_ReturnsDynamoDbNull()
    {
        var entry = _sut.ToEntry(null!);

        Assert.IsType<DynamoDBNull>(entry);
    }

    [Theory]
    [InlineData(GroupBonusTier.TenPercentDiscount, "TenPercentDiscount")]
    [InlineData(GroupBonusTier.FreeDessert, "FreeDessert")]
    public void ToEntry_Tier_ReturnsStringPrimitiveWithName(GroupBonusTier tier, string expectedName)
    {
        var entry = _sut.ToEntry(tier);

        var primitive = Assert.IsType<Primitive>(entry);
        Assert.Equal(DynamoDBEntryType.String, primitive.Type);
        Assert.Equal(expectedName, primitive.AsString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData("TenPercentDiscount")]
    public void ToEntry_WrongType_ThrowsArgumentException(object value)
    {
        Assert.Throws<ArgumentException>(() => _sut.ToEntry(value));
    }

    [Fact]
    public void FromEntry_DynamoDbNull_ReturnsNull()
    {
        Assert.Null(_sut.FromEntry(new DynamoDBNull()));
    }

    [Fact]
    public void FromEntry_NullEntry_ReturnsNull()
    {
        Assert.Null(_sut.FromEntry(null!));
    }

    [Theory]
    [InlineData("TenPercentDiscount", GroupBonusTier.TenPercentDiscount)]
    [InlineData("FreeDessert", GroupBonusTier.FreeDessert)]
    public void FromEntry_Name_ReturnsTier(string name, GroupBonusTier expected)
    {
        var result = _sut.FromEntry(new Primitive(name));

        Assert.Equal(expected, Assert.IsType<GroupBonusTier>(result));
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("0")]
    [InlineData("tenpercentdiscount")]
    public void FromEntry_UnknownOrNumericText_ThrowsArgumentOutOfRangeException(string text)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.FromEntry(new Primitive(text)));
    }

    [Fact]
    public void FromEntry_NumericPrimitive_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.FromEntry(new Primitive("0", saveAsNumeric: true)));
    }
}
