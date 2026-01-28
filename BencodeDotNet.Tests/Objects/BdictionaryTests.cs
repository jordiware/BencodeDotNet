using Jordiware.BencodeDotNet.Objects;
using System.Collections.Immutable;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.Objects;

public class BDictionaryTests
{
    [Fact]
    public void ConstructorCreatesDictionaryWithCorrectCount()
    {
        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BInteger(1),
            [new BString([0x62])] = new BInteger(2),
        });

        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void IndexerReturnsValueForKey()
    {
        var key = new BString([0x61]);
        var value = new BInteger(1);

        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [key] = value
        });

        Assert.Same(value, dict[key]);
    }

    [Fact]
    public void ContainsKeyReturnsTrueForExistingKey()
    {
        var key = new BString([0x61]);

        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [key] = new BInteger(1)
        });

        Assert.True(dict.ContainsKey(key));
    }

    [Fact]
    public void ContainsKeyReturnsTrueForInlineKey()
    {
        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BInteger(1)
        });

        Assert.True(dict.ContainsKey(new BString([0x61])));
    }

    [Fact]
    public void TryGetValueReturnsValueWhenKeyExists()
    {
        var key = new BString([0x61]);
        var value = new BInteger(1);

        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [key] = value
        });

        var found = dict.TryGetValue(key, out var result);

        Assert.True(found);
        Assert.Same(value, result);
    }

    [Fact]
    public void TryGetValueReturnsValueWhenInlineKeyAndValueExist()
    {
        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BInteger(1)
        });

        var found = dict.TryGetValue(new BString([0x61]), out var result);

        Assert.True(found);
        Assert.Equal(new BInteger(1), result);
    }

    [Fact]
    public void ConstructorTakesImmutableSnapshot()
    {
        var source = new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BInteger(1)
        };

        var dict = new BDictionary(source);

        source[new BString([0x62])] = new BInteger(2);

        Assert.Single(dict);
    }

    [Fact]
    public void KeysAreSortedLexicographically()
    {
        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x62])] = new BInteger(2), // "b"
            [new BString([0x61])] = new BInteger(1), // "a"
        });

        var keys = dict.Keys.ToArray();

        Assert.Equal(
            new[] {
                new BString([0x61]),
                new BString([0x62])
            },
            keys
        );
    }

    [Fact]
    public void ToStringEmptyDictionary()
    {
        var dict = new BDictionary(new Dictionary<BString, IBObject>());

        Assert.Equal("de", dict.ToString());
    }

    [Fact]
    public void ToStringEncodesDictionaryInCanonicalOrder()
    {
        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x62])] = new BInteger(2),
            [new BString([0x61])] = new BInteger(1),
        });

        Assert.Equal("d1:ai1e1:bi2ee", dict.ToString());
    }

    [Fact]
    public void ToBinaryEncodingMatchesToStringASCII()
    {
        var dict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BInteger(1)
        });

        var bytes = dict.ToBinaryEncoding();
        var expected = Encoding.ASCII.GetBytes(dict.ToString());

        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void EqualsReturnsTrueForSameKeyValuePairsInDifferentOrder()
    {
        var a = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BInteger(1),
            [new BString([0x62])] = new BInteger(2),
        });

        var b = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x62])] = new BInteger(2),
            [new BString([0x61])] = new BInteger(1),
        });

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void EqualsReturnsTrueForNestedStructures()
    {
        var a = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BList([
                new BInteger(1),
                new BInteger(2)
            ])
        });

        var b = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BList([
                new BInteger(1),
                new BInteger(2)
            ])
        });

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void EqualDictionariesHaveSameHashCode()
    {
        var a = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BInteger(1)
        });

        var b = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BInteger(1)
        });

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void BDictionaryCanBeUsedAsDictionaryKey()
    {
        var outer = new Dictionary<IBObject, string>();

        outer[new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString([0x61])] = new BInteger(1)
        })] = "value";

        Assert.True(outer.ContainsKey(
            new BDictionary(new Dictionary<BString, IBObject>
            {
                [new BString([0x61])] = new BInteger(1)
            })
        ));
    }

    [Fact]
    public void EncodedLengthMatchesBinaryEncodingLengthForEmptyDictionary()
    {
        var bdict = new BDictionary(ImmutableDictionary<BString, IBObject>.Empty);

        var encoded = bdict.ToBinaryEncoding();
        var length = bdict.GetEncodedLength();

        Assert.Equal(encoded.Length, length);
    }

    [Fact]
    public void EncodedLengthMatchesBinaryEncodingLengthForDictionaryWithValues()
    {
        var bdict = new BDictionary(new Dictionary<BString, IBObject>
        {
            [new BString("key", Encoding.UTF8)] = new BInteger(123),
            [new BString("value", Encoding.UTF8)] = new BString("hello", Encoding.UTF8),
            [new BString("nested", Encoding.UTF8)] = new BList(
            [
                new BInteger(1),
                new BInteger(2)
            ])
        });

        var encoded = bdict.ToBinaryEncoding();
        var length = bdict.GetEncodedLength();

        Assert.Equal(encoded.Length, length);
    }
}
