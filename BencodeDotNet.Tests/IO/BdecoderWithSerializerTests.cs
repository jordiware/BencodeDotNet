using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Text;

namespace Jordiware.BencodeDotNet.Tests.IO;

public class BdecoderWithSerializerTests
{
    private static readonly byte[] IntegerPayload = Encoding.ASCII.GetBytes("i42e");

    [Theory]
    [MemberData(nameof(GetSuccessfulDecodeCases))]
    public async Task DecodeReturnsDeserializedValue(Func<BencodeDecoder, BencodeSerializer<int, Binteger>, Task<int>> decode)
    {
        var decoder = new BencodeDecoder();
        var serializer = new IntegerPassthroughSerializer();

        var result = await decode(decoder, serializer);

        Assert.Equal(42, result);
    }

    [Theory]
    [MemberData(nameof(GetWrongBobjectCases))]
    public async Task DecodeThrowsIfDecodedBobjectTypeDoesNotMatch(Func<BencodeDecoder, BencodeSerializer<int, Bstring>, Task<int>> decode)
    {
        var decoder = new BencodeDecoder();
        var serializer = new StringExpectingSerializer();

        await Assert.ThrowsAsync<BencodeSerializerException>(() => decode(decoder, serializer));
    }

    [Theory]
    [MemberData(nameof(GetTryDeserializeFalseCases))]
    public async Task DecodeThrowsIfTryDeserializeReturnsFalse(Func<BencodeDecoder, BencodeSerializer<int, Binteger>, Task<int>> decode)
    {
        var decoder = new BencodeDecoder();
        var serializer = new RejectingIntegerSerializer();

        await Assert.ThrowsAsync<BencodeSerializerException>(() => decode(decoder, serializer));
    }

    [Theory]
    [MemberData(nameof(GetNullResultCases))]
    public async Task DecodeThrowsIfSerializerProducesNull(Func<BencodeDecoder, BencodeSerializer<int?, Binteger>, Task<int?>> decode)
    {
        var decoder = new BencodeDecoder();
        var serializer = new NullProducingIntegerSerializer();

        await Assert.ThrowsAsync<BencodeSerializerException>(() => decode(decoder, serializer));
    }

    public static TheoryData<Func<BencodeDecoder, BencodeSerializer<int, Binteger>, Task<int>>> GetSuccessfulDecodeCases()
    {
        return new()
            {
                async (decoder, serializer) =>
                {
                    var result = decoder.Decode<int, Binteger>(IntegerPayload, serializer);
                    return await Task.FromResult(result);
                },
                async (decoder, serializer) =>
                {
                    using var stream = new MemoryStream(IntegerPayload);
                    return await decoder.DecodeAsync(stream, serializer);
                },
                async (decoder, serializer) =>
                {
                    var path = WriteTempFile(IntegerPayload);
                    try
                    {
                        return await decoder.DecodeAsync(path, serializer);
                    }
                    finally
                    {
                        File.Delete(path);
                    }
                }
            };
    }

    public static TheoryData<Func<BencodeDecoder, BencodeSerializer<int, Bstring>, Task<int>>> GetWrongBobjectCases()
    {
        return new()
            {
                async (decoder, serializer) =>
                {
                    var result = decoder.Decode<int, Bstring>(IntegerPayload, serializer);
                    return await Task.FromResult(result);
                },
                async (decoder, serializer) =>
                {
                    using var stream = new MemoryStream(IntegerPayload);
                    return await decoder.DecodeAsync(stream, serializer);
                }
            };
    }

    public static TheoryData<Func<BencodeDecoder, BencodeSerializer<int, Binteger>, Task<int>>> GetTryDeserializeFalseCases()
    {
        return new()
            {
                async (decoder, serializer) =>
                {
                    var result = decoder.Decode<int, Binteger>(IntegerPayload, serializer);
                    return await Task.FromResult(result);
                },
                async (decoder, serializer) =>
                {
                    using var stream = new MemoryStream(IntegerPayload);
                    return await decoder.DecodeAsync(stream, serializer);
                }
            };
    }

    public static TheoryData<Func<BencodeDecoder, BencodeSerializer<int?, Binteger>, Task<int?>>> GetNullResultCases()
    {
        return new()
            {
                async (decoder, serializer) =>
                {
                    var result = decoder.Decode(IntegerPayload, serializer);
                    return await Task.FromResult(result);
                },
                async (decoder, serializer) =>
                {
                    using var stream = new MemoryStream(IntegerPayload);
                    return await decoder.DecodeAsync(stream, serializer);
                }
            };
    }

    private static string WriteTempFile(byte[] bytes)
    {
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private sealed class IntegerPassthroughSerializer : BencodeSerializer<int, Binteger>
    {
        public override bool TryDeserialize(Binteger value, out int result)
        {
            result = (int)value.Value;
            return true;
        }

        public override bool TrySerialize(int input, out Binteger? output)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class RejectingIntegerSerializer : BencodeSerializer<int, Binteger>
    {
        public override bool TryDeserialize(Binteger value, out int result)
        {
            result = default;
            return false;
        }

        public override bool TrySerialize(int input, out Binteger? output)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class NullProducingIntegerSerializer : BencodeSerializer<int?, Binteger>
    {
        public override bool TryDeserialize(Binteger value, out int? result)
        {
            result = null;
            return true;
        }

        public override bool TrySerialize(int? input, out Binteger? output)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class StringExpectingSerializer : BencodeSerializer<int, Bstring>
    {
        public override bool TryDeserialize(Bstring value, out int result)
        {
            result = default;
            return true;
        }

        public override bool TrySerialize(int input, out Bstring? output)
        {
            throw new NotImplementedException();
        }
    }
}
