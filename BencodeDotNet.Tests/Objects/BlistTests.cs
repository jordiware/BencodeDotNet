using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.Objects;

public class BlistTests
{
    [Fact]
    public void ConstructorCreatesListWithCorrectCount()
    {
        var list = new Blist([
            new Binteger(1),
            new Binteger(2),
            new Binteger(3)
        ]);

        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void ConstructorTakesImmutableSnapshot()
    {
        var source = new List<IBobject>
        {
            new Binteger(1)
        };

        var list = new Blist(source);

        source.Add(new Binteger(2));

        Assert.Single(list);
    }

    [Fact]
    public void IndexerReturnsObjectsInOrder()
    {
        var a = new Binteger(1);
        var b = new Binteger(2);

        var list = new Blist([a, b]);

        Assert.Same(a, list[0]);
        Assert.Same(b, list[1]);
    }

    [Fact]
    public void EnumeratesObjectsInOrder()
    {
        var objects = new IBobject[]
        {
            new Binteger(1),
            new Binteger(2),
            new Binteger(3)
        };

        var list = new Blist(objects);

        Assert.True(Enumerable.SequenceEqual(objects, list.ToArray()));
    }

    [Fact]
    public void EqualsFalseWhenOtherIsNull()
    {
        var list = new Blist([]);

        Assert.False(list.Equals(null));
    }

    [Fact]
    public void EqualsFalseWhenDifferent()
    {
        var a = new Blist([]);
        var b = new Blist([
            new Binteger(1),
        ]);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void EqualsFalseWhenDifferentOrder()
    {
        var a = new Blist([
            new Binteger(2),
            new Binteger(1)
        ]);
        var b = new Blist([
            new Binteger(1),
            new Binteger(2)
        ]);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void EqualsTrueWhenSameSource()
    {
        var objects = new IBobject[]
        {
            new Binteger(1),
            new Binteger(2),
            new Bstring([0x30, 0x31])
        };

        var a = new Blist(objects);
        var b = new Blist(objects);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void EqualsTrueWhenSameValues()
    {
        var a = new Blist([
            new Binteger(1),
            new Bstring([0x30, 0x31]),
            new Blist([
                new Binteger(2),
                new Binteger(3)
            ])
        ]);
        var b = new Blist([
            new Binteger(1),
            new Bstring([0x30, 0x31]),
            new Blist([
                new Binteger(2),
                new Binteger(3)
            ])
        ]);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void ToStringEmptyList()
    {
        var list = new Blist([]);

        Assert.Equal("le", list.ToString());
    }

    [Fact]
    public void ToStringListOfIntegers()
    {
        var list = new Blist([
            new Binteger(1),
            new Binteger(2),
            new Binteger(3)
        ]);

        Assert.Equal("li1ei2ei3ee", list.ToString());
    }

    [Fact]
    public void ToStringHeterogeneousList()
    {
        var list = new Blist([
            new Binteger(1),
            new Blist([
                new Binteger(2),
                new Binteger(3)
            ])
        ]);

        Assert.Equal("li1eli2ei3eee", list.ToString());
    }

    [Fact]
    public void ToBinaryEncodingEmptyList()
    {
        var list = new Blist([]);

        var bytes = list.ToBinaryEncoding();

        Assert.Equal(
            [
                Bencode.ListBeginCharacter,
                Bencode.TerminationCharacter
            ],
            bytes
        );
    }

    [Fact]
    public void ToBinaryEncodingMatchesToStringASCII()
    {
        var list = new Blist([
            new Binteger(1),
            new Binteger(2)
        ]);

        var bytes = list.ToBinaryEncoding();
        var expected = Encoding.ASCII.GetBytes(list.ToString());

        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void EncodedLengthMatchesBinaryEncodingLengthForEmptyList()
    {
        var blist = new Blist([]);

        var encoded = blist.ToBinaryEncoding();
        var length = blist.GetEncodedLength();

        Assert.Equal(encoded.Length, length);
    }

    [Fact]
    public void EncodedLengthMatchesBinaryEncodingLengthForNestedList()
    {
        var blist = new Blist([
            new Binteger(1),
            new Bstring("test", Encoding.UTF8),
            new Blist([
                new Binteger(2),
                new Bstring("nested", Encoding.UTF8)
            ])
        ]);

        var encoded = blist.ToBinaryEncoding();
        var length = blist.GetEncodedLength();

        Assert.Equal(encoded.Length, length);
    }
}
