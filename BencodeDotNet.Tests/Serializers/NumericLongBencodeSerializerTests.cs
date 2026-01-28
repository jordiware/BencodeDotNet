using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class LongBencodeSerializerTests
{
    private static readonly LongBencodeSerializer Serializer = new();

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    public void TrySerializeValidLongAlwaysSucceeds(long value)
    {
        var result = Serializer.TrySerialize(value, out var binteger);

        Assert.True(result);
        Assert.NotNull(binteger);
        Assert.Equal(value, binteger.Value);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    public void TryDeserializeValidLongRangeSucceeds(long encodedValue)
    {
        var binteger = new BInteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.True(result);
        Assert.Equal(encodedValue, value);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(42L)]
    [InlineData(long.MaxValue)]
    public void RoundTripLongValueIsPreserved(long original)
    {
        Assert.True(Serializer.TrySerialize(original, out var binteger));
        Assert.True(Serializer.TryDeserialize(binteger, out var roundTripped));

        Assert.Equal(original, roundTripped);
    }
}

public class UlongBencodeSerializerTests
{
    private static readonly UlongBencodeSerializer Serializer = new();

    [Theory]
    [InlineData((ulong)0)]
    [InlineData((ulong)1)]
    [InlineData((ulong)42)]
    [InlineData((ulong)long.MaxValue)]
    public void TrySerializeValidUlongAlwaysSucceeds(ulong value)
    {
        var result = Serializer.TrySerialize(value, out var binteger);

        Assert.True(result);
        Assert.NotNull(binteger);
        Assert.Equal(value, (ulong)binteger!.Value);
    }

    [Theory]
    [InlineData((ulong)long.MaxValue + 1)]
    [InlineData(ulong.MaxValue)]
    public void TrySerializeOutOfLongRangeFails(ulong value)
    {
        var result = Serializer.TrySerialize(value, out var binteger);

        Assert.False(result);
        Assert.Null(binteger);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(42L)]
    [InlineData(long.MaxValue)]
    public void TryDeserializeValidUlongRangeSucceeds(long encodedValue)
    {
        var binteger = new BInteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.True(result);
        Assert.Equal((ulong)encodedValue, value);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(-42L)]
    [InlineData(long.MinValue)]
    public void TryDeserializeOutOfUlongRangeFails(long encodedValue)
    {
        var binteger = new BInteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Theory]
    [InlineData((ulong)0)]
    [InlineData((ulong)1)]
    [InlineData((ulong)42)]
    [InlineData((ulong)long.MaxValue)]
    public void RoundTripUlongValueIsPreserved(ulong original)
    {
        Assert.True(Serializer.TrySerialize(original, out var binteger));
        Assert.True(Serializer.TryDeserialize(binteger!, out var roundTripped));

        Assert.Equal(original, roundTripped);
    }
}
