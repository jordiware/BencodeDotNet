using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Tests.Serializers;

public class ByteArraySerializerTests
{
    public static IEnumerable<object[]> GetByteArrays()
    {
        yield return new object[] { Array.Empty<byte>() };               // empty array
        yield return new object[] { new byte[] { 0 } };                  // single zero
        yield return new object[] { new byte[] { 1, 2, 3 } };           // small array
        yield return new object[] { new byte[] { 255, 0, 128, 64 } };   // edge byte values
        yield return new object[] { new byte[1024] };                   // 1 KB array
        yield return new object[] { new byte[4096] };                   // 4 KB array
        yield return new object[] { GenerateLargeArray(100_000) };      // 100 KB array
        yield return new object[] { GenerateLargeArray(1_000_000) };      // 1 MB array
        yield return new object[] { GenerateLargeArray(10_000_000) };      // 10 MB array
    }

    private static byte[] GenerateLargeArray(int size)
    {
        var array = new byte[size];
        for (int i = 0; i < size; i++)
        {
            array[i] = (byte)(i % 256);
        }
        return array;
    }

    [Theory]
    [MemberData(nameof(GetByteArrays))]
    public void TrySerializeReturnsTrueAndPreservesData(byte[] input)
    {
        var serializer = new ByteArraySerializer();

        bool result = serializer.TrySerialize(input, out Bstring? output);

        Assert.True(result);
        Assert.NotNull(output);
        Assert.Equal(input, output!.Value);
    }

    [Theory]
    [MemberData(nameof(GetByteArrays))]
    public void TryDeserializeReturnsTrueAndPreservesData(byte[] expected)
    {
        var serializer = new ByteArraySerializer();
        var bstring = new Bstring(expected);

        bool result = serializer.TryDeserialize(bstring, out byte[]? output);

        Assert.True(result);
        Assert.NotNull(output);
        Assert.Equal(expected, output!);
    }

    [Theory]
    [MemberData(nameof(GetByteArrays))]
    public void SerializeThenDeserializeRoundTripPreservesData(byte[] original)
    {
        var serializer = new ByteArraySerializer();

        bool serialized = serializer.TrySerialize(original, out Bstring? bstring);
        bool deserialized = serializer.TryDeserialize(bstring!, out byte[]? result);

        Assert.True(serialized);
        Assert.True(deserialized);
        Assert.NotNull(result);
        Assert.Equal(original, result!);
    }

    [Fact]
    public void TrySerializeReturnsFalseForNullInput()
    {
        var serializer = new ByteArraySerializer();

        bool result = serializer.TrySerialize(null!, out Bstring? output);

        Assert.False(result);
        Assert.Null(output);
    }

    [Fact]
    public void TryDeserializeReturnsFalseForNullInput()
    {
        var serializer = new ByteArraySerializer();

        bool result = serializer.TryDeserialize(null!, out byte[]? output);

        Assert.False(result);
        Assert.Null(output);
    }

    [Fact]
    public void RegistryCanResolveByteArraySerializer()
    {
        var options = new BencodeOptions();
        bool resolved = BencodeSerializer.TryGetSerializerForType(typeof(byte[]), options, out IBencodeSerializer? instance);

        Assert.True(resolved);
        Assert.NotNull(instance);
        Assert.IsType<ByteArraySerializer>(instance);
    }
}