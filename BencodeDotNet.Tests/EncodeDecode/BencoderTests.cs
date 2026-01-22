using Jordiware.BencodeDotNet.Attributes;
using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.EncodeDecode;

public class BencoderTests
{
    [BencodeSerializer(typeof(AttributedSerializer))]
    public sealed class AttributedType
    {
        public int Value { get; init; }
    }

    private sealed class AttributedSerializer
        : BencodeSerializer<AttributedType, Binteger>
    {
        public override bool TryDeserialize(Binteger input, out AttributedType? output)
        {
            throw new NotImplementedException();
        }

        public override bool TrySerialize(AttributedType value, out Binteger result)
        {
            result = new Binteger(value.Value);
            return true;
        }
    }

    private sealed class FailingSerializer
        : BencodeSerializer<AttributedType, Binteger>
    {
        public override bool TryDeserialize(Binteger input, out AttributedType? output)
        {
            throw new NotImplementedException();
        }

        public override bool TrySerialize(AttributedType value, out Binteger result)
        {
            result = null!;
            return false;
        }
    }

    private sealed class NullResultSerializer
        : BencodeSerializer<AttributedType, Binteger>
    {
        public override bool TryDeserialize(Binteger input, out AttributedType? output)
        {
            throw new NotImplementedException();
        }

        public override bool TrySerialize(AttributedType value, out Binteger result)
        {
            result = null!;
            return true;
        }
    }

    private sealed class ListSerializer
        : BencodeSerializer<AttributedType, Blist>
    {
        public override bool TryDeserialize(Blist input, out AttributedType? output)
        {
            throw new NotImplementedException();
        }

        public override bool TrySerialize(AttributedType value, out Blist result)
        {
            result = new Blist([new Binteger(value.Value)]);
            return true;
        }
    }

    [Theory]
    [InlineData(null)]
    public void EncodeThrowsArgumentNullExceptionWhenValueIsNull(object? value)
    {
        var encoder = new Bencoder();

        Assert.Throws<ArgumentNullException>(() => encoder.Encode(value));
    }

    [Theory]
    [InlineData(123)]
    public void EncodeThrowsNotSupportedExceptionWhenNoSerializerIsDeclared(object value)
    {
        var encoder = new Bencoder();

        Assert.Throws<ArgumentNullException>(() => encoder.Encode<object, IBobject>(value, null!));
    }

    [Theory]
    [InlineData(42)]
    public void EncodeUsesAttributedSerializerWhenPresent(int value)
    {
        var encoder = new Bencoder();
        var input = new AttributedType { Value = value };

        var result = encoder.Encode(input);

        Assert.NotNull(result);
        Assert.IsType<Binteger>(result);
    }

    [Theory]
    [InlineData(42)]
    public void EncodeValidatesResultAgainstOptions(int value)
    {
        var options = new BencodeOptions(maxDepth: 0, maxPayloadLength: 64 * 1024, maxContainerItems: 1024);
        var encoder = new Bencoder(options);
        var input = new AttributedType { Value = value };

        Assert.Throws<InvalidOperationException>(() => encoder.Encode(input));
    }

    [Theory]
    [InlineData(null)]
    public void EncodeGenericThrowsArgumentNullExceptionWhenValueIsNull(AttributedType? value)
    {
        var encoder = new Bencoder();
        var serializer = new AttributedSerializer();

        Assert.Throws<ArgumentNullException>(() =>
            encoder.Encode<AttributedType, Binteger>(value!, serializer));
    }

    [Theory]
    [InlineData(1)]
    public void EncodeGenericThrowsArgumentNullExceptionWhenSerializerIsNull(int value)
    {
        var encoder = new Bencoder();
        var input = new AttributedType { Value = value };

        Assert.Throws<ArgumentNullException>(() =>
            encoder.Encode<AttributedType, Binteger>(input, null!));
    }

    [Theory]
    [InlineData(1)]
    public void EncodeGenericThrowsInvalidOperationExceptionWhenSerializationFails(int value)
    {
        var encoder = new Bencoder();
        var serializer = new FailingSerializer();
        var input = new AttributedType { Value = value };

        Assert.Throws<InvalidOperationException>(() =>
            encoder.Encode<AttributedType, Binteger>(input, serializer));
    }

    [Theory]
    [InlineData(1)]
    public void EncodeGenericThrowsInvalidOperationExceptionWhenSerializerReturnsNull(int value)
    {
        var encoder = new Bencoder();
        var serializer = new NullResultSerializer();
        var input = new AttributedType { Value = value };

        Assert.Throws<InvalidOperationException>(() =>
            encoder.Encode<AttributedType, Binteger>(input, serializer));
    }

    [Theory]
    [InlineData(1)]
    public void EncodeGenericReturnsBobjectWhenSerializationSucceeds(int value)
    {
        var encoder = new Bencoder();
        var serializer = new AttributedSerializer();
        var input = new AttributedType { Value = value };

        var result = encoder.Encode<AttributedType, Binteger>(input, serializer);

        Assert.NotNull(result);
        Assert.IsType<Binteger>(result);
    }

    [Theory]
    [InlineData(1)]
    public void EncodeGenericValidatesResultAgainstOptions(int value)
    {
        var options = new BencodeOptions(maxDepth: 0, maxPayloadLength: 64 * 1024, maxContainerItems: 1024);
        var encoder = new Bencoder(options);
        var serializer = new ListSerializer();
        var input = new AttributedType { Value = value };

        Assert.Throws<InvalidOperationException>(() =>
            encoder.Encode<AttributedType, Blist>(input, serializer));
    }
}
