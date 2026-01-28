using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class CharBencodeSerializerTests
{
    private static readonly CharBencodeSerializer Serializer = new();

    [Theory]
    [InlineData('\0')]
    [InlineData('A')]
    [InlineData('z')]
    [InlineData('ñ')]
    [InlineData('\uFFFF')]
    public void TrySerializeAlwaysSucceeds(char value)
    {
        var result = Serializer.TrySerialize(value, out var bencode);

        Assert.True(result);
        Assert.NotNull(bencode);
        Assert.Equal(value, bencode!.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65)]
    [InlineData(122)]
    [InlineData(241)]
    [InlineData(65535)]
    public void TryDeserializeSucceedsForValidCharRange(long value)
    {
        var bencode = new BInteger(value);

        var result = Serializer.TryDeserialize(bencode, out var output);

        Assert.True(result);
        Assert.Equal((char)value, output);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void TryDeserializeFailsForOutOfRangeValues(long value)
    {
        var bencode = new BInteger(value);

        var result = Serializer.TryDeserialize(bencode, out var output);

        Assert.False(result);
        Assert.Equal(default, output);
    }

    [Theory]
    [InlineData('\0')]
    [InlineData('A')]
    [InlineData('z')]
    [InlineData('ñ')]
    [InlineData('\uFFFF')]
    public void RoundTripSerializeThenDeserializePreservesValue(char value)
    {
        var serializeResult = Serializer.TrySerialize(value, out var bencode);

        Assert.True(serializeResult);
        Assert.NotNull(bencode);

        var deserializeResult = Serializer.TryDeserialize(bencode!, out var result);

        Assert.True(deserializeResult);
        Assert.Equal(value, result);
    }
}
