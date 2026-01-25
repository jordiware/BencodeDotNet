using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.EncodeDecode;

public class BencodeReaderTests
{
    private static Stream CreateStream(string content) =>
        new MemoryStream(Encoding.ASCII.GetBytes(content));

    [Fact]
    public async Task ReadAsyncNullStreamThrowsArgumentNullException()
    {
        var reader = new BencodeReader();
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in reader.ReadAsync(null!)) { }
        });
    }

    [Fact]
    public async Task ReadAsyncUnreadableStreamThrowsArgumentException()
    {
        var stream = new UnreadableStream();
        var reader = new BencodeReader();
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in reader.ReadAsync(stream)) { }
        });
    }

    [Theory]
    [InlineData("i42e", 42)]
    [InlineData("i0e", 0)]
    [InlineData("i-1e", -1)]
    public async Task ReadAsyncSingleIntegerReturnsCorrectValue(string bencode, int expected)
    {
        var reader = new BencodeReader();
        var stream = CreateStream(bencode);
        var results = new List<IBobject>();
        await foreach (var obj in reader.ReadAsync(stream))
            results.Add(obj);

        Assert.Single(results);
        Assert.IsType<Binteger>(results[0]);
        Assert.Equal(expected, ((Binteger)results[0]).Value);
    }

    [Fact]
    public async Task ReadAsyncMultipleObjectsReturnsAllObjects()
    {
        var bencode = "i1ei2ei3e";
        var reader = new BencodeReader();
        var stream = CreateStream(bencode);

        var results = new List<int>();
        await foreach (var obj in reader.ReadAsync(stream))
            results.Add((int)((Binteger)obj).Value);

        Assert.Equal(new[] { 1, 2, 3 }, results);
    }

    [Fact]
    public async Task ReadAsyncMalformedBencodeThrowsFormatException()
    {
        var stream = CreateStream("i123"); // missing 'e'
        var reader = new BencodeReader();
        await Assert.ThrowsAsync<FormatException>(async () =>
        {
            await foreach (var _ in reader.ReadAsync(stream)) { }
        });
    }

    [Fact]
    public async Task ReadAsyncValidationFailsThrowsFormatException()
    {
        var options = new BencodeOptions(maxPayloadLength: 1);
        var reader = new BencodeReader(options);
        var stream = CreateStream("5:Hello"); // string length exceeds MaxStringLength
        await Assert.ThrowsAsync<FormatException>(async () =>
        {
            await foreach (var _ in reader.ReadAsync(stream)) { }
        });
    }

    [Fact]
    public async Task ReadAsyncStreamEndsUnexpectedlyThrowsFormatException()
    {
        var stream = new MemoryStream(new byte[] { (byte)'l', (byte)'i', (byte)'1' }); // incomplete list
        var reader = new BencodeReader();
        await Assert.ThrowsAsync<FormatException>(async () =>
        {
            await foreach (var _ in reader.ReadAsync(stream)) { }
        });
    }

    [Fact]
    public async Task ReadAsyncTopLevelListReturnsCorrectBobject()
    {
        var bencode = "li1ei2ei3ee";
        var stream = CreateStream(bencode);
        var reader = new BencodeReader();

        var result = (Blist)(await reader.ReadAsync(stream).FirstAsync());
        var values = result.OfType<Binteger>().Select(b => (int)b.Value).ToArray();
        Assert.Equal(new[] { 1, 2, 3 }, values);
    }

    [Fact]
    public async Task ReadAsyncTopLevelDictionaryReturnsCorrectBobject()
    {
        var bencode = "d3:onei1e3:twoi2ee";
        var stream = CreateStream(bencode);
        var reader = new BencodeReader();

        var dict = (Bdictionary)(await reader.ReadAsync(stream).FirstAsync());
        Assert.Equal(2, dict.Count);
        Assert.Equal(1, ((Binteger)dict[new Bstring("one", Encoding.UTF8)]).Value);
        Assert.Equal(2, ((Binteger)dict[new Bstring("two", Encoding.UTF8)]).Value);
    }

    [Fact]
    public async Task ReadAsyncNestedStructuresReturnsCorrectBobjects()
    {
        var bencode = "d4:dictd1:ai10ee4:listli1ei2ei3eee";
        var stream = CreateStream(bencode);
        var reader = new BencodeReader();

        var dict = (Bdictionary)(await reader.ReadAsync(stream).FirstAsync());
        var list = (Blist)dict[new Bstring("list", Encoding.UTF8)];
        var nestedDict = (Bdictionary)dict[new Bstring("dict", Encoding.UTF8)];

        Assert.Equal(new[] { 1, 2, 3 }, list.OfType<Binteger>().Select(b => (int)b.Value));
        Assert.Equal(10, ((Binteger)nestedDict[new Bstring("a", Encoding.UTF8)]).Value);
    }

    [Fact]
    public async Task ReadAsyncWithCustomSerializerDeserializesCorrectly()
    {
        var bencode = "i42e";
        var stream = CreateStream(bencode);
        var reader = new BencodeReader();

        var serializer = new IntBencodeSerializer();
        var result = await reader.ReadAsync<int>(stream, serializer: serializer).FirstAsync();

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ReadAsyncWithoutSerializerResolvesFromRegistry()
    {
        var bencode = "i42e";
        var stream = CreateStream(bencode);
        var reader = new BencodeReader();

        // Assuming a registry serializer exists for int
        var result = await reader.ReadAsync<int>(stream).FirstAsync();
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ReadAsyncLargeStreamStressTest()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 10000; i++)
            sb.Append($"i{i}e");
        var stream = CreateStream(sb.ToString());
        var reader = new BencodeReader();

        int sum = 0;
        await foreach (var obj in reader.ReadAsync(stream))
            sum += (int)((Binteger)obj).Value;

        int expected = Enumerable.Range(0, 10000).Sum();
        Assert.Equal(expected, sum);
    }

    [Fact]
    public async Task ReadAsyncCancellationTokenCancelsEnumeration()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 10000; i++)
            sb.Append($"i{i}e");

        var stream = CreateStream(sb.ToString());
        var reader = new BencodeReader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await foreach (var _ in reader.ReadAsync(stream, cts.Token)) { }
        });
    }

    [Fact]
    public async Task ReadAsyncHeavyPayloadStressTest()
    {
        var sb = new StringBuilder();
        // 1000 lists each with 1000 integers
        for (int i = 0; i < 1000; i++)
        {
            sb.Append("l");
            for (int j = 0; j < 1000; j++)
                sb.Append($"i{j}e");
            sb.Append("e");
        }

        var stream = CreateStream(sb.ToString());
        var reader = new BencodeReader();

        int totalCount = 0;
        await foreach (var obj in reader.ReadAsync(stream))
        {
            var list = (Blist)obj;
            totalCount += list.Count;
        }

        Assert.Equal(1000 * 1000, totalCount);
    }

    // Helper classes
    private class UnreadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
