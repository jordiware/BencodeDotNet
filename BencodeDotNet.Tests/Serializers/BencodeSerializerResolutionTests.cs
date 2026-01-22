using Jordiware.BencodeDotNet.Attributes;
using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Collections;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

#region Dummy types
[BencodeSerializer(typeof(FakeAttributedSerializer))]
public sealed class AttributedType
{
}

public sealed class FakeAttributedSerializer : BencodeSerializer<AttributedType, IBobject>
{
    public override bool TrySerialize(AttributedType input, out IBobject? output)
    {
        throw new NotImplementedException();
    }

    public override bool TryDeserialize(IBobject input, out AttributedType? output)
    {
        throw new NotImplementedException();
    }
}

[BencodeSerializer(typeof(FakeAttributedEnumerableSerializer))]
public sealed class AttributedEnumerableType : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator()
    {
        return Array.Empty<int>().AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class FakeAttributedEnumerableSerializer : BencodeSerializer<AttributedEnumerableType, IBobject>
{
    public override bool TrySerialize(AttributedEnumerableType input, out IBobject? output)
    {
        throw new NotImplementedException();
    }

    public override bool TryDeserialize(IBobject input, out AttributedEnumerableType? output)
    {
        throw new NotImplementedException();
    }
}
#endregion

public class BencodeSerializerResolutionTests
{
    [Fact]
    public void TryGetSerializerForTypePrefersAttributedSerializer()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(AttributedType), out var serializer);

        Assert.True(result);
        Assert.NotNull(serializer);
        Assert.IsType<FakeAttributedSerializer>(serializer);
    }

    [Fact]
    public void TryGetSerializerForTypeResolvesDictionaryBeforeEnumerable()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(IDictionary<string, int>), out var serializer);

        Assert.True(result);
        Assert.NotNull(serializer);
        Assert.IsType<DictionaryBencodeSerializer<string, int>>(serializer);
    }

    [Fact]
    public void TryGetSerializerForTypeResolvesEnumerableSerializer()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(IEnumerable<int>), out var serializer);

        Assert.True(result);
        Assert.NotNull(serializer);
        Assert.IsType<EnumerableBencodeSerializer<int>>(serializer);
    }

    [Fact]
    public void TryGetSerializerForTypeDoesNotResolveNonGenericEnumerable()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(IEnumerable), out var serializer);

        Assert.False(result);
        Assert.Null(serializer);
    }

    [Fact]
    public void TryGetSerializerForTypeAttributeOverridesEnumerable()
    {
        var result = BencodeSerializer.TryGetSerializerForType(typeof(AttributedEnumerableType), out var serializer);

        Assert.True(result);
        Assert.NotNull(serializer);
        Assert.IsType<FakeAttributedEnumerableSerializer>(serializer);
    }
}
