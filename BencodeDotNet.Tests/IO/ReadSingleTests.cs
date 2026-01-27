using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.IO;

public class ReadSingleTests
{
    [Fact]
    public async Task ReadSingleAsyncGenericDeserializationFailsThrowsSerializerException()
    {
        var reader = new BencodeReader();
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("i1e"));
        var serializer = new FailingSerializer();

        await Assert.ThrowsAsync<BencodeSerializerException>(() => reader.ReadSingleAsync<int>(stream, serializer));
    }

    [Fact]
    public async Task ReadSingleFromFileAsyncValidFileReturnsObject()
    {
        var reader = new BencodeReader();
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "i7e");

        try
        {
            var result = await reader.ReadSingleFromFileAsync(path);
            var integer = Assert.IsType<Binteger>(result);
            Assert.Equal(7, integer.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadSingleFromFileAsyncInvalidPathThrowsArgumentException()
    {
        var reader = new BencodeReader();

        await Assert.ThrowsAsync<ArgumentException>(() => reader.ReadSingleFromFileAsync(" "));
    }

    [Fact]
    public async Task ReadSingleFromFileAsyncGenericValidFileReturnsValue()
    {
        var reader = new BencodeReader();
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "i123e");

        try
        {
            var value = await reader.ReadSingleFromFileAsync<long>(path);
            Assert.Equal(123L, value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadSingleAsyncChunkedStreamSingleByteChunksReturnsObject()
    {
        var reader = new BencodeReader();
        var data = Encoding.ASCII.GetBytes("i123456789e");
        using var stream = new ThrottledReadStream(data, 1);

        var result = await reader.ReadSingleAsync(stream);

        var integer = Assert.IsType<Binteger>(result);
        Assert.Equal(123456789, integer.Value);
    }

    [Fact]
    public async Task ReadSingleAsyncChunkedStreamRandomChunkSizesReturnsObject()
    {
        var reader = new BencodeReader();
        var data = Encoding.ASCII.GetBytes("l4:spami42ed3:foo3:baree");
        using var stream = new ThrottledReadStream(data, maxChunkSize: 3, randomize: true);

        var result = await reader.ReadSingleAsync(stream);

        Assert.IsType<Blist>(result);
    }

    [Fact]
    public async Task ReadSingleAsyncChunkedStreamLargeNestedObjectDoesNotStackOverflow()
    {
        var options = new BencodeOptions();
        var reader = new BencodeReader(options);

        var sb = new StringBuilder();
        sb.Append('l', 1000);
        sb.Append("i42e");
        sb.Append('e', 1000);

        using var stream = new ThrottledReadStream(Encoding.ASCII.GetBytes(sb.ToString()), 2);

        var result = await reader.ReadSingleAsync(stream);

        Assert.IsType<Blist>(result);
    }

    private sealed class FailingSerializer : IBencodeSerializer
    {
        public bool TryDeserialize(IBobject value, out object? result)
        {
            result = null;
            return false;
        }

        public bool TrySerialize(object value, out IBobject? result)
        {
            result = null;
            return false;
        }

        public Task WriteToPipeAsync(object input, PipeWriter writer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class ThrottledReadStream : Stream
    {
        private readonly byte[] _data;
        private readonly int _maxChunkSize;
        private readonly bool _randomize;
        private int _position;
        private readonly Random _random = new();

        public ThrottledReadStream(byte[] data, int maxChunkSize, bool randomize = false)
        {
            _data = data;
            _maxChunkSize = Math.Max(1, maxChunkSize);
            _randomize = randomize;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _data.Length;
        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _data.Length)
                return 0;

            var remaining = _data.Length - _position;
            //var chunk = _randomize ? _random.Next(1, _maxChunkSize + 1) : _maxChunkSize;
            var chunk = _maxChunkSize;
            var toRead = Math.Min(Math.Min(chunk, count), remaining);

            Array.Copy(_data, _position, buffer, offset, toRead);
            _position += toRead;
            return toRead;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_position >= _data.Length)
                return new ValueTask<int>(0);

            var remaining = _data.Length - _position;
            //var chunk = _randomize ? _random.Next(1, _maxChunkSize + 1) : _maxChunkSize;
            var chunk = _maxChunkSize;
            var toRead = Math.Min(Math.Min(chunk, buffer.Length), remaining);

            _data.AsSpan(_position, toRead).CopyTo(buffer.Span);
            _position += toRead;

            return new ValueTask<int>(toRead);
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
