using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for the <see cref="Int128"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// Bencode integers are defined as signed 64-bit values. As a result, only
/// <see cref="Int128"/> values within the range of <see cref="long.MinValue"/> to
/// <see cref="long.MaxValue"/> can be serialized.
/// 
/// Deserialization is always lossless, as any Bencode integer value fits
/// within the range of <see cref="Int128"/>.
/// </remarks>
public sealed class LongLongBencodeSerializer : UnmanagedTypeBencodeSerializer<Int128, Binteger>
{
    /// <summary>
    /// Attempts to serialize a 128-bit signed integer into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Int128"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="input"/> fits within the range of
    /// <see cref="long"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TrySerialize(Int128 input, out Binteger? output)
    {
        output = default;

        if (input > long.MaxValue)
            return false;

        if (input < long.MinValue)
            return false;

        output = new Binteger((long)input);
        return true;
    }

    /// <summary>
    /// Deserializes a Bencode integer into a 128-bit signed integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="Int128"/> value.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as the conversion is lossless.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out Int128 output)
    {
        output = input.Value;
        return true;
    }
}

/// <summary>
/// Provides serialization and deserialization support for the <see cref="UInt128"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// Bencode integers are defined as signed 64-bit values. Consequently, only
/// <see cref="UInt128"/> values up to <see cref="long.MaxValue"/> can be serialized.
/// 
/// Deserialization additionally enforces that the encoded value is non-negative.
/// </remarks>
public sealed class UlongLongBencodeSerializer : UnmanagedTypeBencodeSerializer<UInt128, Binteger>
{
    /// <summary>
    /// Attempts to serialize a 128-bit unsigned integer into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="UInt128"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="input"/> is less than or equal to
    /// <see cref="long.MaxValue"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TrySerialize(UInt128 input, out Binteger? output)
    {
        output = default;

        if (input > long.MaxValue)
            return false;

        output = new Binteger((long)input);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into a 128-bit unsigned integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="UInt128"/> value; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value is non-negative; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out UInt128 output)
    {
        output = default;

        if (input.Value < 0)
            return false;

        output = (UInt128)input.Value;
        return true;
    }
}
