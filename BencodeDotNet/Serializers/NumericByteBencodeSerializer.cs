using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for numeric <see cref="byte"/> values
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// This serializer is explicitly named <c>NumericByteBencodeSerializer</c> to
/// differentiate it from other potential <see cref="byte"/>-based serializers
/// (for example, raw binary or string-backed representations).
/// 
/// Deserialization performs strict range validation and fails if the encoded value
/// does not fit within the bounds of <see cref="byte"/>.
/// </remarks>
public sealed class NumericByteBencodeSerializer : UnmanagedTypeBencodeSerializer<byte, Binteger>
{
    /// <summary>
    /// Serializes an 8-bit unsigned integer into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="byte"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as all <see cref="byte"/> values are
    /// representable in Bencode.
    /// </returns>
    public override bool TrySerialize(byte input, out Binteger output)
    {
        output = new Binteger(input);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into an 8-bit unsigned integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="byte"/> value; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value fits within the bounds of
    /// <see cref="byte"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out byte output)
    {
        output = default;

        if (input.Value > byte.MaxValue)
            return false;

        if (input.Value < byte.MinValue)
            return false;

        output = (byte)input.Value;
        return true;
    }
}

/// <summary>
/// Provides serialization and deserialization support for the <see cref="sbyte"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// This serializer maps <see cref="sbyte"/> values to <see cref="Binteger"/> objects.
/// 
/// Deserialization performs strict range validation and fails if the encoded value
/// does not fit within the bounds of <see cref="sbyte"/>.
/// </remarks>
public sealed class SbyteBencodeSerializer : UnmanagedTypeBencodeSerializer<sbyte, Binteger>
{
    /// <summary>
    /// Serializes an 8-bit signed integer into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="sbyte"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as all <see cref="sbyte"/> values are
    /// representable in Bencode.
    /// </returns>
    public override bool TrySerialize(sbyte input, out Binteger output)
    {
        output = new Binteger(input);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into an 8-bit signed integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="sbyte"/> value; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value fits within the bounds of
    /// <see cref="sbyte"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out sbyte output)
    {
        output = default;

        if (input.Value > sbyte.MaxValue)
            return false;

        if (input.Value < sbyte.MinValue)
            return false;

        output = (sbyte)input.Value;
        return true;
    }
}
