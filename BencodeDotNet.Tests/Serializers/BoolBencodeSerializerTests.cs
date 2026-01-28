using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class BoolBencodeSerializerTests
{
    private static readonly BoolBencodeSerializer Serializer = new();

    [Fact]
    public void TrySerializeFalseProducesIntegerZero()
    {
        var result = Serializer.TrySerialize(false, out var output);

        Assert.True(result);
        Assert.NotNull(output);
        Assert.Equal(0, output.Value);
    }

    [Fact]
    public void TrySerializeTrueProducesIntegerOne()
    {
        var result = Serializer.TrySerialize(true, out var output);

        Assert.True(result);
        Assert.NotNull(output);
        Assert.Equal(1, output.Value);
    }

    [Fact]
    public void TryDeserializeIntegerZeroProducesFalse()
    {
        var result = Serializer.TryDeserialize(new BInteger(0), out var output);

        Assert.True(result);
        Assert.False(output);
    }

    [Fact]
    public void TryDeserializeIntegerOneProducesTrue()
    {
        var result = Serializer.TryDeserialize(new BInteger(1), out var output);

        Assert.True(result);
        Assert.True(output);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(42)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void TryDeserializeInvalidIntegerReturnsFalse(long value)
    {
        var result = Serializer.TryDeserialize(new BInteger(value), out var output);

        Assert.False(result);
        Assert.False(output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RoundTripBoolPreservesValue(bool value)
    {
        var serializeResult = Serializer.TrySerialize(value, out var serialized);
        Assert.True(serializeResult);
        Assert.NotNull(serialized);

        var deserializeResult = Serializer.TryDeserialize(serialized, out var deserialized);
        Assert.True(deserializeResult);
        Assert.Equal(value, deserialized);
    }
}
