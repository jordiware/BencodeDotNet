using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.IO;

public class WriterStressTests
{
    [Theory]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    [InlineData(1_000_000)]
    public async Task WriteAsyncHandlesLargeBencodePayload(int size)
    {
        var writer = new BencodeWriter();
        var payload = new byte[size];
        Array.Fill(payload, (byte)'x');
        var value = new TestBobject(payload);

        using var stream = new MemoryStream();

        await writer.WriteBencodeAsync(value, stream);

        Assert.Equal(size, stream.Length);
    }

    [Fact]
    public async Task WriteAsyncSupportsMultipleConsecutiveWrites()
    {
        var writer = new BencodeWriter();

        using var stream = new MemoryStream();

        await writer.WriteBencodeAsync(new TestBobject(Encoding.ASCII.GetBytes("4:spam")), stream);
        await writer.WriteBencodeAsync(new TestBobject(Encoding.ASCII.GetBytes("4:eggs")), stream);
        await writer.WriteBencodeAsync(new TestBobject(Encoding.ASCII.GetBytes("i42e")), stream);

        var result = Encoding.ASCII.GetString(stream.ToArray());
        Assert.Equal("4:spam4:eggsi42e", result);
    }

    [Fact]
    public async Task GenericWriteAsyncSupportsMultipleConsecutiveWrites()
    {
        var writer = new BencodeWriter();
        var serializerA = new TestSerializer(Encoding.ASCII.GetBytes("i1e"));
        var serializerB = new TestSerializer(Encoding.ASCII.GetBytes("i2e"));

        using var stream = new MemoryStream();

        await writer.WriteAsync(1, stream, serializerA);
        await writer.WriteAsync(2, stream, serializerB);

        var result = Encoding.ASCII.GetString(stream.ToArray());
        Assert.Equal("i1ei2e", result);
    }

    [Fact]
    public async Task WriteAsyncPropagatesCancellationDuringWrite()
    {
        var writer = new BencodeWriter();
        var value = new TestBobject(Encoding.ASCII.GetBytes("4:spam"), delayMs: 100);

        using var stream = new MemoryStream();
        using var cts = new CancellationTokenSource();

        cts.CancelAfter(10);

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            writer.WriteBencodeAsync(value, stream, cts.Token));
    }

    [Fact]
    public async Task GenericWriteAsyncPropagatesCancellationDuringWrite()
    {
        var writer = new BencodeWriter();
        var serializer = new TestSerializer(Encoding.ASCII.GetBytes("i99e"), delayMs: 100);

        using var stream = new MemoryStream();
        using var cts = new CancellationTokenSource();

        cts.CancelAfter(10);

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            writer.WriteAsync(99, stream, serializer, cts.Token));
    }

    [Fact]
    public async Task CancellationDoesNotWritePartialData()
    {
        var writer = new BencodeWriter();
        var payload = Encoding.ASCII.GetBytes("4:spam");
        var value = new TestBobject(payload, delayMs: 100);

        using var stream = new MemoryStream();
        using var cts = new CancellationTokenSource();

        cts.CancelAfter(10);

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            writer.WriteBencodeAsync(value, stream, cts.Token));

        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public async Task StressMultipleSequentialWritesRemainCorrect()
    {
        var writer = new BencodeWriter();
        using var stream = new MemoryStream();

        for (var i = 0; i < 1_000; i++)
        {
            var value = new TestBobject(Encoding.ASCII.GetBytes("i1e"));
            await writer.WriteBencodeAsync(value, stream);
        }

        var result = Encoding.ASCII.GetString(stream.ToArray());
        Assert.Equal(new string('i', 1_000).Replace("i", "i1e"), result);
    }

    [Fact]
    public async Task StressGenericWritesWithSerializerRemainCorrect()
    {
        var writer = new BencodeWriter();
        using var stream = new MemoryStream();
        var serializer = new TestSerializer(Encoding.ASCII.GetBytes("i7e"));

        for (var i = 0; i < 1_000; i++)
            await writer.WriteAsync(7, stream, serializer);

        var result = Encoding.ASCII.GetString(stream.ToArray());
        Assert.Equal(new string('i', 1_000).Replace("i", "i7e"), result);
    }

    #region Test types
    private sealed class TestBobject : IBObject
    {
        private readonly byte[] _data;
        private readonly int _delayMs;

        public TestBobject(byte[] data, int delayMs = 0)
        {
            _data = data;
            _delayMs = delayMs;
        }

        public int GetEncodedLength()
        {
            throw new NotImplementedException();
        }

        public byte[] ToBinaryEncoding()
        {
            throw new NotImplementedException();
        }

        public async Task WriteToPipeAsync(PipeWriter writer, CancellationToken cancellationToken = default)
        {
            if (_delayMs > 0)
                await Task.Delay(_delayMs, cancellationToken);

            await writer.WriteAsync(_data, cancellationToken);
        }
    }

    private sealed class TestSerializer : IBencodeSerializer
    {
        private readonly byte[] _data;
        private readonly int _delayMs;

        public TestSerializer(byte[] data, int delayMs = 0)
        {
            _data = data;
            _delayMs = delayMs;
        }

        public bool TryDeserialize(IBObject input, out object? output)
        {
            throw new NotImplementedException();
        }

        public bool TrySerialize(object input, out IBObject? output)
        {
            throw new NotImplementedException();
        }

        public async Task WriteToPipeAsync(object value, PipeWriter writer, CancellationToken cancellationToken = default)
        {
            if (_delayMs > 0)
                await Task.Delay(_delayMs, cancellationToken);

            await writer.WriteAsync(_data, cancellationToken);
        }
    }
    #endregion
}
