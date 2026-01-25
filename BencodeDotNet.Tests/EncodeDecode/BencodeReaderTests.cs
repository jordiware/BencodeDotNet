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
