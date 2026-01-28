using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.IO;

public class ReaderWriterRoundTripTests
{
    [Fact]
    public async Task RoundTripIntegerPreservesValue()
    {
        var original = new BInteger(42);

        var result = await RoundTripAsync(original);

        var integer = Assert.IsType<BInteger>(result);
        Assert.Equal(original.Value, integer.Value);
    }

    [Fact]
    public async Task RoundTripStringPreservesValue()
    {
        var original = new BString(Encoding.ASCII.GetBytes("spam"));

        var result = await RoundTripAsync(original);

        var str = Assert.IsType<BString>(result);
        Assert.Equal(original.Value, str.Value);
    }

    [Fact]
    public async Task RoundTripListPreservesStructure()
    {
        var original = new BList(
        [
            new BInteger(1),
            new BString(Encoding.ASCII.GetBytes("eggs")),
            new BInteger(3)
        ]);

        var result = await RoundTripAsync(original);

        var list = Assert.IsType<BList>(result);
        Assert.Equal(3, list.Count);
        Assert.Equal(1, ((BInteger)list[0]).Value);
        Assert.Equal("eggs", Encoding.ASCII.GetString(((BString)list[1]).Value));
        Assert.Equal(3, ((BInteger)list[2]).Value);
    }

    [Fact]
    public async Task RoundTripDictionaryPreservesKeysAndValues()
    {
        var original = new BDictionary(new Dictionary<BString, IBObject>()
        {
            [new BString(Encoding.ASCII.GetBytes("a"))] = new BInteger(1),
            [new BString(Encoding.ASCII.GetBytes("b"))] = new BString(Encoding.ASCII.GetBytes("spam"))
        });

        var result = await RoundTripAsync(original);

        var dict = Assert.IsType<BDictionary>(result);

        Assert.Equal(2, dict.Count);
        Assert.Equal(1, ((BInteger)dict[new BString(Encoding.ASCII.GetBytes("a"))]).Value);
        Assert.Equal("spam", Encoding.ASCII.GetString(((BString)dict[new BString(Encoding.ASCII.GetBytes("b"))]).Value));
    }

    [Fact]
    public async Task MultipleConsecutiveRoundTripsRemainCorrect()
    {
        var writer = new BencodeWriter();
        var reader = new BencodeReader();

        using var stream = new MemoryStream();

        await writer.WriteBencodeAsync(new BInteger(1), stream);
        await writer.WriteBencodeAsync(new BInteger(2), stream);
        await writer.WriteBencodeAsync(new BInteger(3), stream);

        stream.Position = 0;

        var streamedArray = await reader.ReadMultipleAsync(stream).ToArrayAsync();

        var first = streamedArray[0];
        var second = streamedArray[1];
        var third = streamedArray[2];

        Assert.Equal(1, ((BInteger)first).Value);
        Assert.Equal(2, ((BInteger)second).Value);
        Assert.Equal(3, ((BInteger)third).Value);
    }

    [Fact]
    public async Task NestedStructuresRoundTripCorrectly()
    {
        var original = new BList(
        [
            new BDictionary(new Dictionary<BString, IBObject>()
            {
                [new BString(Encoding.ASCII.GetBytes("x"))] = new BInteger(9)
            }),
            new BList(
            [
                new BInteger(1),
                new BInteger(2)
            ])
        ]);

        var result = await RoundTripAsync(original);

        var list = Assert.IsType<BList>(result);

        var dict = Assert.IsType<BDictionary>(list[0]);
        Assert.Equal(9, ((BInteger)dict[new BString(Encoding.ASCII.GetBytes("x"))]).Value);

        var innerList = Assert.IsType<BList>(list[1]);
        Assert.Equal(1, ((BInteger)innerList[0]).Value);
        Assert.Equal(2, ((BInteger)innerList[1]).Value);
    }

    [Fact]
    public async Task WriterReaderWriterProducesStableEncoding()
    {
        var original = new BList(
        [
            new BInteger(1),
            new BString(Encoding.ASCII.GetBytes("spam"))
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

    private static async Task<IBObject?> RoundTripAsync(IBObject value)
    {
        var writer = new BencodeWriter();
        var reader = new BencodeReader();

        using var stream = new MemoryStream();

        await writer.WriteBencodeAsync(value, stream);
        stream.Position = 0;

        return await reader.ReadMultipleAsync(stream).FirstOrDefaultAsync();
    }
}
