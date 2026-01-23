using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public sealed class BencodeSerializerRegistryTests
{
    private static readonly BencodeOptions options = new();

    public static TheoryData<Type> AllRegisteredSerializerTypes =>
        [.. BencodeSerializer.TypeSerializers.Values];

    [Theory]
    [InlineData(typeof(bool), typeof(BoolBencodeSerializer))]
    [InlineData(typeof(byte), typeof(NumericByteBencodeSerializer))]
    [InlineData(typeof(sbyte), typeof(SbyteBencodeSerializer))]
    [InlineData(typeof(short), typeof(ShortBencodeSerializer))]
    [InlineData(typeof(ushort), typeof(UshortBencodeSerializer))]
    [InlineData(typeof(int), typeof(IntBencodeSerializer))]
    [InlineData(typeof(uint), typeof(UintBencodeSerializer))]
    [InlineData(typeof(long), typeof(LongBencodeSerializer))]
    [InlineData(typeof(ulong), typeof(UlongBencodeSerializer))]
    [InlineData(typeof(char), typeof(CharBencodeSerializer))]
    [InlineData(typeof(float), typeof(FloatBencodeSerializer))]
    [InlineData(typeof(double), typeof(DoubleBencodeSerializer))]
    [InlineData(typeof(decimal), typeof(DecimalBencodeSerializer))]
    [InlineData(typeof(Guid), typeof(GuidBencodeSerializer))]
    [InlineData(typeof(DateTime), typeof(DateTimeBencodeSerializer))]
    [InlineData(typeof(DateOnly), typeof(DateOnlyBencodeSerializer))]
    [InlineData(typeof(TimeOnly), typeof(TimeOnlyBencodeSerializer))]
    [InlineData(typeof(TimeSpan), typeof(TimeSpanBencodeSerializer))]
    [InlineData(typeof(string), typeof(StringBencodeSerializer))]
    public void TryGetInstanceResolvesExpectedSerializer(Type valueType, Type expectedSerializerType)
    {
        var result = BencodeSerializer.TryGetSerializerForType(valueType, options, out var serializer);

        Assert.True(result);
        Assert.NotNull(serializer);
        Assert.IsType(expectedSerializerType, serializer);
    }

    [Theory]
    [InlineData(typeof(object))]
    public void TryGetInstanceReturnsFalseForUnregisteredTypes(Type valueType)
    {
        var result = BencodeSerializer.TryGetSerializerForType(valueType, options, out var serializer);

        Assert.False(result);
        Assert.Null(serializer);
    }

    [Theory]
    [MemberData(nameof(AllRegisteredSerializerTypes))]
    public void RegisteredSerializerTypesImplementInterface(Type serializerType)
    {
        Assert.True(
            typeof(IBencodeSerializer).IsAssignableFrom(serializerType),
            $"{serializerType.FullName} does not implement IBencodeSerializer");
    }
}
