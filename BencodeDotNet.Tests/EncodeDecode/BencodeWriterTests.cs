using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.EncodeDecode;

public class BencodeWriterTests
{
    [Theory]
    [InlineData(null)]
    public async Task WriteAsyncThrowsWhenBobjectIsNull(IBobject? value)
    {
        var writer = new BencodeWriter();
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            writer.WriteBencodeAsync(value!, stream));
    }

    [Fact]
    public async Task WriteAsyncThrowsWhenStreamIsNull()
    {
        var writer = new BencodeWriter();
        var value = new TestBobject("4:spam");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            writer.WriteAsync(value, null!));
    }

    [Fact]
    public async Task WriteAsyncThrowsWhenStreamIsNotWritable()
    {
        var writer = new BencodeWriter();
        var value = new TestBobject("4:spam");
        using var stream = new MemoryStream(Array.Empty<byte>(), writable: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            writer.WriteAsync(value, stream));
    }

    [Fact]
    public async Task WriteAsyncWritesRawBencodeToStream()
    {
        var writer = new BencodeWriter();
        var value = new TestBobject("4:spam");
        using var stream = new MemoryStream();

        await writer.WriteBencodeAsync(value, stream);

        var result = Encoding.ASCII.GetString(stream.ToArray());
        Assert.Equal("4:spam", result);
    }

    [Theory]
    [InlineData(null)]
    public async Task GenericWriteAsyncThrowsWhenValueIsNull(string? value)
    {
        var writer = new BencodeWriter();
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            writer.WriteAsync(value!, stream));
    }

    [Fact]
    public async Task GenericWriteAsyncThrowsWhenSerializerCannotBeResolved()
    {
        var writer = new BencodeWriter();
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            writer.WriteAsync(new object(), stream));
    }

    [Fact]
    public async Task GenericWriteAsyncUsesProvidedSerializer()
    {
        var writer = new BencodeWriter();
        using var stream = new MemoryStream();
        var serializer = new TestSerializer("i42e");

        await writer.WriteAsync(42, stream, serializer);

        var result = Encoding.ASCII.GetString(stream.ToArray());
        Assert.Equal("i42e", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WriteToFileAsyncThrowsWhenPathIsInvalid(string? path)
    {
        var writer = new BencodeWriter();
        var value = new TestBobject("4:spam");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            writer.WriteToFileAsync(value, path!));
    }

    [Fact]
    public async Task WriteToFileAsyncCreatesFileAndWritesData()
    {
        var writer = new BencodeWriter();
        var value = new TestBobject("4:spam");
        var path = Path.GetTempFileName();
        File.Delete(path);

        try
        {
            await writer.WriteBencodeToFileAsync(value, path);

            var result = await File.ReadAllTextAsync(path);
            Assert.Equal("4:spam", result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteToFileAsyncThrowsWhenFileExistsAndOverwriteIsFalse()
    {
        var writer = new BencodeWriter();
        var value = new TestBobject("4:spam");
        var path = Path.GetTempFileName();

        try
        {
            await Assert.ThrowsAsync<IOException>(() =>
                writer.WriteToFileAsync(value, path, overwrite: false));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GenericWriteToFileAsyncUsesProvidedSerializer()
    {
        var writer = new BencodeWriter();
        var serializer = new TestSerializer("i99e");
        var path = Path.GetTempFileName();
        File.Delete(path);

        try
        {
            await writer.WriteToFileAsync(99, path, overwrite: false, serializer);

            var result = await File.ReadAllTextAsync(path);
            Assert.Equal("i99e", result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #region Test types
    private sealed class TestBobject : IBobject
    {
        private readonly byte[] _data;

        public TestBobject(string value)
        {
            _data = Encoding.ASCII.GetBytes(value);
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
            await writer.WriteAsync(_data, cancellationToken);
        }
    }

    private sealed class TestSerializer : IBencodeSerializer
    {
        private readonly byte[] _data;

        public TestSerializer(string value)
        {
            _data = Encoding.ASCII.GetBytes(value);
        }

        public bool TryDeserialize(IBobject input, out object? output)
        {
            throw new NotImplementedException();
        }

        public bool TrySerialize(object input, out IBobject? output)
        {
            throw new NotImplementedException();
        }

        public async Task WriteToPipeAsync(object value, PipeWriter writer, CancellationToken cancellationToken = default)
        {
            await writer.WriteAsync(_data, cancellationToken);
        }
    }
    #endregion
}
