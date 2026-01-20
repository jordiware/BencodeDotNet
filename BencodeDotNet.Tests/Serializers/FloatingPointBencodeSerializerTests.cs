using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class FloatBencodeSerializerTests
{
    private static readonly FloatBencodeSerializer Serializer = new();

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(-1f)]
    [InlineData(3.5f)]
    [InlineData(123.456f)]
    public void TrySerializeValidFloatReturnsTrue(float value)
    {
        var result = Serializer.TrySerialize(value, out var bstring);

        Assert.True(result);
        Assert.NotNull(bstring);
    }

    [Theory]
    [InlineData("0", 0f)]
    [InlineData("1", 1f)]
    [InlineData("-1", -1f)]
    [InlineData("3.5", 3.5f)]
    [InlineData("123.456", 123.456f)]
    public void TryDeserializeValidBstringReturnsTrue(string text, float expected)
    {
        var input = new Bstring(text, Encoding.ASCII);

        var result = Serializer.TryDeserialize(input, out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(-1f)]
    [InlineData(3.5f)]
    [InlineData(123.456f)]
    public void RoundTripPreservesFloatValue(float original)
    {
        var serializeResult = Serializer.TrySerialize(original, out var bstring);
        var deserializeResult = Serializer.TryDeserialize(bstring!, out var roundTrip);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(original, roundTrip);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("3,14")]
    public void TryDeserializeInvalidBstringReturnsFalse(string text)
    {
        var input = new Bstring(text, Encoding.ASCII);

        var result = Serializer.TryDeserialize(input, out _);

        Assert.False(result);
    }
}

public class DoubleBencodeSerializerTests
{
    private static readonly DoubleBencodeSerializer Serializer = new();

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-1d)]
    [InlineData(3.141592653589793)]
    [InlineData(1.2345678901234567)]
    public void TrySerializeValidDoubleReturnsTrue(double value)
    {
        var result = Serializer.TrySerialize(value, out var bstring);

        Assert.True(result);
        Assert.NotNull(bstring);
    }

    [Theory]
    [InlineData("0", 0d)]
    [InlineData("1", 1d)]
    [InlineData("-1", -1d)]
    [InlineData("3.141592653589793", 3.141592653589793)]
    [InlineData("1.2345678901234567", 1.2345678901234567)]
    public void TryDeserializeValidBstringReturnsTrue(string text, double expected)
    {
        var input = new Bstring(text, Encoding.ASCII);

        var result = Serializer.TryDeserialize(input, out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-1d)]
    [InlineData(3.141592653589793)]
    [InlineData(1.2345678901234567)]
    public void RoundTripPreservesDoubleValue(double original)
    {
        var serializeResult = Serializer.TrySerialize(original, out var bstring);
        var deserializeResult = Serializer.TryDeserialize(bstring!, out var roundTrip);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(original, roundTrip);
    }

    [Theory]
    [InlineData("")]
    [InlineData("NaNd")]
    [InlineData("invalid")]
    public void TryDeserializeInvalidBstringReturnsFalse(string text)
    {
        var input = new Bstring(text, Encoding.ASCII);

        var result = Serializer.TryDeserialize(input, out _);

        Assert.False(result);
    }
}

public class DecimalBencodeSerializerTests
{
    private static readonly DecimalBencodeSerializer Serializer = new();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(12345.6789)]
    public void TrySerializeValidDecimalReturnsTrue(decimal value)
    {
        var result = Serializer.TrySerialize(value, out var bstring);

        Assert.True(result);
        Assert.NotNull(bstring);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("-1", -1)]
    [InlineData("12345.6789", 12345.6789)]
    public void TryDeserializeValidBstringReturnsTrue(string text, decimal expected)
    {
        var input = new Bstring(text, Encoding.ASCII);

        var result = Serializer.TryDeserialize(input, out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(12345.6789)]
    [InlineData(7922816251426433d)]
    public void RoundTripPreservesDecimalValue(double original)
    {
        var value = (decimal)original;

        var serializeResult = Serializer.TrySerialize(value, out var bstring);
        var deserializeResult = Serializer.TryDeserialize(bstring!, out var roundTrip);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(value, roundTrip);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-decimal")]
    [InlineData("1,23")]
    public void TryDeserializeInvalidBstringReturnsFalse(string text)
    {
        var input = new Bstring(text, Encoding.ASCII);

        var result = Serializer.TryDeserialize(input, out _);

        Assert.False(result);
    }
}
