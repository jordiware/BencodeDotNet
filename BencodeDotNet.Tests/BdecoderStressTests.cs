using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests;

public class BdecoderStressTests
{
    private static readonly BdecodingOptions options = new();

    [Theory]
    [InlineData(1_000_000)]
    [InlineData(2_000_000)]
    [InlineData(5_000_000)]
    [InlineData(10_000_000)]
    [InlineData(20_000_000)]
    [InlineData(50_000_000)]
    [InlineData(100_000_000)]
    public async Task DecodeHugeString(int size)
    {
        var bencode = $"{size}:{new string('a', size)}";

        using var decoder = Bdecoder.FromString(bencode, Encoding.ASCII, options);

        if (size > options.MaxStringLength)
        {
            await Assert.ThrowsAsync<FormatException>(() => decoder.DecodeAsync());
        }
        else
        {
            var result = await decoder.DecodeAsync();

            var str = Assert.IsType<Bstring>(result);
            Assert.Equal(size, str.Value.Length);
            Assert.All(str.Value, b => Assert.Equal((byte)'a', b));
        }
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    [InlineData(1_000)]
    [InlineData(2_000)]
    [InlineData(5_000)]
    [InlineData(10_000)]
    public async Task DecodeHugeListWithIntegers(int count)
    {
        var sb = new StringBuilder();
        sb.Append('l');
        for (int i = 0; i < count; i++)
            sb.Append("i1e");
        sb.Append('e');

        using var decoder = Bdecoder.FromString(sb.ToString(), Encoding.ASCII, options);

        if (count > options.MaxContainerItems)
        {
            await Assert.ThrowsAsync<FormatException>(() => decoder.DecodeAsync());
        }
        else
        {
            var result = await decoder.DecodeAsync();

            var list = Assert.IsType<Blist>(result);
            Assert.Equal(count, list.Count);
            Assert.All(list, i => Assert.Equal(1, Assert.IsType<Binteger>(i).Value));
        }
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    [InlineData(1_000)]
    [InlineData(2_000)]
    [InlineData(5_000)]
    [InlineData(10_000)]
    public async Task DecodeDeeplyNestedLists(int depth)
    {
        var sb = new StringBuilder(depth * 2);
        for (int i = 0; i <= depth; i++)
            sb.Append('l');
        for (int i = 0; i <= depth; i++)
            sb.Append('e');

        using var decoder = Bdecoder.FromString(sb.ToString(), Encoding.ASCII, options);

        if (depth >= options.MaxDepth)
        {
            await Assert.ThrowsAsync<FormatException>(() => decoder.DecodeAsync());
        }
        else
        {
            var result = await decoder.DecodeAsync();

            IBobject current = result;
            for (int i = 0; i < depth; i++)
            {
                var list = Assert.IsType<Blist>(current);
                Assert.Single(list);
                current = list[0];
            }
        }
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    [InlineData(850)]
    [InlineData(1_000)]
    [InlineData(2_000)]
    [InlineData(5_000)]
    [InlineData(10_000)]
    public async Task DecodeDeeplyNestedDictionaries(int depth)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < depth; i++)
            sb.Append("d1:a");
        sb.Append("i1e");
        for (int i = 0; i < depth; i++)
            sb.Append('e');

        using var decoder = Bdecoder.FromString(sb.ToString(), Encoding.ASCII, options);

        if (depth >= options.MaxDepth)
        {
            await Assert.ThrowsAsync<FormatException>(() => decoder.DecodeAsync());
        }
        else
        {
            var result = await decoder.DecodeAsync();

            IBobject current = result;
            for (int i = 0; i < depth; i++)
            {
                var dict = Assert.IsType<Bdictionary>(current);
                current = dict[new Bstring("a", Encoding.ASCII)];
            }
        }
    }

    [Fact]
    public async Task DecodeWithChunkedStream()
    {
        var data = Encoding.ASCII.GetBytes("d3:barl4:spami42ee3:fooi99ee");

        var stream = new ChunkedStream(data, 1);
        using var decoder = new Bdecoder<ChunkedStream>(ref stream, options);

        var result = await decoder.DecodeAsync();

        var dict = Assert.IsType<Bdictionary>(result);
        Assert.Equal(99, Assert.IsType<Binteger>(dict[new Bstring("foo", Encoding.ASCII)]).Value);
    }

    [Fact]
    public async Task DecodeRandomValidObjects()
    {
        for (int i = 0; i < 10_000; i++)
        {
            var input = BencodeFuzzer.Generate();

            using var decoder = Bdecoder.FromString(input, Encoding.ASCII, options);

            var result = await decoder.DecodeAsync();

            Assert.NotNull(result);
        }
    }
}

internal sealed class ChunkedStream : MemoryStream
{
    private readonly int _chunkSize;

    public ChunkedStream(byte[] buffer, int chunkSize) : base(buffer)
    {
        _chunkSize = chunkSize;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return base.Read(buffer, offset, Math.Min(count, _chunkSize));
    }
}

internal static class BencodeFuzzer
{
    private static readonly Random _rng = new(123);

    public static string Generate(int depth = 0)
    {
        if (depth > 5)
            return "i1e";

        var strLength = _rng.Next(0, 20);

        return _rng.Next(4) switch
        {
            0 => $"i{_rng.Next(-1000, 1000)}e",
            1 => $"{strLength}:{new string('x', strLength)}",
            2 => "l" + Generate(depth + 1) + "e",
            _ => "d1:a" + Generate(depth + 1) + "e"
        };
    }
}
