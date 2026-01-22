using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class ArrayBencodeSerializerTests
{
    [Theory]
    [InlineData(null)]
    public void TrySerializeReturnsFalseWhenInputIsNull(int[]? input)
    {
        var serializer = new ArrayBencodeSerializer<int>();

        var result = serializer.TrySerialize(input!, out var output);

        Assert.False(result);
        Assert.Null(output);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 1 })]
    [InlineData(new[] { 1, 2, 3 })]
    public void TrySerializeSerializesIntegerArray(int[] input)
    {
        var serializer = new ArrayBencodeSerializer<int>();

        var result = serializer.TrySerialize(input, out var output);

        Assert.True(result);
        Assert.NotNull(output);
        Assert.Equal(input.Length, output!.Count);
    }

    [Fact]
    public void TrySerializeSerializesStringArray()
    {
        var input = new[] { "a", "b", "c" };
        var serializer = new ArrayBencodeSerializer<string>();

        var result = serializer.TrySerialize(input, out var output);

        Assert.True(result);
        Assert.NotNull(output);
        Assert.Equal(input.Length, output!.Count);
    }

    [Theory]
    [InlineData(null)]
    public void TryDeserializeReturnsFalseWhenInputIsNull(Blist? input)
    {
        var serializer = new ArrayBencodeSerializer<int>();

        var result = serializer.TryDeserialize(input!, out var output);

        Assert.False(result);
        Assert.Null(output);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 1 })]
    [InlineData(new[] { 1, 2, 3 })]
    public void TryDeserializeDeserializesIntegerArray(int[] values)
    {
        var serializer = new ArrayBencodeSerializer<int>();

        var list = new List<Binteger>();
        foreach (var value in values)
            list.Add(new Binteger(value));

        var input = new Blist(list);

        var result = serializer.TryDeserialize(input, out var output);

        Assert.True(result);
        Assert.NotNull(output);
        Assert.Equal(values, output);
    }

    [Fact]
    public void TryDeserializeDeserializesStringArray()
    {
        var values = new[] { "a", "b", };

        var serializer = new ArrayBencodeSerializer<string>();

        var list = new List<Bstring>();
        foreach (var value in values)
            list.Add(new Bstring(value, Encoding.UTF8));

        var input = new Blist(list);

        var result = serializer.TryDeserialize(input, out var output);

        Assert.True(result);
        Assert.NotNull(output);
        Assert.Equal(values, output);
    }

    [Theory]
    [InlineData(1)]
    public void TryDeserializeReturnsFalseWhenElementTypeIsInvalid(int value)
    {
        var serializer = new ArrayBencodeSerializer<string>();
        var input = new Blist([new Binteger(value)]);

        var result = serializer.TryDeserialize(input, out var output);

        Assert.False(result);
        Assert.Null(output);
    }

    [Theory]
    [InlineData(new[] { 1, 2, 3 })]
    public void SerializeThenDeserializeRoundTripsIntegerArray(int[] values)
    {
        var serializer = new ArrayBencodeSerializer<int>();

        var serializeResult = serializer.TrySerialize(values, out var encoded);
        var deserializeResult = serializer.TryDeserialize(encoded!, out var decoded);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.NotNull(decoded);
        Assert.Equal(values, decoded);
    }

    [Fact]
    public void SerializeThenDeserializeRoundTripsStringArray()
    {
        var values = new[] { "x", "y", "z" };
        var serializer = new ArrayBencodeSerializer<string>();

        var serializeResult = serializer.TrySerialize(values, out var encoded);
        var deserializeResult = serializer.TryDeserialize(encoded!, out var decoded);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.NotNull(decoded);
        Assert.Equal(values, decoded);
    }
}
