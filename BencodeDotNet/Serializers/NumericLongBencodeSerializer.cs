using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for the <see cref="long"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// This serializer provides a direct, lossless mapping between <see cref="long"/>
/// values and <see cref="Binteger"/> objects.
/// 
/// No range validation is required during deserialization, as the Bencode integer
/// representation is natively backed by a 64-bit signed integer.
/// </remarks>
public sealed class LongBencodeSerializer : UnmanagedTypeBencodeSerializer<long, Binteger>
{
    /// <summary>
    /// Serializes a 64-bit signed integer into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="long"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as all <see cref="long"/> values are
    /// representable in Bencode.
    /// </returns>
    public override bool TrySerialize(long input, out Binteger output)
    {
        output = new Binteger(input);
        return true;
    }

    /// <summary>
    /// Deserializes a Bencode integer into a 64-bit signed integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="long"/> value.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>, as the conversion is lossless.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out long output)
    {
        output = input.Value;
        return true;
    }
}

/// <summary>
/// Provides serialization and deserialization support for the <see cref="ulong"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// Bencode integers are defined as signed 64-bit values. As a result, only
/// <see cref="ulong"/> values up to <see cref="long.MaxValue"/> can be serialized.
/// 
/// Deserialization additionally enforces that the encoded value is non-negative.
/// </remarks>
public sealed class UlongBencodeSerializer : UnmanagedTypeBencodeSerializer<ulong, Binteger>
{
    /// <summary>
    /// Attempts to serialize a 64-bit unsigned integer into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="ulong"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="input"/> is less than or equal to
    /// <see cref="long.MaxValue"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TrySerialize(ulong input, out Binteger? output)
    {
        output = default;

        if (input > long.MaxValue)
            return false;

        output = new Binteger((long)input);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into a 64-bit unsigned integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="ulong"/> value; otherwise, <c>0</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value is non-negative; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out ulong output)
    {
        output = default;

        if (input.Value < 0)
            return false;

        output = (ulong)input.Value;
        return true;
    }
}
