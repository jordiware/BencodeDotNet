using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests;

public class BencodeOptionsValidateTests
{
    [Fact]
    public void ValidateDoesNotThrowForValidPrimitive()
    {
        var options = new BencodeOptions(maxDepth: 4, maxContainerItems: 10, maxPayloadLength: 32);

        var node = new Binteger(42);

        options.Validate(node);
    }

    [Fact]
    public void ValidateDoesNotThrowWhenPayloadLengthEqualsMaximum()
    {
        var value = new Bstring("abcd", Encoding.UTF8); // "4:abcd" → length 6

        var options = new BencodeOptions(maxDepth: 1, maxContainerItems: 1, maxPayloadLength: value.GetEncodedLength());

        options.Validate(value);
    }

    [Fact]
    public void ValidateThrowsWhenPayloadLengthExceeded()
    {
        var value = new Bstring("abcd", Encoding.UTF8);

        var options = new BencodeOptions(maxDepth: 1, maxContainerItems: 1, maxPayloadLength: value.GetEncodedLength() - 1);

        Assert.Throws<InvalidOperationException>(() => options.Validate(value));
    }

    [Fact]
    public void ValidateThrowsWhenMaxDepthExceeded()
    {
        var node = new Blist([
            new Blist([
                new Blist([
                    new Binteger(1)
                ])
            ])
        ]);

        var options = new BencodeOptions(maxDepth: 3, maxContainerItems: 10, maxPayloadLength: 64);

        Assert.Throws<InvalidOperationException>(() => options.Validate(node));
    }

    [Fact]
    public void ValidateDoesNotThrowWhenDepthEqualsMaximum()
    {
        var node = new Blist([
            new Blist([
                new Binteger(1)
            ])
        ]);

        var options = new BencodeOptions(maxDepth: 3, maxContainerItems: 10, maxPayloadLength: 64);

        options.Validate(node);
    }

    [Fact]
    public void ValidateThrowsWhenListItemCountExceeded()
    {
        var list = new Blist([
            new Binteger(1),
            new Binteger(2),
            new Binteger(3)
        ]);

        var options = new BencodeOptions(maxDepth: 1, maxContainerItems: 2, maxPayloadLength: 64);

        Assert.Throws<InvalidOperationException>(() => options.Validate(list));
    }

    [Fact]
    public void ValidateThrowsWhenDictionaryEntryCountExceeded()
    {
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring("a", Encoding.UTF8)] = new Binteger(1),
            [new Bstring("b", Encoding.UTF8)] = new Binteger(2)
        });

        var options = new BencodeOptions(maxDepth: 1, maxContainerItems: 1, maxPayloadLength: 64);

        Assert.Throws<InvalidOperationException>(() => options.Validate(dict));
    }

    [Fact]
    public void ValidateCountsDictionaryKeyLengthTowardsPayload()
    {
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring("longkey", Encoding.UTF8)] = new Binteger(1)
        });

        var encodedLength =
            2 +
            new Bstring("longkey", Encoding.UTF8).GetEncodedLength() +
            new Binteger(1).GetEncodedLength();

        var options = new BencodeOptions(maxDepth: 1, maxContainerItems: 1, maxPayloadLength: encodedLength - 1);

        Assert.Throws<InvalidOperationException>(() => options.Validate(dict));
    }

    [Fact]
    public void ValidateThrowsWhenNestedPayloadExceedsMaximum()
    {
        var node = new Blist([
            new Bstring("abcd", Encoding.UTF8),
            new Bstring("abcd", Encoding.UTF8)
        ]);

        var encodedLength =
            2 +
            new Bstring("abcd", Encoding.UTF8).GetEncodedLength() +
            new Bstring("abcd", Encoding.UTF8).GetEncodedLength();

        var options = new BencodeOptions(maxDepth: 2, maxContainerItems: 2, maxPayloadLength: encodedLength - 1);

        Assert.Throws<InvalidOperationException>(() => options.Validate(node));
    }

    [Fact]
    public void ValidateDoesNotThrowForComplexValidStructure()
    {
        var node = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring("numbers", Encoding.UTF8)] = new Blist([
                new Binteger(1),
                new Binteger(2)
            ]),
            [new Bstring("value", Encoding.UTF8)] = new Bstring("ok", Encoding.UTF8)
        });

        var options = new BencodeOptions(maxDepth: 3, maxContainerItems: 4, maxPayloadLength: 128);

        options.Validate(node);
    }
}
