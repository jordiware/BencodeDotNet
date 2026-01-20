using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class StringBencodeSerializerTests
{
    public static IEnumerable<object?[]> NullAndEmptyStringCases()
    {
        yield return new object?[] { null, false, null };
        yield return new object?[] { string.Empty, true, string.Empty };
    }

    [Theory]
    [MemberData(nameof(NullAndEmptyStringCases))]
    public void TrySerializeHandlesNullAndEmptyStringCorrectly(string? input, bool expectedResult,string? expectedOutput)
    {
        var serializer = new StringBencodeSerializer(default(Encoding));

        var result = serializer.TrySerialize(input!, out var bstring);

        Assert.Equal(expectedResult, result);

        if (!expectedResult)
        {
            Assert.Null(bstring);
            return;
        }

        Assert.NotNull(bstring);

        if (expectedOutput == string.Empty)
            Assert.Empty(bstring);
    }

    public static IEnumerable<object[]> RoundTripUtf8Cases()
    {
        yield return new object[] { "Hello" };
        yield return new object[] { "Hello, 世界" };
        yield return new object[] { "¡Hola!" };
    }

    [Theory]
    [MemberData(nameof(RoundTripUtf8Cases))]
    public void TrySerializeAndDeserializeRoundTripWithUtf8Encoding(string value)
    {
        var serializer = new StringBencodeSerializer(Encoding.UTF8);

        var serializeResult = serializer.TrySerialize(value, out var bstring);
        var deserializeResult = serializer.TryDeserialize(bstring!, out var output);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(value, output);
    }

    public static IEnumerable<object[]> RoundTripAsciiCases()
    {
        yield return new object[] { "Hello" };
        yield return new object[] { "ASCII123" };
    }

    [Theory]
    [MemberData(nameof(RoundTripAsciiCases))]
    public void TrySerializeAndDeserializeRoundTripWithAsciiEncoding(string value)
    {
        var serializer = new StringBencodeSerializer(Encoding.ASCII);

        var serializeResult = serializer.TrySerialize(value, out var bstring);
        var deserializeResult = serializer.TryDeserialize(bstring!, out var output);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(value, output);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(128)]
    public void TryDeserializeEmptyAndNonEmptyBstrings(int byteCount)
    {
        var serializer = new StringBencodeSerializer(StringBencodeSerializer.DefaultEncoding);
        var bytes = new byte[byteCount];
        var bstring = new Bstring(bytes);

        var result = serializer.TryDeserialize(bstring, out var output);

        Assert.True(result);
        Assert.NotNull(output);
        Assert.Equal(byteCount == 0 ? string.Empty : StringBencodeSerializer.DefaultEncoding.GetString(bytes), output);
    }
}

public class BencodeSerializerRegistryStringTests
{
    public static IEnumerable<object?[]> RegistryConstructionCases()
    {
        yield return new object?[] { null };
        yield return new object?[] { Encoding.ASCII };
        yield return new object?[] { "ascii" };
    }

    [Theory]
    [MemberData(nameof(RegistryConstructionCases))]
    public void TryGetSerializerInstanceForStringWithArgumentsCreatesSerializer(object? argument)
    {
        var serializer = default(IBencodeSerializer);
        var result = argument is null
            ? BencodeSerializer.TryGetSerializerForType(typeof(string), out serializer)
            : BencodeSerializer.TryGetSerializerForType(typeof(string), out serializer, argument);

        Assert.True(result);
        Assert.NotNull(serializer);
        Assert.IsType<StringBencodeSerializer>(serializer);
    }

    public static IEnumerable<object[]> RegistryRoundTripCases()
    {
        yield return new object[] { Encoding.UTF8, "Hello, 世界" };
        yield return new object[] { Encoding.ASCII, "Hello" };
    }

    [Theory]
    [MemberData(nameof(RegistryRoundTripCases))]
    public void TryGetSerializerInstanceForStringRoundTripsCorrectly(Encoding encoding, string value)
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(string), out var serializer, encoding);

        Assert.True(result);
        Assert.NotNull(serializer);

        var stringSerializer = (StringBencodeSerializer)serializer!;

        var serializeResult = stringSerializer.TrySerialize(value, out var bstring);
        var deserializeResult = stringSerializer.TryDeserialize(bstring!, out var output);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(value, output);
    }

    [Fact]
    public void TryGetSerializerInstanceForStringCreatesDistinctInstancesPerCall()
    {
        BencodeSerializer.TryGetSerializerForType(typeof(string), out var first, Encoding.UTF8);

        BencodeSerializer.TryGetSerializerForType(typeof(string), out var second, Encoding.ASCII);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }
}
