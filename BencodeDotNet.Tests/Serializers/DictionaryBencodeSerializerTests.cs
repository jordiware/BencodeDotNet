using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class DictionaryBencodeSerializerTests
{
    public static IEnumerable<object[]> SimpleDictionaryData()
    {
        yield return new object[]
        {
            new Dictionary<string, int>
            {
                ["a"] = 1,
                ["b"] = 2,
                ["c"] = 3
            }
        };

        yield return new object[]
        {
            new Dictionary<string, int>
            {
                [""] = 0,
                ["x"] = -42
            }
        };
    }

    public static IEnumerable<object[]> GuidKeyDictionaryData()
    {
        yield return new object[]
        {
            new Dictionary<Guid, string>
            {
                [Guid.NewGuid()] = "one",
                [Guid.NewGuid()] = "two"
            }
        };
    }

    [Theory]
    [MemberData(nameof(SimpleDictionaryData))]
    public void TrySerializeValidDictionaryReturnsTrue(Dictionary<string, int> input)
    {
        var serializer = new DictionaryBencodeSerializer<string, int>();

        var result = serializer.TrySerialize(input, out var output);

        Assert.True(result);
        Assert.NotNull(output);
        Assert.Equal(input.Count, output!.Count);
    }

    [Theory]
    [MemberData(nameof(SimpleDictionaryData))]
    public void TryDeserializeValidDictionaryReturnsTrue(Dictionary<string, int> input)
    {
        var serializer = new DictionaryBencodeSerializer<string, int>();

        serializer.TrySerialize(input, out var encoded);
        var result = serializer.TryDeserialize(encoded!, out var decoded);

        Assert.True(result);
        Assert.NotNull(decoded);
        Assert.Equal(input.Count, decoded!.Count);
    }

    [Theory]
    [MemberData(nameof(SimpleDictionaryData))]
    public void DictionaryRoundTripPreservesKeysAndValues(Dictionary<string, int> input)
    {
        var serializer = new DictionaryBencodeSerializer<string, int>();

        serializer.TrySerialize(input, out var encoded);
        serializer.TryDeserialize(encoded!, out var decoded);

        foreach (var (key, value) in input)
        {
            Assert.True(decoded!.ContainsKey(key));
            Assert.Equal(value, decoded[key]);
        }
    }

    [Theory]
    [MemberData(nameof(GuidKeyDictionaryData))]
    public void DictionaryWithGuidKeysRoundTripsCorrectly(Dictionary<Guid, string> input)
    {
        var serializer = new DictionaryBencodeSerializer<Guid, string>();

        Assert.True(serializer.TrySerialize(input, out var encoded));
        Assert.True(serializer.TryDeserialize(encoded!, out var decoded));

        Assert.Equal(input.Count, decoded!.Count);

        foreach (var (key, value) in input)
        {
            Assert.True(decoded.ContainsKey(key));
            Assert.Equal(value, decoded[key]);
        }
    }

    [Fact]
    public void EmptyDictionarySerializesAndDeserializesCorrectly()
    {
        var serializer = new DictionaryBencodeSerializer<string, int>();
        var input = new Dictionary<string, int>();

        var serializeResult = serializer.TrySerialize(input, out var encoded);
        var deserializeResult = serializer.TryDeserialize(encoded!, out var decoded);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.NotNull(decoded);
        Assert.Empty(decoded!);
    }

    [Fact]
    public void TrySerializeReturnsFalseIfValueIsNull()
    {
        var serializer = new DictionaryBencodeSerializer<string, string>();
        var input = new Dictionary<string, string>
        {
            ["key"] = null!
        };

        var result = serializer.TrySerialize(input, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryDeserializeReturnsFalseOnLogicalKeyCollision()
    {
        var serializer = new DictionaryBencodeSerializer<int, string>();

        var bdict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString("i1e"u8.ToArray())] = new BString("a"u8.ToArray()),
            [new BString("i01e"u8.ToArray())] = new BString("b"u8.ToArray())
        });

        var result = serializer.TryDeserialize(bdict, out _);

        Assert.False(result);
    }
}
