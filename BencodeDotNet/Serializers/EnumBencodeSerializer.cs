using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Utils;
using System.IO.Pipelines;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for enum types
/// using the Bencode integer representation.
/// </summary>
/// <typeparam name="TEnum">
/// The enum type to serialize and deserialize.
/// </typeparam>
/// <remarks>
/// Enum values are serialized as <see cref="Binteger"/> instances containing
/// the numeric value of the enum’s underlying type.
/// 
/// During deserialization, non-flags enums require values to be explicitly
/// defined in <typeparamref name="TEnum"/>, while enums marked with
/// <see cref="FlagsAttribute"/> allow composite bitwise values, provided the
/// numeric value fits within the enum’s underlying type.
/// </remarks>
public sealed class EnumBencodeSerializer<TEnum> : BencodeSerializer<TEnum, Binteger> 
    where TEnum : struct, Enum
{
    private static readonly Type EnumType = typeof(TEnum);

    private static readonly bool IsFlagsEnum = EnumType.IsDefined(typeof(FlagsAttribute), inherit: false);

    private static readonly Type UnderlyingType = Enum.GetUnderlyingType(EnumType);

    /// <summary>
    /// Serializes the specified enum value into its Bencode integer representation.
    /// </summary>
    /// <param name="input">The enum value to serialize.</param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains a
    /// <see cref="Binteger"/> representing the numeric value of
    /// <paramref name="input"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if serialization succeeds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public override bool TrySerialize(TEnum input, out Binteger? output)
    {
        var underlying = Convert.ToInt64(input);
        output = new Binteger(underlying);
        return true;
    }

    /// <summary>
    /// Deserializes the specified Bencode integer into an enum value.
    /// </summary>
    /// <param name="input">The <see cref="Binteger"/> to deserialize.</param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// enum value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if deserialization succeeds; otherwise,
    /// <see langword="false"/> when the value is invalid for
    /// <typeparamref name="TEnum"/>.
    /// </returns>
    /// <remarks>
    /// For non-flags enums, the integer value must be explicitly defined in
    /// <typeparamref name="TEnum"/>. For enums marked with
    /// <see cref="FlagsAttribute"/>, composite values are allowed as long as
    /// the numeric value fits within the enum’s underlying type.
    /// </remarks>
    public override bool TryDeserialize(Binteger input, out TEnum output)
    {
        output = default;

        var value = input.Value;

        if (!IsValueInUnderlyingRange(value))
            return false;

        var convertedValue = Convert.ChangeType(value, UnderlyingType);

        if (!IsFlagsEnum && !Enum.IsDefined(EnumType, convertedValue))
            return false;

        output = (TEnum)Enum.ToObject(EnumType, convertedValue);
        return true;
    }

    /// <summary>
    /// Asynchronously serializes an enum value to the provided
    /// <see cref="PipeWriter"/> in Bencode format.
    /// </summary>
    /// <param name="input">
    /// The enum value to serialize.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the serialized value will be written.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    /// <remarks>
    /// The enum value is converted to its underlying integral representation
    /// using <see cref="Convert.ToInt64(object)"/> and written as a Bencode integer,
    /// mirroring the behavior of the corresponding <c>TrySerialize</c> implementation.
    /// </remarks>
    public override async Task WriteToPipeAsync(TEnum input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        var underlying = Convert.ToInt64(input);
        await BencodePipeWriter.WriteIntegerAsync(underlying, writer, cancellationToken);
    }

    private static bool IsValueInUnderlyingRange(long value)
    {
        if (UnderlyingType == typeof(byte) && value is >= byte.MinValue and <= byte.MaxValue)
            return true;
        if (UnderlyingType == typeof(sbyte) && value is >= sbyte.MinValue and <= sbyte.MaxValue)
            return true;
        if (UnderlyingType == typeof(short) && value is >= short.MinValue and <= short.MaxValue)
            return true;
        if (UnderlyingType == typeof(ushort) && value is >= ushort.MinValue and <= ushort.MaxValue)
            return true;
        if (UnderlyingType == typeof(int) && value is >= int.MinValue and <= int.MaxValue)
            return true;
        if (UnderlyingType == typeof(uint) && value is >= uint.MinValue and <= uint.MaxValue)
            return true;
        if (UnderlyingType == typeof(long))
            return true;
        if (UnderlyingType == typeof(ulong) && value is >= 0)
            return true;

        return false;
    }
}
