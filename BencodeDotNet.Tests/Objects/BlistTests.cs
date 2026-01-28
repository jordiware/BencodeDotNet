using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.Objects;

public class BListTests
{
    [Fact]
    public void ConstructorCreatesListWithCorrectCount()
    {
        var list = new BList([
            new BInteger(1),
            new BInteger(2),
            new BInteger(3)
        ]);

        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void ConstructorTakesImmutableSnapshot()
    {
        var source = new List<IBObject>
        {
            new BInteger(1)
        };

        var list = new BList(source);

        source.Add(new BInteger(2));

        Assert.Single(list);
    }

    [Fact]
    public void IndexerReturnsObjectsInOrder()
    {
        var a = new BInteger(1);
        var b = new BInteger(2);

        var list = new BList([a, b]);

        Assert.Same(a, list[0]);
        Assert.Same(b, list[1]);
    }

    [Fact]
    public void EnumeratesObjectsInOrder()
    {
        var objects = new IBObject[]
        {
            new BInteger(1),
            new BInteger(2),
            new BInteger(3)
        };

        var list = new BList(objects);

        Assert.True(Enumerable.SequenceEqual(objects, list.ToArray()));
    }

    [Fact]
    public void EqualsFalseWhenOtherIsNull()
    {
        var list = new BList([]);

        Assert.False(list.Equals(null));
    }

    [Fact]
    public void EqualsFalseWhenDifferent()
    {
        var a = new BList([]);
        var b = new BList([
            new BInteger(1),
        ]);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void EqualsFalseWhenDifferentOrder()
    {
        var a = new BList([
            new BInteger(2),
            new BInteger(1)
        ]);
        var b = new BList([
            new BInteger(1),
            new BInteger(2)
        ]);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void EqualsTrueWhenSameSource()
    {
        var objects = new IBObject[]
        {
            new BInteger(1),
            new BInteger(2),
            new BString([0x30, 0x31])
        };

        var a = new BList(objects);
        var b = new BList(objects);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void EqualsTrueWhenSameValues()
    {
        var a = new BList([
            new BInteger(1),
            new BString([0x30, 0x31]),
            new BList([
                new BInteger(2),
                new BInteger(3)
            ])
        ]);
        var b = new BList([
            new BInteger(1),
            new BString([0x30, 0x31]),
            new BList([
                new BInteger(2),
                new BInteger(3)
            ])
        ]);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void ToStringEmptyList()
    {
        var list = new BList([]);

        Assert.Equal("le", list.ToString());
    }

    [Fact]
    public void ToStringListOfIntegers()
    {
        var list = new BList([
            new BInteger(1),
            new BInteger(2),
            new BInteger(3)
        ]);

        Assert.Equal("li1ei2ei3ee", list.ToString());
    }

    [Fact]
    public void ToStringHeterogeneousList()
    {
        var list = new BList([
            new BInteger(1),
            new BList([
                new BInteger(2),
                new BInteger(3)
            ])
        ]);

        Assert.Equal("li1eli2ei3eee", list.ToString());
    }

    [Fact]
    public void ToBinaryEncodingEmptyList()
    {
        var list = new BList([]);

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
        var list = new BList([
            new BInteger(1),
            new BInteger(2)
        ]);

        var bytes = list.ToBinaryEncoding();
        var expected = Encoding.ASCII.GetBytes(list.ToString());

        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void EncodedLengthMatchesBinaryEncodingLengthForEmptyList()
    {
        var blist = new BList([]);

        var encoded = blist.ToBinaryEncoding();
        var length = blist.GetEncodedLength();

        Assert.Equal(encoded.Length, length);
    }

    [Fact]
    public void EncodedLengthMatchesBinaryEncodingLengthForNestedList()
    {
        var blist = new BList([
            new BInteger(1),
            new BString("test", Encoding.UTF8),
            new BList([
                new BInteger(2),
                new BString("nested", Encoding.UTF8)
            ])
        ]);

        var encoded = blist.ToBinaryEncoding();
        var length = blist.GetEncodedLength();

        Assert.Equal(encoded.Length, length);
    }
}
