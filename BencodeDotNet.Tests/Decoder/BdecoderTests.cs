using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.Decoder;

public class BdecoderTests
{
    [Theory]
    [InlineData("i0e", 0)]
    [InlineData("i42e", 42)]
    [InlineData("i-42e", -42)]
    [InlineData("i-420e", -420)]
    [InlineData("i123456789e", 123456789)]
    public async Task DecodeValidInteger(string input, long expected)
    {
        var decoder = Bdecoder.FromString(input, Encoding.ASCII);

        var result = await decoder.DecodeAsync();

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
        var decoder = Bdecoder.FromString(input, Encoding.ASCII);

        await Assert.ThrowsAsync<FormatException>(() => decoder.DecodeAsync());
    }

    [Fact]
    public async Task DecodeEmptyIntegerThrows()
    {
        var decoder = Bdecoder.FromString("ie", Encoding.ASCII);

        await Assert.ThrowsAsync<FormatException>(() => decoder.DecodeAsync());
    }

    [Theory]
    [InlineData("0:", "")]
    [InlineData("4:spam", "spam")]
    [InlineData("11:hello world", "hello world")]
    public async Task DecodeValidString(string input, string expected)
    {
        var decoder = Bdecoder.FromString(input, Encoding.ASCII);

        var result = await decoder.DecodeAsync();

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
        var decoder = Bdecoder.FromString(input, Encoding.ASCII);

        await Assert.ThrowsAsync<FormatException>(() => decoder.DecodeAsync());
    }

    [Fact]
    public async Task DecodeEmptyList()
    {
        var decoder = Bdecoder.FromString("le", Encoding.ASCII);

        var result = await decoder.DecodeAsync();

        var list = Assert.IsType<Blist>(result);
        Assert.Empty(list);
    }

    [Fact]
    public async Task DecodeMixedList()
    {
        var decoder = Bdecoder.FromString("l4:spami42ee", Encoding.ASCII);

        var result = await decoder.DecodeAsync();

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
        var decoder = Bdecoder.FromString("de", Encoding.ASCII);

        var result = await decoder.DecodeAsync();

        var dict = Assert.IsType<Bdictionary>(result);
        Assert.Empty(dict);
    }

    [Fact]
    public async Task DecodeSimpleDictionary()
    {
        var decoder = Bdecoder.FromString("d3:cow3:moo4:spam4:eggse", Encoding.ASCII);

        var result = await decoder.DecodeAsync();
        var dict = Assert.IsType<Bdictionary>(result);

        Assert.Equal(new Bstring("moo", Encoding.ASCII), dict[new Bstring("cow", Encoding.ASCII)]);
        Assert.Equal(new Bstring("eggs", Encoding.ASCII), dict[new Bstring("spam", Encoding.ASCII)]);
    }

    [Fact]
    public async Task DecodeComplexDictionary()
    {
        var decoder = Bdecoder.FromString("d3:barl4:spami42ee3:fooi99ee", Encoding.ASCII);

        var result = await decoder.DecodeAsync();
        var dict = Assert.IsType<Bdictionary>(result);

        var bar = Assert.IsType<Blist>(dict[new Bstring("bar", Encoding.ASCII)]);
        Assert.Contains(new Bstring("spam", Encoding.ASCII), bar);
        Assert.Contains(new Binteger(42), bar);
        Assert.Equal(new Binteger(99), dict[new Bstring("foo", Encoding.ASCII)]);
    }

    [Fact]
    public async Task DictionaryKeyMustBeString()
    {
        var decoder = Bdecoder.FromString("di1e3:fooee", Encoding.ASCII);

        await Assert.ThrowsAsync<InvalidOperationException>(() => decoder.DecodeAsync());
    }

    [Fact]
    public async Task DictionaryKeysMustBeSorted()
    {
        // "spam" > "cow" -> invalid
        var decoder = Bdecoder.FromString("d4:spam3:moo3:cow3:mooee", Encoding.ASCII);

        await Assert.ThrowsAsync<FormatException>(() => decoder.DecodeAsync());
    }

    [Fact]
    public async Task DictionaryMissingValueThrows()
    {
        var decoder = Bdecoder.FromString("d3:fooee", Encoding.ASCII);

        await Assert.ThrowsAsync<InvalidOperationException>(() => decoder.DecodeAsync());
    }

    [Fact]
    public async Task DecodeRespectsCancellation()
    {
        var decoder = Bdecoder.FromString("l4:spami42ee", Encoding.ASCII);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => decoder.DecodeAsync(cts.Token));
    }
}
