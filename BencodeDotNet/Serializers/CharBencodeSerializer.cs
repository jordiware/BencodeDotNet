using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for <see cref="char"/> values
/// using the Bencode <see cref="Binteger"/> representation.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="char"/> in .NET represents a UTF-16 code unit and is therefore
/// modeled as a numeric scalar value when encoded in Bencode.
/// </para>
/// <para>
/// This serializer maps <see cref="char"/> values directly to their numeric
/// representation using <see cref="Binteger"/>, ensuring lossless, encoding-agnostic,
/// and round-trip-safe conversion.
/// </para>
/// <para>
/// During deserialization, the underlying integer value is validated to ensure
/// it falls within the valid <see cref="char"/> range
/// (<see cref="char.MinValue"/> to <see cref="char.MaxValue"/>).
/// </para>
/// </remarks>
public sealed class CharBencodeSerializer : UnmanagedTypeBencodeSerializer<char, Binteger>
{
    /// <summary>
    /// Attempts to serialize a <see cref="char"/> value into a <see cref="Binteger"/>.
    /// </summary>
    /// <param name="input">
    /// The <see cref="char"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the resulting
    /// <see cref="Binteger"/> representation of <paramref name="input"/>.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as all <see cref="char"/> values
    /// are valid and serializable as <see cref="Binteger"/>.
    /// </returns>
    public override bool TrySerialize(char input, out Binteger? output)
    {
        output = new Binteger(input);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="Binteger"/> into a <see cref="char"/>.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance containing the numeric value to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="char"/> value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the integer value is within the valid range of
    /// <see cref="char"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out char output)
    {
        output = default;

        if (input.Value is >= char.MinValue and <= char.MaxValue)
        {
            output = (char)input.Value;
            return true;
        }

        return false;
    }
}
