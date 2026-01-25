using System.Text;

namespace Jordiware.BencodeDotNet.Tests.IO;

public class BdecoderGenericDecodeTests
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

        Assert.Throws<NotSupportedException>(() => decoder.Decode<DateTimeOffset>(bytes));
    }

    [Theory]
    [InlineData("i42e")]
    public void DecodeThrowsIfDeserializationFails(string input)
    {
        var decoder = new BencodeDecoder();
        var bytes = Encoding.ASCII.GetBytes(input);

        Assert.Throws<InvalidOperationException>(() => decoder.Decode<string>(bytes));
    }

    [Theory]
    [InlineData("i42e")]
    public async Task DecodeAsyncThrowsIfCancelled(string input)
    {
        var decoder = new BencodeDecoder();
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => decoder.DecodeAsync<int>(stream, cts.Token));
    }
}
