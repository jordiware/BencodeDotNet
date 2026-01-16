using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Tests.Objects;

public class BintegerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void ConstructorStoresValue(long value)
    {
        var binteger = new Binteger(value);

        Assert.Equal(value, binteger.Value);
    }

    [Theory]
    [InlineData(0, "i0e")]
    [InlineData(42, "i42e")]
    [InlineData(-42, "i-42e")]
    [InlineData(long.MaxValue, "i9223372036854775807e")]
    [InlineData(long.MinValue, "i-9223372036854775808e")]
    public void ToStringReturnsValidBencode(long value, string expected)
    {
        var binteger = new Binteger(value);
        var result = binteger.ToString();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, new byte[] { 0x69, 0x30, 0x65 })]
    [InlineData(123, new byte[] { 0x69, 0x31, 0x32, 0x33, 0x65 })]
    [InlineData(-123, new byte[] { 0x69, 0x2d, 0x31, 0x32, 0x33, 0x65 })]
    public void ToBinaryEncodingMatchesBencode(long value, byte[] expected)
    {
        var binteger = new Binteger(value);
        var bytes = binteger.ToBinaryEncoding();

        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void EqualsReturnsTrueForSameValue()
    {
        var a = new Binteger(42);
        var b = new Binteger(42);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void EqualsReturnsFalseForDifferentValue()
    {
        var a = new Binteger(42);
        var b = new Binteger(43);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void EqualsReturnsFalseForNull()
    {
        var a = new Binteger(42);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void CompareToReturnsZeroForEqualValues()
    {
        var a = new Binteger(10);
        var b = new Binteger(10);

        Assert.Equal(0, a.CompareTo(b));
    }

    [Fact]
    public void CompareToReturnsPositiveWhenGreater()
    {
        var a = new Binteger(10);
        var b = new Binteger(5);

        Assert.True(a.CompareTo(b) > 0);
    }

    [Fact]
    public void CompareToReturnsNegativeWhenLess()
    {
        var a = new Binteger(5);
        var b = new Binteger(10);

        Assert.True(a.CompareTo(b) < 0);
    }

    [Fact]
    public void CompareToReturnsPositiveWhenOtherIsNull()
    {
        var a = new Binteger(10);

        Assert.True(a.CompareTo(null) > 0);
    }
}
