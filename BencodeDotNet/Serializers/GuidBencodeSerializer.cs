using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Utils;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for the <see cref="Guid"/> type
/// using the Bencode byte string representation.
/// </summary>
/// <remarks>
/// <see cref="Guid"/> values are serialized as their raw 16-byte binary form using
/// <see cref="Guid.TryWriteBytes(Span{byte})"/> and stored in a <see cref="Bstring"/>.
/// This representation is compact, culture-independent, and preserves the full
/// identifier without loss.
/// 
/// Deserialization requires the input <see cref="Bstring"/> to contain exactly
/// 16 bytes. When this condition is met, the original <see cref="Guid"/> value is
/// reconstructed using the <see cref="Guid(byte[])"/> constructor.
/// 
/// This serializer guarantees lossless round-trip behavior for all
/// <see cref="Guid"/> values produced by this implementation.
/// </remarks>
public sealed class GuidBencodeSerializer : UnmanagedTypeBencodeSerializer<Guid, Bstring>
{
    /// <summary>
    /// Attempts to serialize a <see cref="Guid"/> value into a Bencode byte string.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Guid"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains a <see cref="Bstring"/>
    /// holding the 16-byte binary representation of the <see cref="Guid"/>.
    /// When this method returns <see langword="false"/>, this parameter is set to
    /// <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the <see cref="Guid"/> was successfully serialized;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Serialization uses <see cref="Guid.TryWriteBytes(Span{byte})"/> to write the
    /// binary representation of the identifier into a fixed-size buffer. This operation
    /// is guaranteed to succeed for valid <see cref="Guid"/> values when a buffer of
    /// at least 16 bytes is provided.
    /// </remarks>
    public override bool TrySerialize(Guid input, out Bstring? output)
    {
        output = default;
        Span<byte> bytes = stackalloc byte[16];
        if (input.TryWriteBytes(bytes))
        {
            output = new Bstring(bytes.ToArray());
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode byte string into a <see cref="Guid"/> value.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Bstring"/> containing the serialized <see cref="Guid"/> value.
    /// The byte string must contain exactly 16 bytes.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="Guid"/> value.
    /// When this method returns <see langword="false"/>, this parameter is set to
    /// <see langword="default"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the <see cref="Bstring"/> contained a valid
    /// 16-byte <see cref="Guid"/> representation; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Deserialization validates that the input byte string contains exactly
    /// 16 bytes before attempting reconstruction. This prevents invalid or
    /// malformed input from causing exceptions during <see cref="Guid"/> creation.
    /// </remarks>
    public override bool TryDeserialize(Bstring input, out Guid output)
    {
        output = default;

        if (input is null || input.Count != 16)
            return false;

        output = new Guid(input.Value);
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="Guid"/> value into the provided
    /// <see cref="PipeWriter"/> as a Bencode byte string (<c>16:&lt;raw-bytes&gt;</c>).
    /// </summary>
    /// <param name="input">
    /// The <see cref="Guid"/> value to serialize.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the encoded bytes will be written.
    /// The writer is owned by the caller and must not be completed, flushed, or disposed
    /// by this method.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to cancel the operation.
    /// </param>
    /// <remarks>
    /// This implementation writes the fixed-length byte string prefix (<c>16:</c>)
    /// followed by the 16-byte binary representation of the <see cref="Guid"/>.
    /// The operation is allocation-free and does not perform any intermediate buffering.
    /// </remarks>
    public override async Task WriteToPipeAsync(Guid input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        Span<byte> bytes = stackalloc byte[16];
        input.TryWriteBytes(bytes);
        await PipeWriterUtils.WriteBytesAsync(bytes.ToArray(), writer, cancellationToken);
    }
}
