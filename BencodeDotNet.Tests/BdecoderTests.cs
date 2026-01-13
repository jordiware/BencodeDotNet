using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests;

public class BdecoderTests
{
    [Theory]
    [InlineData("i0e", 0)]
    [InlineData("i42e", 42)]
    [InlineData("i-42e", -42)]
    [InlineData("i123456789e", 123456789)]
    public async Task DecodeValidInteger(string input, long expected)
    {
        using var decoder = Bdecoder.FromString(input, Encoding.ASCII);

        var result = await decoder.DecodeAsync();

        var integer = Assert.IsType<Binteger>(result);
        Assert.Equal(expected, integer.Value);
    }

    [Theory]
    [InlineData("ie")]
    [InlineData("i01e")]
    [InlineData("i-0e")]
    [InlineData("i12xe")]
    public async Task DecodeInvalidIntegerThrows(string input)
    {
        using var decoder = Bdecoder.FromString(input, Encoding.ASCII);

        await Assert.ThrowsAsync<FormatException>(
            () => decoder.DecodeAsync());
    }

    [Theory]
    [InlineData("0:", "")]
    [InlineData("4:spam", "spam")]
    [InlineData("11:hello world", "hello world")]
    public async Task DecodeValidString(string input, string expected)
    {
        using var decoder = Bdecoder.FromString(input, Encoding.UTF8);

        var result = await decoder.DecodeAsync();

        var str = Assert.IsType<Bstring>(result);
        Assert.Equal(expected, Encoding.UTF8.GetString(str.Value));
    }

    [Theory]
    [InlineData("01:a")]
    [InlineData("-1:a")]
    [InlineData("3:ab")]
    public async Task DecodeInvalidStringThrows(string input)
    {
        using var decoder = Bdecoder.FromString(input, Encoding.UTF8);

        await Assert.ThrowsAsync<FormatException>(
            () => decoder.DecodeAsync());
    }

    [Fact]
    public async Task DecodeEmptyList()
    {
        using var decoder = Bdecoder.FromString("le", Encoding.UTF8);

        var result = await decoder.DecodeAsync();

        var list = Assert.IsType<Blist>(result);
        Assert.Empty(list);
    }

    [Fact]
    public async Task DecodeMixedList()
    {
        using var decoder = Bdecoder.FromString("l4:spami42ee", Encoding.UTF8);

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
        using var decoder = Bdecoder.FromString("de", Encoding.UTF8);

        var result = await decoder.DecodeAsync();

        var dict = Assert.IsType<Bdictionary>(result);
        Assert.Empty(dict);
    }

    [Fact]
    public async Task DecodeSimpleDictionary()
    {
        using var decoder =
            Bdecoder.FromString("d3:cow3:moo4:spam4:eggse", Encoding.UTF8);

        var result = await decoder.DecodeAsync();
        var dict = Assert.IsType<Bdictionary>(result);

        Assert.Equal(new Bstring(Encoding.UTF8.GetBytes("moo")), dict[new Bstring(Encoding.UTF8.GetBytes("cow"))]);
        Assert.Equal(new Bstring(Encoding.UTF8.GetBytes("eggs")), dict[new Bstring(Encoding.UTF8.GetBytes("spam"))]);
    }

    [Fact]
    public async Task DictionaryKeyMustBeString()
    {
        using var decoder = Bdecoder.FromString("di1e3:fooee", Encoding.UTF8);

        await Assert.ThrowsAsync<FormatException>(
            () => decoder.DecodeAsync());
    }

    [Fact]
    public async Task DictionaryKeysMustBeSorted()
    {
        // "spam" > "cow" -> invalid
        using var decoder =
            Bdecoder.FromString("d4:spam3:moo3:cow3:mooee", Encoding.UTF8);

        await Assert.ThrowsAsync<FormatException>(
            () => decoder.DecodeAsync());
    }

    [Fact]
    public async Task DictionaryMissingValueThrows()
    {
        using var decoder = Bdecoder.FromString("d3:fooee", Encoding.UTF8);

        await Assert.ThrowsAsync<FormatException>(
            () => decoder.DecodeAsync());
    }

    [Fact]
    public async Task DecodeWorksWithSmallBufferReads()
    {
        var bytes = Encoding.UTF8.GetBytes("l4:spami42ee");

        var stream = new MemoryStream(bytes, writable: false);
        using var decoder = new Bdecoder<MemoryStream>(ref stream);

        var result = await decoder.DecodeAsync();

        var list = Assert.IsType<Blist>(result);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task DecodeRespectsCancellation()
    {
        using var decoder = Bdecoder.FromString("l4:spami42ee", Encoding.ASCII);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => decoder.DecodeAsync(cts.Token));
    }
}
