using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.IO;

public class DecoderStressTests
{
    private static readonly BencodeOptions options = new(textEncoding: Encoding.ASCII);
    private static readonly BencodeDecoder decoder = new(options);

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

        if (bencode.Length > options.MaxPayloadLength)
        {
            Assert.Throws<BencodeValidationException>(() => decoder.Decode(bencode));
        }
        else
        {
            var result = decoder.Decode(bencode);

            var str = Assert.IsType<BString>(result);
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

        if (count > options.MaxContainerItems)
        {
            Assert.Throws<BencodeValidationException>(() => decoder.Decode(sb.ToString()));
        }
        else
        {
            var result = decoder.Decode(sb.ToString());

            var list = Assert.IsType<BList>(result);
            Assert.Equal(count, list.Count);
            Assert.All(list, i => Assert.Equal(1, Assert.IsType<BInteger>(i).Value));
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

        if (depth >= options.MaxDepth)
        {
            Assert.Throws<BencodeValidationException>(() => decoder.Decode(sb.ToString()));
        }
        else
        {
            var result = decoder.Decode(sb.ToString());

            IBObject current = result;
            for (int i = 0; i < depth; i++)
            {
                var list = Assert.IsType<BList>(current);
                Assert.Single(list);
                current = list[0];
            }
        }
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    [InlineData(800)]
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

        if (depth >= options.MaxDepth)
        {
            Assert.Throws<BencodeValidationException>(() => decoder.Decode(sb.ToString()));
        }
        else
        {
            var result = decoder.Decode(sb.ToString());

            IBObject current = result;
            for (int i = 0; i < depth; i++)
            {
                var dict = Assert.IsType<BDictionary>(current);
                current = dict[new BString("a", Encoding.ASCII)];
            }
        }
    }


    [Fact]
    public async Task DecodeRandomValidObjects()
    {
        for (int i = 0; i < 10_000; i++)
        {
            var input = BencodeFuzzer.Generate();

            var result = decoder.Decode(input);

            Assert.NotNull(result);
        }
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
