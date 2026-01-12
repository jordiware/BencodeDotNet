using Jordiware.BencodeDotNet.Objects;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests;

public class BstringTests
{
    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 0x30 })]
    [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 })]
    [InlineData(new byte[] { 0xf0, 0x9f, 0x99, 0x82 })]
    public void BytesConstructorStoresValue(byte[] value)
    {
        var bstring = new Bstring(value);

        Assert.Equal(value, bstring.Value);
    }

    [Theory]
    [InlineData("", 
                new byte[0])]
    [InlineData("0", 
                new byte[] { 0x30 })]
    [InlineData("Hello, world!", 
                new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 })]
    [InlineData("🙂", 
                new byte[] { 0xf0, 0x9f, 0x99, 0x82 })]
    public void Utf8StringConstructorStoresValue(string value, byte[] expected)
    {
        var bstring = new Bstring(value, Encoding.UTF8);

        Assert.Equal(expected, bstring.Value);
    }

    [Theory]
    [InlineData(new byte[0], 
                "0:")]
    [InlineData(new byte[] { 0x30 }, 
                "1:0")]
    [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 }, 
                "13:Hello, world!")]
    public void ToStringReturnsValidBencode(byte[] value, string expected)
    {
        var bstring = new Bstring(value);

        Assert.Equal(expected, bstring.ToString());
    }

    [Theory]
    [InlineData(new byte[0], 
                "0:")]
    [InlineData(new byte[] { 0x30 }, 
                "1:30")]
    [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 }, 
                "13:48-65-6C-6C-6F-2C-20-77-6F-72-6C-64-21")]
    [InlineData(new byte[] { 0xf0, 0x9f, 0x99, 0x82 }, 
                "4:F0-9F-99-82")]
    public void ToHexStringReturnsValidBencode(byte[] value, string expected)
    {
        var bstring = new Bstring(value);

        Assert.Equal(expected, bstring.ToHexString());
    }

    [Theory]
    [InlineData(new byte[0], 
                new byte[] { 0x30, 0x3a})]
    [InlineData(new byte[] { 0x30 }, 
                new byte[] { 0x31, 0x3a, 0x30 })]
    [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 },
                new byte[] { 0x31, 0x33, 0x3a, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 })]
    [InlineData(new byte[] { 0xf0, 0x9f, 0x99, 0x82 }, 
                new byte[] { 0x34, 0x3a, 0xf0, 0x9f, 0x99, 0x82 })]
    public void ToBinaryEncodingMatchesBencode(byte[] value, byte[] expected)
    {
        var bstring = new Bstring(value);

        Assert.Equal(expected, bstring.ToBinaryEncoding());
    }

    [Theory]
    [InlineData(new byte[0],
                new byte[0])]
    [InlineData(new byte[] { 0x30 },
                new byte[] { 0x30 })]
    [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 },
                new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 })]
    [InlineData(new byte[] { 0xf0, 0x9f, 0x99, 0x82 },
                new byte[] { 0xf0, 0x9f, 0x99, 0x82 })]
    public void EqualsReturnsTrueForSameValue(byte[] left, byte[] right)
    {
        var a = new Bstring(left);
        var b = new Bstring(right);

        Assert.True(a.Equals(b));
    }

    [Theory]
    [InlineData(new byte[0],
                new byte[] { 0x00 })]
    [InlineData(new byte[] { 0x30 },
                new byte[] { 0xf0, 0x9f, 0x98, 0x82 })]
    [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 },
                new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x76, 0x6f, 0x72, 0x6c, 0x64, 0x21 })]
    public void EqualsReturnsFalsForDifferentValue(byte[] left, byte[] right)
    {
        var a = new Bstring(left);
        var b = new Bstring(right);

        Assert.False(a.Equals(b));
    }

    [Theory]
    [InlineData(new byte[0],
                new byte[0])]
    [InlineData(new byte[] { 0x30 },
                new byte[] { 0x30 })]
    [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 },
                new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 })]
    [InlineData(new byte[] { 0xf0, 0x9f, 0x99, 0x82 },
                new byte[] { 0xf0, 0x9f, 0x99, 0x82 })]
    public void CompareToReturnsZeroForEqualValues(byte[] left, byte[] right)
    {
        var a = new Bstring(left);
        var b = new Bstring(right);

        Assert.True(a.CompareTo(b) == 0);
    }

    [Theory]
    [InlineData(new byte[] { 0x01 },
                new byte[0])]
    [InlineData(new byte[] { 0x31 },
                new byte[] { 0x30 })]
    [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 },
                new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64 })]
    [InlineData(new byte[] { 0x49, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64 },
                new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 })]
    public void CompareToReturnsPositiveForGreaterValues(byte[] left, byte[] right)
    {
        var a = new Bstring(left);
        var b = new Bstring(right);

        Assert.True(a.CompareTo(b) > 0);
    }

    [Theory]
    [InlineData(new byte[] { 0x01 },
                new byte[0])]
    [InlineData(new byte[] { 0x31 },
                new byte[] { 0x30 })]
    [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 },
                new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64 })]
    [InlineData(new byte[] { 0x49, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64 },
                new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x21 })]
    public void CompareToReturnsNegativeForLesserValues(byte[] left, byte[] right)
    {
        var a = new Bstring(left);
        var b = new Bstring(right);

        Assert.True(b.CompareTo(a) < 0);
    }

    [Fact]
    public void CompareToReturnsPositiveWhenOtherIsNull()
    {
        var a = new Bstring([]);

        Assert.True(a.CompareTo(null) > 0);
    }
}
