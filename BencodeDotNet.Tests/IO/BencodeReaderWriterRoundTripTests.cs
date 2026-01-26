using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.IO;

public class BencodeReaderWriterRoundTripTests
{
    [Fact]
    public async Task RoundTripIntegerPreservesValue()
    {
        var original = new Binteger(42);

        var result = await RoundTripAsync(original);

        var integer = Assert.IsType<Binteger>(result);
        Assert.Equal(original.Value, integer.Value);
    }

    [Fact]
    public async Task RoundTripStringPreservesValue()
    {
        var original = new Bstring(Encoding.ASCII.GetBytes("spam"));

        var result = await RoundTripAsync(original);

        var str = Assert.IsType<Bstring>(result);
        Assert.Equal(original.Value, str.Value);
    }

    [Fact]
    public async Task RoundTripListPreservesStructure()
    {
        var original = new Blist(
        [
            new Binteger(1),
            new Bstring(Encoding.ASCII.GetBytes("eggs")),
            new Binteger(3)
        ]);

        var result = await RoundTripAsync(original);

        var list = Assert.IsType<Blist>(result);
        Assert.Equal(3, list.Count);
        Assert.Equal(1, ((Binteger)list[0]).Value);
        Assert.Equal("eggs", Encoding.ASCII.GetString(((Bstring)list[1]).Value));
        Assert.Equal(3, ((Binteger)list[2]).Value);
    }

    [Fact]
    public async Task RoundTripDictionaryPreservesKeysAndValues()
    {
        var original = new Bdictionary(new Dictionary<Bstring, IBobject>()
        {
            [new Bstring(Encoding.ASCII.GetBytes("a"))] = new Binteger(1),
            [new Bstring(Encoding.ASCII.GetBytes("b"))] = new Bstring(Encoding.ASCII.GetBytes("spam"))
        });

        var result = await RoundTripAsync(original);

        var dict = Assert.IsType<Bdictionary>(result);

        Assert.Equal(2, dict.Count);
        Assert.Equal(1, ((Binteger)dict[new Bstring(Encoding.ASCII.GetBytes("a"))]).Value);
        Assert.Equal("spam", Encoding.ASCII.GetString(((Bstring)dict[new Bstring(Encoding.ASCII.GetBytes("b"))]).Value));
    }

    [Fact]
    public async Task MultipleConsecutiveRoundTripsRemainCorrect()
    {
        var writer = new BencodeWriter();
        var reader = new BencodeReader();

        using var stream = new MemoryStream();

        await writer.WriteBencodeAsync(new Binteger(1), stream);
        await writer.WriteBencodeAsync(new Binteger(2), stream);
        await writer.WriteBencodeAsync(new Binteger(3), stream);

        stream.Position = 0;

        var streamedArray = await reader.ReadMultipleAsync(stream).ToArrayAsync();

        var first = streamedArray[0];
        var second = streamedArray[1];
        var third = streamedArray[2];

        Assert.Equal(1, ((Binteger)first).Value);
        Assert.Equal(2, ((Binteger)second).Value);
        Assert.Equal(3, ((Binteger)third).Value);
    }

    [Fact]
    public async Task NestedStructuresRoundTripCorrectly()
    {
        var original = new Blist(
        [
            new Bdictionary(new Dictionary<Bstring, IBobject>()
            {
                [new Bstring(Encoding.ASCII.GetBytes("x"))] = new Binteger(9)
            }),
            new Blist(
            [
                new Binteger(1),
                new Binteger(2)
            ])
        ]);

        var result = await RoundTripAsync(original);

        var list = Assert.IsType<Blist>(result);

        var dict = Assert.IsType<Bdictionary>(list[0]);
        Assert.Equal(9, ((Binteger)dict[new Bstring(Encoding.ASCII.GetBytes("x"))]).Value);

        var innerList = Assert.IsType<Blist>(list[1]);
        Assert.Equal(1, ((Binteger)innerList[0]).Value);
        Assert.Equal(2, ((Binteger)innerList[1]).Value);
    }

    [Fact]
    public async Task WriterReaderWriterProducesStableEncoding()
    {
        var original = new Blist(
        [
            new Binteger(1),
            new Bstring(Encoding.ASCII.GetBytes("spam"))
        ]);

        var writer = new BencodeWriter();
        var reader = new BencodeReader();

        using var stream1 = new MemoryStream();
        foreach (var bobject in original)
            await writer.WriteBencodeAsync(bobject, stream1);

        stream1.Position = 0;
        var decoded = await reader.ReadMultipleAsync(stream1).ToArrayAsync();

        using var stream2 = new MemoryStream();
        foreach (var bobject in decoded)
            await writer.WriteBencodeAsync(bobject, stream2);

        Assert.Equal(stream1.ToArray(), stream2.ToArray());
    }

    private static async Task<IBobject?> RoundTripAsync(IBobject value)
    {
        var writer = new BencodeWriter();
        var reader = new BencodeReader();

        using var stream = new MemoryStream();

        await writer.WriteBencodeAsync(value, stream);
        stream.Position = 0;

        return await reader.ReadMultipleAsync(stream).FirstOrDefaultAsync();
    }
}
