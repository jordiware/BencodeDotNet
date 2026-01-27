using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Utils;
using System.IO.Pipelines;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for the <see cref="int"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// This serializer maps <see cref="int"/> values to <see cref="Binteger"/> objects.
/// 
/// Deserialization performs strict range validation and fails if the encoded value
/// does not fit within the bounds of <see cref="int"/>.
/// </remarks>
public sealed class IntBencodeSerializer : UnmanagedTypeBencodeSerializer<int, Binteger>
{
    /// <summary>
    /// Serializes a 32-bit signed integer into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="int"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as all <see cref="int"/> values are
    /// representable in Bencode.
    /// </returns>
    public override bool TrySerialize(int input, out Binteger output)
    {
        output = new Binteger(input);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into a 32-bit signed integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="int"/> value; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value fits within the bounds of
    /// <see cref="int"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out int output)
    {
        output = default;

        if (input.Value > int.MaxValue)
            return false;

        if (input.Value < int.MinValue)
            return false;

        output = (int)input.Value;
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="int"/> value into the provided
    /// <see cref="PipeWriter"/> using the Bencode integer format (<c>'i' ... 'e'</c>).
    /// </summary>
    /// <param name="input">
    /// The <see cref="int"/> value to serialize.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the Bencoded bytes will be written.
    /// The writer is owned by the caller and must not be completed, flushed, or disposed
    /// by this method.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to cancel the operation.
    /// </param>
    public override async Task WriteToPipeAsync(int input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await PipeWriterUtils.WriteIntegerAsync(input, writer, cancellationToken);
    }
}

/// <summary>
/// Provides serialization and deserialization support for the <see cref="uint"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// This serializer maps <see cref="uint"/> values to <see cref="Binteger"/> objects.
/// 
/// Deserialization enforces non-negative values and validates that the encoded
/// integer fits within the bounds of <see cref="uint"/>.
/// </remarks>
public sealed class UintBencodeSerializer : UnmanagedTypeBencodeSerializer<uint, Binteger>
{
    /// <summary>
    /// Serializes a 32-bit unsigned integer into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="uint"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as all <see cref="uint"/> values are
    /// representable in Bencode.
    /// </returns>
    public override bool TrySerialize(uint input, out Binteger output)
    {
        output = new Binteger(input);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into a 32-bit unsigned integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="uint"/> value; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value is non-negative and fits within
    /// the bounds of <see cref="uint"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out uint output)
    {
        output = default;

        if (input.Value > uint.MaxValue)
            return false;

        if (input.Value < uint.MinValue)
            return false;

        output = (uint)input.Value;
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="uint"/> value into the provided
    /// <see cref="PipeWriter"/> using the Bencode integer format (<c>'i' ... 'e'</c>).
    /// </summary>
    /// <param name="input">
    /// The <see cref="uint"/> value to serialize.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the Bencoded bytes will be written.
    /// The writer is owned by the caller and must not be completed, flushed, or disposed
    /// by this method.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to cancel the operation.
    /// </param>
    public override async Task WriteToPipeAsync(uint input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await PipeWriterUtils.WriteIntegerAsync(input, writer, cancellationToken);
    }
}
