using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.EncodeDecode;

public class BdecoderTests
{
    private static readonly BencodeOptions options = new(textEncoding: Encoding.ASCII);
    private static readonly BencodeDecoder decoder = new(options);

    [Theory]
    [InlineData("i0e", 0)]
    [InlineData("i42e", 42)]
    [InlineData("i-42e", -42)]
    [InlineData("i-420e", -420)]
    [InlineData("i123456789e", 123456789)]
    public async Task DecodeValidInteger(string input, long expected)
    {
        var result = decoder.Decode(input);

        var integer = Assert.IsType<Binteger>(result);
        Assert.Equal(expected, integer.Value);
    }

    [Theory]
    [InlineData("i01e")]
    [InlineData("i1-e")]
    [InlineData("i-0e")]
    [InlineData("i--1e")]
    [InlineData("i+1e")]
    [InlineData("i12xe")]
    [InlineData("i-e")]
    public async Task DecodeInvalidIntegerThrows(string input)
    {
        Assert.Throws<FormatException>(() => decoder.Decode(input));
    }

    [Fact]
    public async Task DecodeEmptyIntegerThrows()
    {
        Assert.Throws<FormatException>(() => decoder.Decode("ie"));
    }

    [Theory]
    [InlineData("0:", "")]
    [InlineData("4:spam", "spam")]
    [InlineData("11:hello world", "hello world")]
    public async Task DecodeValidString(string input, string expected)
    {
        var result = decoder.Decode(input);

        var str = Assert.IsType<Bstring>(result);
        Assert.Equal(expected, Encoding.ASCII.GetString(str.Value));
    }

    [Theory]
    [InlineData("01:a")]
    [InlineData("-1:a")]
    [InlineData("--1:a")]
    [InlineData("3:ab")]
    [InlineData("2:abc")]
    [InlineData("2::ab")]
    [InlineData("2x:ab")]
    public async Task DecodeInvalidStringThrows(string input)
    {
        Assert.Throws<FormatException>(() => decoder.Decode(input));
    }

    [Fact]
    public async Task DecodeEmptyList()
    {
        var result = decoder.Decode("le");

        var list = Assert.IsType<Blist>(result);
        Assert.Empty(list);
    }

    [Fact]
    public async Task DecodeMixedList()
    {
        var result = decoder.Decode("l4:spami42ee");

        var list = Assert.IsType<Blist>(result);
        Assert.Collection(
            list,
            item => Assert.IsType<Bstring>(item),
            item => Assert.Equal(42, Assert.IsType<Binteger>(item).Value)
        );
    }

    [Fact]
    public async Task DecodeEmptyDictionary()
    {
        var result = decoder.Decode("de");

        var dict = Assert.IsType<Bdictionary>(result);
        Assert.Empty(dict);
    }

    [Fact]
    public async Task DecodeSimpleDictionary()
    {
        var result = decoder.Decode("d3:cow3:moo4:spam4:eggse");
        var dict = Assert.IsType<Bdictionary>(result);

        Assert.Equal(new Bstring("moo", Encoding.ASCII), dict[new Bstring("cow", Encoding.ASCII)]);
        Assert.Equal(new Bstring("eggs", Encoding.ASCII), dict[new Bstring("spam", Encoding.ASCII)]);
    }

    [Fact]
    public async Task DecodeComplexDictionary()
    {
        var result = decoder.Decode("d3:barl4:spami42ee3:fooi99ee");
        var dict = Assert.IsType<Bdictionary>(result);

        var bar = Assert.IsType<Blist>(dict[new Bstring("bar", Encoding.ASCII)]);
        Assert.Contains(new Bstring("spam", Encoding.ASCII), bar);
        Assert.Contains(new Binteger(42), bar);
        Assert.Equal(new Binteger(99), dict[new Bstring("foo", Encoding.ASCII)]);
    }

    [Fact]
    public async Task DictionaryKeyMustBeString()
    {
        Assert.Throws<InvalidOperationException>(() => decoder.Decode("di1e3:fooee"));
    }

    [Fact]
    public async Task DictionaryKeysMustBeSorted()
    {
        // "spam" > "cow" -> invalid
        Assert.Throws<FormatException>(() => decoder.Decode("d4:spam3:moo3:cow3:mooee"));
    }

    [Fact]
    public async Task DictionaryMissingValueThrows()
    {
        Assert.Throws<InvalidOperationException>(() => decoder.Decode("d3:fooee"));
    }

    [Fact]
    public async Task DecodeRespectsCancellation()
    {
        var bytes = Encoding.ASCII.GetBytes("l4:spami42ee");
        using var stream = new MemoryStream(bytes);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => decoder.DecodeAsync(stream, cts.Token));
    }
}
