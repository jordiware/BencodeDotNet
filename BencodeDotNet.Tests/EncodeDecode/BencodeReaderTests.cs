using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.EncodeDecode;

public class BencodeReaderTests
{
    private static readonly Random _random = new();

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

    [Fact]
    public async Task ReadAsyncRandomizedFuzzingStressTest()
    {
        var options = new BencodeOptions(maxDepth: 10, maxPayloadLength: 100);

        var reader = new BencodeReader(options);

        for (int test = 0; test < 50; test++) // 50 fuzz iterations
        {
            var bencode = GenerateRandomBencode(_random.Next(1, 500), 0, options.MaxDepth);
            var stream = CreateStream(bencode);

            try
            {
                await foreach (var _ in reader.ReadAsync(stream))
                {
                    // Just iterate to trigger parsing and validation
                }
            }
            catch (FormatException)
            {
                // Expected for malformed or invalid random streams
            }
            catch (InvalidOperationException)
            {
                // Expected for malformed or invalid random streams
            }
        }
    }

    [Fact]
    public async Task ReadAsyncStreamedSuperStressTest()
    {
        var options = new BencodeOptions();

        var reader = new BencodeReader(options);

        // Stream that generates ~10M Bencode objects on-the-fly
        using var stream = new InfiniteBencodeStream(10_000_000, options.MaxDepth);

        using var cts = new CancellationTokenSource();
        // Cancel after a short delay to simulate mid-enumeration interruption
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            cts.Cancel();
        });

        int count = 0;

        try
        {
            await foreach (var _ in reader.ReadAsync(stream, cts.Token))
            {
                count++;
            }
        }
        catch (TaskCanceledException)
        {
            // Expected when cancellation occurs
        }
        catch (FormatException)
        {
            // Possible due to deliberate random truncation
        }
        catch (InvalidOperationException)
        {
            // Expected for malformed or invalid random streams
        }

        Assert.True(count > 0, "At least some objects should have been parsed before cancellation or error.");
    }

    [Fact]
    public async Task ReadAsyncTortureModeStressTest()
    {
        var options = new BencodeOptions(maxDepth: 5);

        var reader = new BencodeReader(options);

        // Streaming generator simulating millions of objects with nesting and random truncation
        using var stream = new TortureBencodeStream(totalObjects: 1_00_000, maxDepth: options.MaxDepth);

        using var cts = new CancellationTokenSource();
        // Randomly cancel mid-stream to simulate real-world interruptions
        _ = Task.Run(async () =>
        {
            await Task.Delay(_random.Next(100, 1000) * 1000);
            cts.Cancel();
        });

        int parsedObjects = 0;

        try
        {
            await foreach (var _ in reader.ReadAsync(stream, cts.Token))
            {
                parsedObjects++;
            }
        }
        catch (TaskCanceledException)
        {
            // Expected: cancellation mid-enumeration
        }
        catch (FormatException)
        {
            // Expected: random truncation or malformed segments
        }
        catch (InvalidOperationException)
        {
            // Expected for malformed or invalid random streams
        }

        Assert.True(parsedObjects > 0, "At least some objects should have been parsed before cancellation or error.");
    }

    private static string GenerateRandomBencode(int elements, int depth, int maxDepth)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < elements; i++)
        {
            int choice = _random.Next(4);

            switch (choice)
            {
                case 0: // integer
                    sb.Append('i');
                    sb.Append(_random.Next(-10000, 10000));
                    sb.Append('e');
                    break;

                case 1: // string
                    int len = _random.Next(0, 50);
                    string str = RandomString(len);
                    sb.Append(len);
                    sb.Append(':');
                    sb.Append(str);
                    break;

                case 2: // list
                    if (depth < maxDepth)
                    {
                        sb.Append('l');
                        sb.Append(GenerateRandomBencode(_random.Next(0, 10), depth + 1, maxDepth));
                        sb.Append('e');
                    }
                    break;

                case 3: // dictionary
                    if (depth < maxDepth)
                    {
                        sb.Append('d');
                        int entries = _random.Next(0, 5);
                        for (int j = 0; j < entries; j++)
                        {
                            string key = RandomString(_random.Next(1, 10));
                            sb.Append(key.Length).Append(':').Append(key);
                            sb.Append(GenerateRandomBencode(1, depth + 1, maxDepth));
                        }
                        sb.Append('e');
                    }
                    break;
            }
        }

        // Occasionally truncate to simulate malformed streams
        if (_random.NextDouble() < 0.1 && sb.Length > 0)
            sb.Length -= _random.Next(0, Math.Min(5, sb.Length));

        return sb.ToString();
    }

    private static string GenerateHeavyRandomBencode(int depth, int maxDepth)
    {
        var sb = new StringBuilder();
        int elements = _random.Next(1, 20);

        for (int i = 0; i < elements; i++)
        {
            int choice = _random.Next(4);

            switch (choice)
            {
                case 0: // integer
                    sb.Append('i')
                      .Append(_random.Next(-1000000, 1000000))
                      .Append('e');
                    break;

                case 1: // string
                    int len = _random.Next(0, 150);
                    string str = RandomString(len);
                    sb.Append(len).Append(':').Append(str);
                    break;

                case 2: // list
                    if (depth < maxDepth)
                    {
                        sb.Append('l')
                          .Append(GenerateHeavyRandomBencode(depth + 1, maxDepth))
                          .Append('e');
                    }
                    break;

                case 3: // dictionary
                    if (depth < maxDepth)
                    {
                        sb.Append('d');
                        int entries = _random.Next(1, 5);
                        for (int j = 0; j < entries; j++)
                        {
                            string key = RandomString(_random.Next(1, 15));
                            sb.Append(key.Length).Append(':').Append(key);
                            sb.Append(GenerateHeavyRandomBencode(depth + 1, maxDepth));
                        }
                        sb.Append('e');
                    }
                    break;
            }
        }

        // Random truncation for malformed streams
        if (_random.NextDouble() < 0.15 && sb.Length > 0)
            sb.Length -= _random.Next(0, Math.Min(10, sb.Length));

        return sb.ToString();
    }

    private static string RandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(chars[_random.Next(chars.Length)]);
        return sb.ToString();
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

    private class InfiniteBencodeStream : Stream
    {
        private readonly int _maxObjects;
        private int _generated;
        private readonly int _maxDepth;
        private byte[] _buffer = Array.Empty<byte>();
        private int _position = 0;
        private static readonly Random _random = new();

        public InfiniteBencodeStream(int maxObjects, int maxDepth)
        {
            _maxObjects = maxObjects;
            _maxDepth = maxDepth;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _buffer.Length)
            {
                if (_generated >= _maxObjects)
                    return 0; // EOF

                _buffer = Encoding.ASCII.GetBytes(GenerateRandomBencode(0, _maxDepth));
                _position = 0;
                _generated++;
            }

            int toCopy = Math.Min(count, _buffer.Length - _position);
            Array.Copy(_buffer, _position, buffer, offset, toCopy);
            _position += toCopy;
            return toCopy;
        }

        private static string GenerateRandomBencode(int depth, int maxDepth)
        {
            var sb = new StringBuilder();
            int elements = _random.Next(1, 5);

            for (int i = 0; i < elements; i++)
            {
                int choice = _random.Next(3);
                switch (choice)
                {
                    case 0: // integer
                        sb.Append('i').Append(_random.Next(-1000, 1000)).Append('e');
                        break;
                    case 1: // string
                        string str = RandomString(_random.Next(1, 20));
                        sb.Append(str.Length).Append(':').Append(str);
                        break;
                    case 2: // list
                        if (depth < maxDepth)
                            sb.Append('l').Append(GenerateRandomBencode(depth + 1, maxDepth)).Append('e');
                        break;
                }
            }

            // Occasionally truncate to simulate malformed data
            if (_random.NextDouble() < 0.05 && sb.Length > 0)
                sb.Length -= _random.Next(0, Math.Min(5, sb.Length));

            return sb.ToString();
        }

        private static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(chars[_random.Next(chars.Length)]);
            return sb.ToString();
        }
    }

    private class TortureBencodeStream : Stream
    {
        private readonly int _totalObjects;
        private readonly int _maxDepth;
        private int _generated;
        private byte[] _buffer = Array.Empty<byte>();
        private int _position = 0;
        private static readonly Random _random = new();

        public TortureBencodeStream(int totalObjects, int maxDepth)
        {
            _totalObjects = totalObjects;
            _maxDepth = maxDepth;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _buffer.Length)
            {
                if (_generated >= _totalObjects)
                    return 0; // EOF

                var data = GenerateRandomBencode(0, _maxDepth);
                // Randomly truncate for malformed segments
                if (_random.NextDouble() < 0.01 && data.Length > 0)
                    data = data.Substring(0, data.Length - _random.Next(1, Math.Min(10, data.Length)));

                _buffer = Encoding.ASCII.GetBytes(data);
                _position = 0;
                _generated++;
            }

            int toCopy = Math.Min(count, _buffer.Length - _position);
            Array.Copy(_buffer, _position, buffer, offset, toCopy);
            _position += toCopy;
            return toCopy;
        }

        private static string GenerateRandomBencode(int depth, int maxDepth)
        {
            var sb = new StringBuilder();
            int elements = _random.Next(3, 10);

            for (int i = 0; i < elements; i++)
            {
                if (depth == maxDepth)
                {
                    int choice = _random.Next(2);
                    switch (choice)
                    {
                        case 0: // integer
                            sb.Append('i');
                            sb.Append(_random.Next(-1_000_000, 1_000_000));
                            sb.Append('e');
                            break;

                        case 1: // string
                            string str = RandomString(_random.Next(0, 100));
                            sb.Append(str.Length);
                            sb.Append(':');
                            sb.Append(str);
                            break;
                    }
                }
                else
                {
                    int choice = _random.Next(9);
                    switch (choice)
                    {
                        case 0: // integer
                        case 1:
                        case 2:
                            sb.Append('i');
                            sb.Append(_random.Next(-1_000_000, 1_000_000));
                            sb.Append('e');
                            break;

                        case 3: // string
                        case 4:
                        case 5:
                            string str = RandomString(_random.Next(0, 100));
                            sb.Append(str.Length);
                            sb.Append(':');
                            sb.Append(str);
                            break;

                        case 6: // list
                        case 7:
                        case 8:
                            sb.Append('l');
                            sb.Append(GenerateRandomBencode(depth + 1, maxDepth));
                            sb.Append('e');
                            break;

                        case 9: // dictionary
                            sb.Append('d');
                            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                            int dictEntries = _random.Next(1, 5);
                            for (int j = 0; j < dictEntries; j++)
                            {
                                string key = chars.Substring(j, 5);
                                sb.Append(key.Length).Append(':').Append(key);
                                sb.Append(GenerateRandomBencode(depth + 1, maxDepth));
                            }
                            sb.Append('e');
                            break;
                    }
                }
            }

            return sb.ToString();
        }

        private static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(chars[_random.Next(chars.Length)]);
            return sb.ToString();
        }
    }
}
