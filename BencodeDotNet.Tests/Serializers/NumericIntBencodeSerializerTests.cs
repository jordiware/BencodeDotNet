using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class IntBencodeSerializerTests
{
    private static readonly IntBencodeSerializer Serializer = new();

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void TrySerializeValidIntAlwaysSucceeds(int value)
    {
        var result = Serializer.TrySerialize(value, out var binteger);

        Assert.True(result);
        Assert.NotNull(binteger);
        Assert.Equal(value, binteger.Value);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void TryDeserializeValidIntRangeSucceeds(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.True(result);
        Assert.Equal((int)encodedValue, value);
    }

    [Theory]
    [InlineData((long)int.MinValue - 1)]
    [InlineData(-5_000_000_000)]
    [InlineData((long)int.MaxValue + 1)]
    [InlineData(5_000_000_000)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void TryDeserializeOutOfIntRangeFails(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    public void RoundTripIntValueIsPreserved(int original)
    {
        Assert.True(Serializer.TrySerialize(original, out var binteger));
        Assert.True(Serializer.TryDeserialize(binteger, out var roundTripped));

        Assert.Equal(original, roundTripped);
    }
}

public class UintBencodeSerializerTests
{
    private static readonly UintBencodeSerializer Serializer = new();

    [Theory]
    [InlineData((uint)0)]
    [InlineData((uint)1)]
    [InlineData((uint)42)]
    [InlineData(uint.MaxValue)]
    public void TrySerializeValidUintAlwaysSucceeds(uint value)
    {
        var result = Serializer.TrySerialize(value, out var binteger);

        Assert.True(result);
        Assert.NotNull(binteger);
        Assert.Equal(value, binteger.Value);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(42L)]
    [InlineData(uint.MaxValue)]
    public void TryDeserializeValidUintRangeSucceeds(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.True(result);
        Assert.Equal((uint)encodedValue, value);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(-42L)]
    [InlineData((long)uint.MaxValue + 1)]
    [InlineData(5_000_000_000)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void TryDeserializeOutOfUintRangeFails(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Theory]
    [InlineData((uint)0)]
    [InlineData((uint)1)]
    [InlineData((uint)42)]
    [InlineData(uint.MaxValue)]
    public void RoundTripUintValueIsPreserved(uint original)
    {
        Assert.True(Serializer.TrySerialize(original, out var binteger));
        Assert.True(Serializer.TryDeserialize(binteger, out var roundTripped));

        Assert.Equal(original, roundTripped);
    }
}
