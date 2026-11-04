using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using System.Linq;

namespace APIHandler.Converters;

// Stores a nullable enum as its name. A native enum is stored as a number, and the first
// value of an enum is 0, which a missing or null value must not be mistaken for.
public class NullableEnumAsStringConverter<TEnum> : IPropertyConverter where TEnum : struct, Enum
{
    public DynamoDBEntry ToEntry(object value)
    {
        // The SDK does not call ToEntry for a null property. This branch is defensive.
        if (value is null)
        {
            return DynamoDBNull.Null;
        }

        if (value is not TEnum enumValue)
        {
            throw new ArgumentException($"Value must be of type {typeof(TEnum).Name}", nameof(value));
        }

        return new Primitive(enumValue.ToString());
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry is null or DynamoDBNull)
        {
            return null!;
        }

        // Enum.TryParse accepts numeric text such as "0", so the text must also match a name.
        if (entry is Primitive { Type: DynamoDBEntryType.String } primitive
            && primitive.Value is string text
            && Enum.GetNames<TEnum>().Contains(text, StringComparer.Ordinal)
            && Enum.TryParse(text, out TEnum result))
        {
            return result;
        }

        throw new ArgumentOutOfRangeException(nameof(entry), $"Value is not a defined name of enum {typeof(TEnum).Name}");
    }
}
