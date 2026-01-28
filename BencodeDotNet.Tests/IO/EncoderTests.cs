using Jordiware.BencodeDotNet.Attributes;
using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.IO;

public class EncoderTests
{
    [BencodeSerializer(typeof(AttributedSerializer))]
    public sealed class AttributedType
    {
        public int Value { get; init; }
    }

    private sealed class AttributedSerializer : BencodeSerializer<AttributedType, BInteger>
    {
        public override bool TryDeserialize(BInteger input, out AttributedType? output)
        {
            throw new NotImplementedException();
        }

        public override bool TrySerialize(AttributedType value, out BInteger result)
        {
            result = new BInteger(value.Value);
            return true;
        }
    }

    private sealed class FailingSerializer : BencodeSerializer<AttributedType, BInteger>
    {
        public override bool TryDeserialize(BInteger input, out AttributedType? output)
        {
            throw new NotImplementedException();
        }

        public override bool TrySerialize(AttributedType value, out BInteger result)
        {
            result = null!;
            return false;
        }
    }

    private sealed class NullResultSerializer : BencodeSerializer<AttributedType, BInteger>
    {
        public override bool TryDeserialize(BInteger input, out AttributedType? output)
        {
            throw new NotImplementedException();
        }

        public override bool TrySerialize(AttributedType value, out BInteger result)
        {
            result = null!;
            return true;
        }
    }

    private sealed class ListSerializer : BencodeSerializer<AttributedType, BList>
    {
        public override bool TryDeserialize(BList input, out AttributedType? output)
        {
            throw new NotImplementedException();
        }

        public override bool TrySerialize(AttributedType value, out BList result)
        {
            result = new BList([new BInteger(value.Value)]);
            return true;
        }
    }

    [Theory]
    [InlineData(null)]
    public void EncodeThrowsArgumentNullExceptionWhenValueIsNull(object? value)
    {
        var encoder = new BencodeEncoder();

        Assert.Throws<ArgumentNullException>(() => encoder.Encode(value));
    }

    [Theory]
    [InlineData(123)]
    public void EncodeThrowsNotSupportedExceptionWhenNoSerializerIsDeclared(object value)
    {
        var encoder = new BencodeEncoder();

        Assert.Throws<ArgumentNullException>(() => encoder.Encode<object, IBObject>(value, null!));
    }

    [Theory]
    [InlineData(42)]
    public void EncodeUsesAttributedSerializerWhenPresent(int value)
    {
        var encoder = new BencodeEncoder();
        var input = new AttributedType { Value = value };

        var result = encoder.Encode(input);

        Assert.NotNull(result);
        Assert.IsType<BInteger>(result);
    }

    [Theory]
    [InlineData(42)]
    public void EncodeValidatesResultAgainstOptions(int value)
    {
        var options = new BencodeOptions(maxDepth: 0, maxPayloadLength: 64 * 1024, maxContainerItems: 1024);
        var encoder = new BencodeEncoder(options);
        var input = new AttributedType { Value = value };

        Assert.Throws<BencodeValidationException>(() => encoder.Encode(input));
    }

    [Theory]
    [InlineData(null)]
    public void EncodeGenericThrowsArgumentNullExceptionWhenValueIsNull(AttributedType? value)
    {
        var encoder = new BencodeEncoder();
        var serializer = new AttributedSerializer();

        Assert.Throws<ArgumentNullException>(() =>
            encoder.Encode<AttributedType, BInteger>(value!, serializer));
    }

    [Theory]
    [InlineData(1)]
    public void EncodeGenericThrowsArgumentNullExceptionWhenSerializerIsNull(int value)
    {
        var encoder = new BencodeEncoder();
        var input = new AttributedType { Value = value };

        Assert.Throws<ArgumentNullException>(() =>
            encoder.Encode<AttributedType, BInteger>(input, null!));
    }

    [Theory]
    [InlineData(1)]
    public void EncodeGenericThrowsInvalidOperationExceptionWhenSerializationFails(int value)
    {
        var encoder = new BencodeEncoder();
        var serializer = new FailingSerializer();
        var input = new AttributedType { Value = value };

        Assert.Throws<BencodeSerializerException>(() =>
            encoder.Encode<AttributedType, BInteger>(input, serializer));
    }

    [Theory]
    [InlineData(1)]
    public void EncodeGenericThrowsInvalidOperationExceptionWhenSerializerReturnsNull(int value)
    {
        var encoder = new BencodeEncoder();
        var serializer = new NullResultSerializer();
        var input = new AttributedType { Value = value };

        Assert.Throws<BencodeSerializerException>(() =>
            encoder.Encode<AttributedType, BInteger>(input, serializer));
    }

    [Theory]
    [InlineData(1)]
    public void EncodeGenericReturnsBobjectWhenSerializationSucceeds(int value)
    {
        var encoder = new BencodeEncoder();
        var serializer = new AttributedSerializer();
        var input = new AttributedType { Value = value };

        var result = encoder.Encode<AttributedType, BInteger>(input, serializer);

        Assert.NotNull(result);
        Assert.IsType<BInteger>(result);
    }

    [Theory]
    [InlineData(1)]
    public void EncodeGenericValidatesResultAgainstOptions(int value)
    {
        var options = new BencodeOptions(maxDepth: 0, maxPayloadLength: 64 * 1024, maxContainerItems: 1024);
        var encoder = new BencodeEncoder(options);
        var serializer = new ListSerializer();
        var input = new AttributedType { Value = value };

        Assert.Throws<BencodeValidationException>(() =>
            encoder.Encode<AttributedType, BList>(input, serializer));
    }
}
