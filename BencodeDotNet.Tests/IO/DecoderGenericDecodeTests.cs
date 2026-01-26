using System.Text;

namespace Jordiware.BencodeDotNet.Tests.IO;

public class DecoderGenericDecodeTests
{
    [Theory]
    [InlineData("i42e")]
    public void DecodeUsesRegisteredSerializer(string input)
    {
        var decoder = new BencodeDecoder();
        var bytes = Encoding.ASCII.GetBytes(input);

        var result = decoder.Decode<int>(bytes);

        Assert.Equal(42, result);
    }

    [Theory]
    [InlineData("i42e")]
    public void DecodeThrowsIfSerializerNotFound(string input)
    {
        var decoder = new BencodeDecoder();
        var bytes = Encoding.ASCII.GetBytes(input);

        Assert.Throws<BencodeSerializerNotFoundException>(() => decoder.Decode<DateTimeOffset>(bytes));
    }

    [Theory]
    [InlineData("i42e")]
    public void DecodeThrowsIfDeserializationFails(string input)
    {
        var decoder = new BencodeDecoder();
        var bytes = Encoding.ASCII.GetBytes(input);

        Assert.Throws<BencodeSerializerException>(() => decoder.Decode<string>(bytes));
    }
}
