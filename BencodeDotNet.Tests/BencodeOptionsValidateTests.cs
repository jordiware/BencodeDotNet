using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests;

public class BencodeOptionsValidateTests
{
    [Fact]
    public void ValidateDoesNotThrowForValidPrimitive()
    {
        var options = new BencodeOptions(maxDepth: 4, maxContainerItems: 10, maxPayloadLength: 32);

        var node = new BInteger(42);

        options.Validate(node);
    }

    [Fact]
    public void ValidateDoesNotThrowWhenPayloadLengthEqualsMaximum()
    {
        var value = new BString("abcd", Encoding.UTF8); // "4:abcd" → length 6

        var options = new BencodeOptions(maxDepth: 1, maxContainerItems: 1, maxPayloadLength: value.GetEncodedLength());

        options.Validate(value);
    }

    [Fact]
    public void ValidateThrowsWhenPayloadLengthExceeded()
    {
        var value = new BString("abcd", Encoding.UTF8);

        var options = new BencodeOptions(maxDepth: 1, maxContainerItems: 1, maxPayloadLength: value.GetEncodedLength() - 1);

        Assert.Throws<BencodeValidationException>(() => options.Validate(value));
    }

    [Fact]
    public void ValidateThrowsWhenMaxDepthExceeded()
    {
        var node = new BList([
            new BList([
                new BList([
                    new BInteger(1)
                ])
            ])
        ]);

        var options = new BencodeOptions(maxDepth: 3, maxContainerItems: 10, maxPayloadLength: 64);

        Assert.Throws<BencodeValidationException>(() => options.Validate(node));
    }

    [Fact]
    public void ValidateDoesNotThrowWhenDepthEqualsMaximum()
    {
        var node = new BList([
            new BList([
                new BInteger(1)
            ])
        ]);

        var options = new BencodeOptions(maxDepth: 3, maxContainerItems: 10, maxPayloadLength: 64);

        options.Validate(node);
    }

    [Fact]
    public void ValidateThrowsWhenListItemCountExceeded()
    {
        var list = new BList([
            new BInteger(1),
            new BInteger(2),
            new BInteger(3)
        ]);

        var options = new BencodeOptions(maxDepth: 1, maxContainerItems: 2, maxPayloadLength: 64);

        Assert.Throws<BencodeValidationException>(() => options.Validate(list));
    }

    [Fact]
    public void ValidateThrowsWhenDictionaryEntryCountExceeded()
    {
        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString("a", Encoding.UTF8)] = new BInteger(1),
            [new BString("b", Encoding.UTF8)] = new BInteger(2)
        });

        var options = new BencodeOptions(maxDepth: 1, maxContainerItems: 1, maxPayloadLength: 64);

        Assert.Throws<BencodeValidationException>(() => options.Validate(dict));
    }

    [Fact]
    public void ValidateCountsDictionaryKeyLengthTowardsPayload()
    {
        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString("longkey", Encoding.UTF8)] = new BInteger(1)
        });

        var encodedLength =
            2 +
            new BString("longkey", Encoding.UTF8).GetEncodedLength() +
            new BInteger(1).GetEncodedLength();

        var options = new BencodeOptions(maxDepth: 1, maxContainerItems: 1, maxPayloadLength: encodedLength - 1);

        Assert.Throws<BencodeValidationException>(() => options.Validate(dict));
    }

    [Fact]
    public void ValidateThrowsWhenNestedPayloadExceedsMaximum()
    {
        var node = new BList([
            new BString("abcd", Encoding.UTF8),
            new BString("abcd", Encoding.UTF8)
        ]);

        var encodedLength =
            2 +
            new BString("abcd", Encoding.UTF8).GetEncodedLength() +
            new BString("abcd", Encoding.UTF8).GetEncodedLength();

        var options = new BencodeOptions(maxDepth: 2, maxContainerItems: 2, maxPayloadLength: encodedLength - 1);

        Assert.Throws<BencodeValidationException>(() => options.Validate(node));
    }

    [Fact]
    public void ValidateDoesNotThrowForComplexValidStructure()
    {
        var node = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString("numbers", Encoding.UTF8)] = new BList([
                new BInteger(1),
                new BInteger(2)
            ]),
            [new BString("value", Encoding.UTF8)] = new BString("ok", Encoding.UTF8)
        });

        var options = new BencodeOptions(maxDepth: 3, maxContainerItems: 4, maxPayloadLength: 128);

        options.Validate(node);
    }
}
