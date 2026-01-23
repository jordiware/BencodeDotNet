using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public sealed class ReflectionFallbackModel
{
    public int Value { get; set; }
}

public abstract class AbstractModel
{
    public int Value { get; set; }
}

public sealed class NoDefaultConstructorModel
{
    public NoDefaultConstructorModel(int value) => Value = value;
    public int Value { get; }
}

public sealed class ReflectionFallbackResolutionTests
{
    private static readonly BencodeOptions options = new();

    [Fact]
    public void TryGetSerializerForTypeUsesReflectionFallbackForEligibleType()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(ReflectionFallbackModel), options, out var serializer);

        Assert.True(result);
        Assert.NotNull(serializer);
        Assert.IsType<ReflectionBencodeSerializer<ReflectionFallbackModel>>(serializer);
    }

    [Fact]
    public void TryGetSerializerForTypeDoesNotFallbackForObjectType()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(object), options, out var serializer);

        Assert.False(result);
        Assert.Null(serializer);
    }

    [Fact]
    public void TryGetSerializerForTypeDoesNotFallbackForAbstractType()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(AbstractModel), options, out var serializer);

        Assert.False(result);
        Assert.Null(serializer);
    }

    [Fact]
    public void TryGetSerializerForTypeDoesNotFallbackForInterfaceType()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(IDisposable), options, out var serializer);

        Assert.False(result);
        Assert.Null(serializer);
    }

    [Fact]
    public void TryGetSerializerForTypeDoesNotFallbackWithoutParameterlessConstructor()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(NoDefaultConstructorModel), options, out var serializer);

        Assert.False(result);
        Assert.Null(serializer);
    }

    [Fact]
    public void TryGetSerializerForTypeDoesNotFallbackForPrimitiveType()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(int), options, out var serializer);

        Assert.True(result);
        Assert.IsType<IntBencodeSerializer>(serializer);
        Assert.IsNotType<ReflectionBencodeSerializer<int>>(serializer);
    }

    [Fact]
    public void ReflectionFallbackSerializerCanSerializeAndDeserialize()
    {
        BencodeSerializer.TryGetSerializerForType(typeof(ReflectionFallbackModel), options, out var serializer);

        var typed = (ReflectionBencodeSerializer<ReflectionFallbackModel>)serializer!;

        var model = new ReflectionFallbackModel { Value = 42 };

        var serializeResult = typed.TrySerialize(model, out var dict);
        var deserializeResult = typed.TryDeserialize(dict!, out var roundtrip);

        Assert.True(serializeResult);
        Assert.True(deserializeResult);
        Assert.NotNull(roundtrip);
        Assert.Equal(42, roundtrip!.Value);
    }
}
