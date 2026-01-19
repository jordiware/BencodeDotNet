using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class NumericByteBencodeSerializerTests
{
    private static readonly NumericByteBencodeSerializer Serializer = new();

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)1)]
    [InlineData((byte)127)]
    [InlineData((byte)255)]
    public void TrySerializeValidByteAlwaysSucceeds(byte value)
    {
        var result = Serializer.TrySerialize(value, out var bencode);

        Assert.True(result);
        Assert.NotNull(bencode);
        Assert.Equal(value, bencode.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(255)]
    public void TryDeserializeValidByteRangeSucceeds(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.True(result);
        Assert.Equal((byte)encodedValue, value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-128)]
    [InlineData(256)]
    [InlineData(1000)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void TryDeserializeOutOfByteRangeFails(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)1)]
    [InlineData((byte)42)]
    [InlineData((byte)255)]
    public void RoundTripByteValueIsPreserved(byte original)
    {
        Assert.True(Serializer.TrySerialize(original, out var binteger));
        Assert.True(Serializer.TryDeserialize(binteger, out var roundTripped));

        Assert.Equal(original, roundTripped);
    }
}

public class SbyteBencodeSerializerTests
{
    private static readonly SbyteBencodeSerializer Serializer = new();

    [Theory]
    [InlineData((sbyte)-128)]
    [InlineData((sbyte)-1)]
    [InlineData((sbyte)0)]
    [InlineData((sbyte)1)]
    [InlineData((sbyte)127)]
    public void TrySerializeValidSbyteAlwaysSucceeds(sbyte value)
    {
        var result = Serializer.TrySerialize(value, out var bencode);

        Assert.True(result);
        Assert.NotNull(bencode);
        Assert.Equal(value, bencode.Value);
    }

    [Theory]
    [InlineData(-128)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    public void TryDeserializeValidSbyteRangeSucceeds(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.True(result);
        Assert.Equal((sbyte)encodedValue, value);
    }

    [Theory]
    [InlineData(-129)]
    [InlineData(-1000)]
    [InlineData(128)]
    [InlineData(1000)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void TryDeserializeOutOfSbyteRangeFails(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = Serializer.TryDeserialize(binteger, out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Theory]
    [InlineData((sbyte)-128)]
    [InlineData((sbyte)-1)]
    [InlineData((sbyte)0)]
    [InlineData((sbyte)42)]
    [InlineData((sbyte)127)]
    public void RoundTripSbyteValueIsPreserved(sbyte original)
    {
        Assert.True(Serializer.TrySerialize(original, out var binteger));
        Assert.True(Serializer.TryDeserialize(binteger, out var roundTripped));

        Assert.Equal(original, roundTripped);
    }
}
