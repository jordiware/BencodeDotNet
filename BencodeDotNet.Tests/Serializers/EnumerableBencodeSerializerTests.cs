using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class EnumerableBencodeSerializerTests
{
    public static IEnumerable<object[]> IntEnumerableSerializationData()
    {
        yield return new object[] { Array.Empty<int>() };
        yield return new object[] { new[] { 0 } };
        yield return new object[] { new[] { 1, 2, 3, 4, 5 } };
        yield return new object[] { new List<int> { -1, 0, 42, int.MaxValue } };
    }

    public static IEnumerable<object[]> StringEnumerableSerializationData()
    {
        yield return new object[] { Array.Empty<string>() };
        yield return new object[] { new[] { "a" } };
        yield return new object[] { new[] { "foo", "bar", "baz" } };
        yield return new object[] { new List<string> { string.Empty, "hello", "world" } };
    }

    public static IEnumerable<object[]> NoEmptyIntEnumerableSerializationData()
    {
        yield return new object[] { new[] { 0 } };
        yield return new object[] { new[] { 1, 2, 3, 4, 5 } };
        yield return new object[] { new List<int> { -1, 0, 42, int.MaxValue } };
    }

    public static IEnumerable<object[]> NoEmptyStringEnumerableSerializationData()
    {
        yield return new object[] { new[] { "a" } };
        yield return new object[] { new[] { "foo", "bar", "baz" } };
        yield return new object[] { new List<string> { string.Empty, "hello", "world" } };
    }

    public static IEnumerable<object[]> NullEnumerableData()
    {
        yield return new object[] { null! };
    }

    [Theory]
    [MemberData(nameof(IntEnumerableSerializationData))]
    public void TrySerializeIntEnumerableProducesBlist(IEnumerable<int> input)
    {
        var serializer = new EnumerableBencodeSerializer<int>();

        var result = serializer.TrySerialize(input, out var blist);

        Assert.True(result);
        Assert.NotNull(blist);
        Assert.Equal(input.Count(), blist!.Count);
    }

    [Theory]
    [MemberData(nameof(StringEnumerableSerializationData))]
    public void TrySerializeStringEnumerableProducesBlist(IEnumerable<string> input)
    {
        var serializer = new EnumerableBencodeSerializer<string>();

        var result = serializer.TrySerialize(input, out var blist);

        Assert.True(result);
        Assert.NotNull(blist);
        Assert.Equal(input.Count(), blist!.Count);
    }

    [Theory]
    [MemberData(nameof(NullEnumerableData))]
    public void TrySerializeNullEnumerableFails(IEnumerable<int> input)
    {
        var serializer = new EnumerableBencodeSerializer<int>();

        var result = serializer.TrySerialize(input, out var blist);

        Assert.False(result);
        Assert.Null(blist);
    }

    [Theory]
    [MemberData(nameof(IntEnumerableSerializationData))]
    public void TryDeserializeBlistProducesEnumerableOfInt(IEnumerable<int> input)
    {
        var serializer = new EnumerableBencodeSerializer<int>();

        var serializeResult = serializer.TrySerialize(input, out var blist);
        var deserializeResult = serializer.TryDeserialize(blist!, out var output);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.NotNull(output);
        Assert.Equal(input, output!);
    }

    [Theory]
    [MemberData(nameof(StringEnumerableSerializationData))]
    public void TryDeserializeBlistProducesEnumerableOfString(IEnumerable<string> input)
    {
        var serializer = new EnumerableBencodeSerializer<string>();

        var serializeResult = serializer.TrySerialize(input, out var blist);
        var deserializeResult = serializer.TryDeserialize(blist!, out var output);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.NotNull(output);
        Assert.Equal(input, output!);
    }

    [Theory]
    [MemberData(nameof(IntEnumerableSerializationData))]
    public void RoundTripPreservesEnumerableOrder(IEnumerable<int> input)
    {
        var serializer = new EnumerableBencodeSerializer<int>();

        var serializeResult = serializer.TrySerialize(input, out var blist);
        var deserializeResult = serializer.TryDeserialize(blist!, out var output);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(input.ToArray(), output!.ToArray());
    }

    [Theory]
    [MemberData(nameof(NoEmptyIntEnumerableSerializationData))]
    public void TryDeserializeFailsWhenElementTypeDoesNotMatch(IEnumerable<int> input)
    {
        var intSerializer = new EnumerableBencodeSerializer<int>();
        var stringSerializer = new EnumerableBencodeSerializer<string>();

        var serializeResult = intSerializer.TrySerialize(input, out var blist);
        var deserializeResult = stringSerializer.TryDeserialize(blist!, out var output);

        Assert.True(serializeResult);
        Assert.False(deserializeResult);
        Assert.Null(output);
    }
}
