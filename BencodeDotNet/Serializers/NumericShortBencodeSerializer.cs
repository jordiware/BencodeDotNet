using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Utils;
using System.IO.Pipelines;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for the <see cref="short"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// This serializer maps <see cref="short"/> values to <see cref="BInteger"/> objects.
/// 
/// Deserialization performs strict range validation and fails if the encoded value
/// does not fit within the bounds of <see cref="short"/>.
/// </remarks>
public sealed class ShortBencodeSerializer : UnmanagedTypeBencodeSerializer<short, BInteger>
{
    /// <summary>
    /// Serializes a 16-bit signed integer into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="short"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="BInteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as all <see cref="short"/> values are
    /// representable in Bencode.
    /// </returns>
    public override bool TrySerialize(short input, out BInteger output)
    {
        output = new BInteger(input);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into a 16-bit signed integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="BInteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="short"/> value; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value fits within the bounds of
    /// <see cref="short"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(BInteger input, out short output)
    {
        output = default;

        if (input.Value > short.MaxValue)
            return false;

        if (input.Value < short.MinValue)
            return false;

        output = (short)input.Value;
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="short"/> value into the provided
    /// <see cref="PipeWriter"/> using the Bencode integer format (<c>'i' ... 'e'</c>).
    /// </summary>
    /// <param name="input">
    /// The <see cref="short"/> value to serialize.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the Bencoded bytes will be written.
    /// The writer is owned by the caller and must not be completed, flushed, or disposed
    /// by this method.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to cancel the operation.
    /// </param>
    public override async Task WriteToPipeAsync(short input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await PipeWriterUtils.WriteIntegerAsync(input, writer, cancellationToken);
    }
}

/// <summary>
/// Provides serialization and deserialization support for the <see cref="ushort"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// This serializer maps <see cref="ushort"/> values to <see cref="BInteger"/> objects.
/// 
/// Deserialization enforces non-negative values and validates that the encoded integer
/// fits within the bounds of <see cref="ushort"/>.
/// </remarks>
public sealed class UshortBencodeSerializer : UnmanagedTypeBencodeSerializer<ushort, BInteger>
{
    /// <summary>
    /// Serializes a 16-bit unsigned integer into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="ushort"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="BInteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as all <see cref="ushort"/> values are
    /// representable in Bencode.
    /// </returns>
    public override bool TrySerialize(ushort input, out BInteger output)
    {
        output = new BInteger(input);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into a 16-bit unsigned integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="BInteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="ushort"/> value; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value is non-negative and fits within
    /// the bounds of <see cref="ushort"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(BInteger input, out ushort output)
    {
        output = default;

        if (input.Value > ushort.MaxValue)
            return false;

        if (input.Value < ushort.MinValue)
            return false;

        output = (ushort)input.Value;
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="ushort"/> value into the provided
    /// <see cref="PipeWriter"/> using the Bencode integer format (<c>'i' ... 'e'</c>).
    /// </summary>
    /// <param name="input">
    /// The <see cref="ushort"/> value to serialize.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the Bencoded bytes will be written.
    /// The writer is owned by the caller and must not be completed, flushed, or disposed
    /// by this method.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to cancel the operation.
    /// </param>
    public override async Task WriteToPipeAsync(ushort input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await PipeWriterUtils.WriteIntegerAsync(input, writer, cancellationToken);
    }
}
