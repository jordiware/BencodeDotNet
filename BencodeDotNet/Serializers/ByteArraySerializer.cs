using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Utils;
using System.Buffers.Text;
using System.IO.Pipelines;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization of <see cref="byte[]"/> to and from <see cref="Bstring"/> 
/// in Bencode format. This serializer treats the byte array purely as raw data, not as a numeric value.
/// </summary>
public sealed class ByteArraySerializer : ReferenceTypeBencodeSerializer<byte[], Bstring>
{
    /// <summary>
    /// Attempts to serialize the provided <see cref="byte[]"/> into a <see cref="Bstring"/>.
    /// </summary>
    /// <param name="input">The byte array to serialize.</param>
    /// <param name="output">When this method returns, contains the serialized <see cref="Bstring"/> if successful; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the input was not <c>null</c> and serialization succeeded; otherwise, <c>false</c>.</returns>
    public override bool TrySerialize(byte[] input, out Bstring? output)
    {
        output = default;
        if (input is null)
            return false;

        output = new Bstring(input);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize the provided <see cref="Bstring"/> into a <see cref="byte[]"/>.
    /// </summary>
    /// <param name="input">The <see cref="Bstring"/> to deserialize.</param>
    /// <param name="output">When this method returns, contains the deserialized byte array if successful; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the input was not <c>null</c> and deserialization succeeded; otherwise, <c>false</c>.</returns>
    public override bool TryDeserialize(Bstring input, out byte[]? output)
    {
        output = default;
        if (input is null)
            return false;

        output = input.Value;
        return true;
    }

    /// <summary>
    /// Asynchronously writes the specified <see cref="byte[]"/> to a <see cref="PipeWriter"/> 
    /// in Bencode string format (<c>length:data</c>).
    /// </summary>
    /// <param name="input">The byte array to write.</param>
    /// <param name="writer">The <see cref="PipeWriter"/> to write the serialized data to.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the write to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous write operation.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown if the length of the byte array cannot be formatted as UTF-8.
    /// </exception>
    public override async Task WriteToPipeAsync(byte[] input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await PipeWriterUtils.WriteBytesAsync(input.AsMemory(), writer, cancellationToken);
    }
}
