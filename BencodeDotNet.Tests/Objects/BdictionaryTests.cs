using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.Objects;

public class BdictionaryTests
{
    [Fact]
    public void ConstructorCreatesDictionaryWithCorrectCount()
    {
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Binteger(1),
            [new Bstring([0x62])] = new Binteger(2),
        });

        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void IndexerReturnsValueForKey()
    {
        var key = new Bstring([0x61]);
        var value = new Binteger(1);

        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [key] = value
        });

        Assert.Same(value, dict[key]);
    }

    [Fact]
    public void ContainsKeyReturnsTrueForExistingKey()
    {
        var key = new Bstring([0x61]);

        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [key] = new Binteger(1)
        });

        Assert.True(dict.ContainsKey(key));
    }

    [Fact]
    public void ContainsKeyReturnsTrueForInlineKey()
    {
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Binteger(1)
        });

        Assert.True(dict.ContainsKey(new Bstring([0x61])));
    }

    [Fact]
    public void TryGetValueReturnsValueWhenKeyExists()
    {
        var key = new Bstring([0x61]);
        var value = new Binteger(1);

        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
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
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Binteger(1)
        });

        var found = dict.TryGetValue(new Bstring([0x61]), out var result);

        Assert.True(found);
        Assert.Equal(new Binteger(1), result);
    }

    [Fact]
    public void ConstructorTakesImmutableSnapshot()
    {
        var source = new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Binteger(1)
        };

        var dict = new Bdictionary(source);

        source[new Bstring([0x62])] = new Binteger(2);

        Assert.Single(dict);
    }

    [Fact]
    public void KeysAreSortedLexicographically()
    {
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x62])] = new Binteger(2), // "b"
            [new Bstring([0x61])] = new Binteger(1), // "a"
        });

        var keys = dict.Keys.ToArray();

        Assert.Equal(
            new[] {
                new Bstring([0x61]),
                new Bstring([0x62])
            },
            keys
        );
    }

    [Fact]
    public void ToStringEmptyDictionary()
    {
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>());

        Assert.Equal("de", dict.ToString());
    }

    [Fact]
    public void ToStringEncodesDictionaryInCanonicalOrder()
    {
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x62])] = new Binteger(2),
            [new Bstring([0x61])] = new Binteger(1),
        });

        Assert.Equal("d1:ai1e1:bi2ee", dict.ToString());
    }

    [Fact]
    public void ToBinaryEncodingMatchesToStringASCII()
    {
        var dict = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Binteger(1)
        });

        var bytes = dict.ToBinaryEncoding();
        var expected = Encoding.ASCII.GetBytes(dict.ToString());

        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void EqualsReturnsTrueForSameKeyValuePairsInDifferentOrder()
    {
        var a = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Binteger(1),
            [new Bstring([0x62])] = new Binteger(2),
        });

        var b = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x62])] = new Binteger(2),
            [new Bstring([0x61])] = new Binteger(1),
        });

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void EqualsReturnsTrueForNestedStructures()
    {
        var a = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Blist([
                new Binteger(1),
                new Binteger(2)
            ])
        });

        var b = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Blist([
                new Binteger(1),
                new Binteger(2)
            ])
        });

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void EqualDictionariesHaveSameHashCode()
    {
        var a = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Binteger(1)
        });

        var b = new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Binteger(1)
        });

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void BdictionaryCanBeUsedAsDictionaryKey()
    {
        var outer = new Dictionary<IBobject, string>();

        outer[new Bdictionary(new Dictionary<Bstring, IBobject>
        {
            [new Bstring([0x61])] = new Binteger(1)
        })] = "value";

        Assert.True(outer.ContainsKey(
            new Bdictionary(new Dictionary<Bstring, IBobject>
            {
                [new Bstring([0x61])] = new Binteger(1)
            })
        ));
    }
}
