using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class GuidBencodeSerializerTests
{
    private static readonly GuidBencodeSerializer Serializer = new();

    [Fact]
    public void TrySerializeValidGuidReturnsTrueAndBstringWith16Bytes()
    {
        var guid = Guid.NewGuid();

        var result = Serializer.TrySerialize(guid, out var bstring);

        Assert.True(result);
        Assert.NotNull(bstring);
        Assert.Equal(16, bstring!.Count);
    }

    [Fact]
    public void TryDeserializeValidBstringWith16BytesReturnsTrueAndGuid()
    {
        var guid = Guid.NewGuid();
        var bytes = guid.ToByteArray();
        var bstring = new Bstring(bytes);

        var result = Serializer.TryDeserialize(bstring, out var deserialized);

        Assert.True(result);
        Assert.Equal(guid, deserialized);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(17)]
    [InlineData(32)]
    public void TryDeserializeInvalidBstringLengthReturnsFalse(int length)
    {
        var bytes = new byte[length];
        var bstring = new Bstring(bytes);

        var result = Serializer.TryDeserialize(bstring, out var deserialized);

        Assert.False(result);
        Assert.Equal(default, deserialized);
    }

    [Fact]
    public void TryDeserializeNullBstringReturnsFalse()
    {
        var result = Serializer.TryDeserialize(null!, out var deserialized);

        Assert.False(result);
        Assert.Equal(default, deserialized);
    }

    [Fact]
    public void RoundTripGuidSerializeThenDeserializeReturnsOriginalValue()
    {
        var original = Guid.NewGuid();

        var serializeResult = Serializer.TrySerialize(original, out var bstring);
        var deserializeResult = Serializer.TryDeserialize(bstring!, out var roundTripped);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void RoundTripEmptyGuidSerializeThenDeserializeReturnsEmptyGuid()
    {
        var original = Guid.Empty;

        var serializeResult = Serializer.TrySerialize(original, out var bstring);
        var deserializeResult = Serializer.TryDeserialize(bstring!, out var roundTripped);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(original, roundTripped);
    }
}
