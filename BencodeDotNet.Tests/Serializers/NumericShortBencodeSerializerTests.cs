using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class NumericShortBencodeSerializerTests
{
    private static readonly ShortBencodeSerializer ShortSerializer = new();
    private static readonly UshortBencodeSerializer UshortSerializer = new();

    #region Test ShortSerializer
    [Theory]
    [InlineData((short)-32768)]
    [InlineData((short)-1)]
    [InlineData((short)0)]
    [InlineData((short)1)]
    [InlineData((short)32767)]
    public void TrySerializeValidShortAlwaysSucceeds(short value)
    {
        var result = ShortSerializer.TrySerialize(value, out var binteger);

        Assert.True(result);
        Assert.NotNull(binteger);
        Assert.Equal(value, binteger.Value);
    }

    [Theory]
    [InlineData(-32768)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32767)]
    public void TryDeserializeValidShortRangeSucceeds(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = ShortSerializer.TryDeserialize(binteger, out var value);

        Assert.True(result);
        Assert.Equal((short)encodedValue, value);
    }

    [Theory]
    [InlineData(-32769)]
    [InlineData(-100000)]
    [InlineData(32768)]
    [InlineData(100000)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void TryDeserializeOutOfShortRangeFails(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = ShortSerializer.TryDeserialize(binteger, out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Theory]
    [InlineData((short)-32768)]
    [InlineData((short)-1)]
    [InlineData((short)0)]
    [InlineData((short)42)]
    [InlineData((short)32767)]
    public void RoundTripShortValueIsPreserved(short original)
    {
        Assert.True(ShortSerializer.TrySerialize(original, out var binteger));
        Assert.True(ShortSerializer.TryDeserialize(binteger, out var roundTripped));

        Assert.Equal(original, roundTripped);
    }
    #endregion

    #region Test UshortSerializer
    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1)]
    [InlineData((ushort)42)]
    [InlineData((ushort)65535)]
    public void TrySerializeValidUshortAlwaysSucceeds(ushort value)
    {
        var result = UshortSerializer.TrySerialize(value, out var binteger);

        Assert.True(result);
        Assert.NotNull(binteger);
        Assert.Equal(value, binteger.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(65535)]
    public void TryDeserializeValidUshortRangeSucceeds(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = UshortSerializer.TryDeserialize(binteger, out var value);

        Assert.True(result);
        Assert.Equal((ushort)encodedValue, value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-42)]
    [InlineData(65536)]
    [InlineData(100000)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void TryDeserializeOutOfUshortRangeFails(long encodedValue)
    {
        var binteger = new Binteger(encodedValue);

        var result = UshortSerializer.TryDeserialize(binteger, out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1)]
    [InlineData((ushort)42)]
    [InlineData((ushort)65535)]
    public void RoundTripUshortValueIsPreserved(ushort original)
    {
        Assert.True(UshortSerializer.TrySerialize(original, out var binteger));
        Assert.True(UshortSerializer.TryDeserialize(binteger, out var roundTripped));

        Assert.Equal(original, roundTripped);
    }
    #endregion
}
