using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class EnumBencodeSerializerTests
{
    public enum SimpleEnum
    {
        Zero = 0,
        One = 1,
        Two = 2
    }

    [Flags]
    public enum FlagsEnum
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }

    [Flags]
    public enum ByteFlagsEnum : byte
    {
        A = 1,
        B = 2,
        C = 4
    }

    [Theory]
    [InlineData(SimpleEnum.Zero, 0)]
    [InlineData(SimpleEnum.One, 1)]
    [InlineData(SimpleEnum.Two, 2)]
    public void SerializeNonFlagsEnumProducesExpectedBInteger(SimpleEnum input, long expected)
    {
        var serializer = new EnumBencodeSerializer<SimpleEnum>();

        var result = serializer.TrySerialize(input, out var bencode);

        Assert.True(result);
        Assert.NotNull(bencode);
        Assert.Equal(expected, bencode.Value);
    }

    [Theory]
    [InlineData(0, SimpleEnum.Zero)]
    [InlineData(1, SimpleEnum.One)]
    [InlineData(2, SimpleEnum.Two)]
    public void DeserializeNonFlagsEnumWithDefinedValueSucceeds(long input, SimpleEnum expected)
    {
        var serializer = new EnumBencodeSerializer<SimpleEnum>();
        var bencode = new BInteger(input);

        var result = serializer.TryDeserialize(bencode, out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(42)]
    public void DeserializeNonFlagsEnumWithUndefinedValueFails(long input)
    {
        var serializer = new EnumBencodeSerializer<SimpleEnum>();
        var bencode = new BInteger(input);

        var result = serializer.TryDeserialize(bencode, out _);

        Assert.False(result);
    }

    [Theory]
    [InlineData(SimpleEnum.Zero)]
    [InlineData(SimpleEnum.One)]
    [InlineData(SimpleEnum.Two)]
    public void NonFlagsEnumRoundTripPreservesValue(SimpleEnum input)
    {
        var serializer = new EnumBencodeSerializer<SimpleEnum>();

        var serializeResult = serializer.TrySerialize(input, out var bencode);
        var deserializeResult = serializer.TryDeserialize(bencode!, out var value);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(input, value);
    }

    [Theory]
    [InlineData(FlagsEnum.Read, 1)]
    [InlineData(FlagsEnum.Write, 2)]
    [InlineData(FlagsEnum.Execute, 4)]
    [InlineData(FlagsEnum.Read | FlagsEnum.Write, 3)]
    [InlineData(FlagsEnum.Read | FlagsEnum.Execute, 5)]
    public void SerializeFlagsEnumProducesExpectedBInteger(FlagsEnum input, long expected)
    {
        var serializer = new EnumBencodeSerializer<FlagsEnum>();

        var result = serializer.TrySerialize(input, out var bencode);

        Assert.True(result);
        Assert.NotNull(bencode);
        Assert.Equal(expected, bencode.Value);
    }

    [Theory]
    [InlineData(0, FlagsEnum.None)]
    [InlineData(1, FlagsEnum.Read)]
    [InlineData(3, FlagsEnum.Read | FlagsEnum.Write)]
    [InlineData(7, FlagsEnum.Read | FlagsEnum.Write | FlagsEnum.Execute)]
    public void DeserializeFlagsEnumWithCompositeValueSucceeds(long input, FlagsEnum expected)
    {
        var serializer = new EnumBencodeSerializer<FlagsEnum>();
        var bencode = new BInteger(input);

        var result = serializer.TryDeserialize(bencode, out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(FlagsEnum.None)]
    [InlineData(FlagsEnum.Read)]
    [InlineData(FlagsEnum.Read | FlagsEnum.Write)]
    [InlineData(FlagsEnum.Read | FlagsEnum.Write | FlagsEnum.Execute)]
    public void FlagsEnumRoundTripPreservesValue(FlagsEnum input)
    {
        var serializer = new EnumBencodeSerializer<FlagsEnum>();

        var serializeResult = serializer.TrySerialize(input, out var bencode);
        var deserializeResult = serializer.TryDeserialize(bencode!, out var value);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(input, value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    [InlineData(1000)]
    public void DeserializeFlagsEnumWithOutOfRangeValueFails(long input)
    {
        var serializer = new EnumBencodeSerializer<ByteFlagsEnum>();
        var bencode = new BInteger(input);

        var result = serializer.TryDeserialize(bencode, out _);

        Assert.False(result);
    }

    [Theory]
    [InlineData(ByteFlagsEnum.A)]
    [InlineData(ByteFlagsEnum.B)]
    [InlineData(ByteFlagsEnum.A | ByteFlagsEnum.C)]
    public void FlagsEnumWithByteUnderlyingTypeRoundTripPreservesValue(ByteFlagsEnum input)
    {
        var serializer = new EnumBencodeSerializer<ByteFlagsEnum>();

        var serializeResult = serializer.TrySerialize(input, out var bencode);
        var deserializeResult = serializer.TryDeserialize(bencode!, out var value);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.Equal(input, value);
    }
}
