using Jordiware.BencodeDotNet.Attributes;
using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.EncodeDecode;

public class BencodeRoundTripTests
{
    [BencodeSerializer(typeof(SimpleObjectSerializer))]
    private sealed class SimpleObject
    {
        public int Value { get; init; }
    }

    private sealed class SimpleObjectSerializer
        : BencodeSerializer<SimpleObject, Bdictionary>
    {
        private static readonly Bstring key = new Bstring("value", Encoding.UTF8);

        public override bool TrySerialize(SimpleObject value, out Bdictionary result)
        {
            result = new Bdictionary(new Dictionary<Bstring, IBobject>
            {
                [key] = new Binteger(value.Value)
            });
            return true;
        }

        public override bool TryDeserialize(Bdictionary value, out SimpleObject result)
        {
            result = new SimpleObject
            {
                Value = (int)((Binteger)value[key]).Value
            };
            return true;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    public void IntegerRoundTripsCorrectly(long value)
    {
        var encoder = new BencodeEncoder();
        var decoder = new BencodeDecoder();

        var encoded = encoder.Encode(value);
        var decoded = decoder.Decode<long>(encoded.ToBinaryEncoding());

        Assert.Equal(value, decoded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("bencode")]
    public void StringRoundTripsCorrectly(string value)
    {
        var encoder = new BencodeEncoder();
        var decoder = new BencodeDecoder();

        var encoded = encoder.Encode(value);
        var decoded = decoder.Decode<string>(encoded.ToBinaryEncoding());

        Assert.Equal(value, decoded);
    }

    [Theory]
    [InlineData(new int[] { })]
    [InlineData(new[] { 1 })]
    [InlineData(new[] { 1, 2, 3 })]
    public void ListRoundTripsCorrectly(int[] values)
    {
        var encoder = new BencodeEncoder();
        var decoder = new BencodeDecoder();

        var encoded = encoder.Encode(values);
        var decoded = decoder.Decode<int[]>(encoded.ToBinaryEncoding());

        Assert.Equal(values, decoded);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void DictionaryRoundTripsCorrectly(int value)
    {
        var encoder = new BencodeEncoder();
        var decoder = new BencodeDecoder();

        var input = new Dictionary<string, int>
        {
            ["value"] = value
        };

        var encoded = encoder.Encode(input);
        var decoded = decoder.Decode<Dictionary<string, int>>(encoded.ToBinaryEncoding());

        Assert.Equal(input, decoded);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    public void AttributedObjectRoundTripsCorrectly(int value)
    {
        var encoder = new BencodeEncoder();
        var decoder = new BencodeDecoder();

        var input = new SimpleObject { Value = value };

        var encoded = encoder.Encode(input);
        var decoded = decoder.Decode<SimpleObject>(encoded.ToBinaryEncoding());

        Assert.NotNull(decoded);
        Assert.Equal(input.Value, decoded.Value);
    }

    [Theory]
    [InlineData(1)]
    public void NestedObjectRoundTripsCorrectly(int value)
    {
        var encoder = new BencodeEncoder();
        var decoder = new BencodeDecoder();

        var input = new Dictionary<string, int[]>
        {
            ["number"] = [value, value + 1],
            ["list"] = [value, value + 1]
        };

        var encoded = encoder.Encode(input);
        var decoded = decoder.Decode<Dictionary<string, int[]>>(encoded.ToBinaryEncoding());

        Assert.NotNull(decoded);
        Assert.Equal(value, decoded["number"][0]);
    }
}
