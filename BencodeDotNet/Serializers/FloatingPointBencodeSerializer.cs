using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Utils;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for the <see cref="float"/> type
/// using a Bencode string representation.
/// </summary>
/// <remarks>
/// Bencode does not define a native floating-point representation. This serializer
/// encodes <see cref="float"/> values as ASCII strings using a canonical textual
/// format produced by <see cref="FloatingPointNumberFormatter"/>.
///
/// <para>
/// Serialization delegates formatting to <see cref="FloatingPointNumberFormatter"/>,
/// ensuring a culture-invariant and deterministic representation suitable for
/// cross-platform and cross-language interchange.
/// </para>
///
/// <para>
/// Deserialization parses the textual representation using
/// <see cref="CultureInfo.InvariantCulture"/> and <see cref="NumberStyles.Float"/>,
/// guaranteeing symmetry with the formatter and preventing culture-dependent behavior.
/// </para>
///
/// <para>
/// Round-trip serialization is lossless for values produced by this serializer,
/// within the precision limits of the <see cref="float"/> type.
/// </para>
/// </remarks>
public sealed class FloatBencodeSerializer : UnmanagedTypeBencodeSerializer<float, BString>
{
    /// <summary>
    /// Attempts to serialize a <see cref="float"/> value into a <see cref="BString"/>.
    /// </summary>
    /// <param name="input">The <see cref="float"/> value to serialize.</param>
    /// <param name="output">
    /// When this method returns <c>true</c>, contains the resulting <see cref="BString"/>
    /// representing the formatted floating-point value; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value was successfully formatted and serialized;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool TrySerialize(float input, out BString? output)
    {
        output = default;
        if (FloatingPointNumberFormatter.TryFormat(input, out var s))
        {
            output = new BString(s, Encoding.ASCII);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="BString"/> into a <see cref="float"/> value.
    /// </summary>
    /// <param name="input">The <see cref="BString"/> containing the textual representation.</param>
    /// <param name="output">
    /// When this method returns <c>true</c>, contains the parsed <see cref="float"/> value;
    /// otherwise, the default value of <see cref="float"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value was successfully parsed;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool TryDeserialize(BString input, out float output)
    {
        var s = Encoding.ASCII.GetString(input.Value);
        return float.TryParse(s,
                              NumberStyles.Float,
                              CultureInfo.InvariantCulture,
                              out output);
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="float"/> value into the provided
    /// <see cref="PipeWriter"/> in Bencode format by converting it to a string
    /// using ASCII encoding.
    /// </summary>
    /// <param name="input">
    /// The <see cref="float"/> value to serialize.
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
    public override async Task WriteToPipeAsync(float input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        if (!FloatingPointNumberFormatter.TryFormat(input, out var s))
            throw new BencodeFormatException($"Unable to format float value {input}.");

        await PipeWriterUtils.WriteStringAsync(s, Encoding.ASCII, writer, cancellationToken);
    }
}

/// <summary>
/// Provides serialization and deserialization support for the <see cref="double"/> type
/// using a Bencode string representation.
/// </summary>
/// <remarks>
/// <see cref="double"/> values are serialized as ASCII strings using a canonical,
/// culture-invariant textual format defined by
/// <see cref="FloatingPointNumberFormatter"/>.
///
/// <para>
/// This approach avoids ambiguity inherent to binary floating-point encodings
/// in Bencode and ensures consistent interpretation across platforms and runtimes.
/// </para>
///
/// <para>
/// Deserialization parses the textual representation using
/// <see cref="CultureInfo.InvariantCulture"/> and <see cref="NumberStyles.Float"/>,
/// guaranteeing compatibility with the formatter output.
/// </para>
///
/// <para>
/// Round-trip serialization is lossless for values produced by this serializer,
/// within the precision guarantees of the <see cref="double"/> type.
/// </para>
/// </remarks>
public sealed class DoubleBencodeSerializer : UnmanagedTypeBencodeSerializer<double, BString>
{
    /// <summary>
    /// Attempts to serialize a <see cref="double"/> value into a <see cref="BString"/>.
    /// </summary>
    /// <param name="input">The <see cref="double"/> value to serialize.</param>
    /// <param name="output">
    /// When this method returns <c>true</c>, contains the resulting <see cref="BString"/>
    /// representing the formatted floating-point value; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value was successfully formatted and serialized;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool TrySerialize(double input, out BString? output)
    {
        output = default;
        if (FloatingPointNumberFormatter.TryFormat(input, out var s))
        {
            output = new BString(s, Encoding.ASCII);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="BString"/> into a <see cref="double"/> value.
    /// </summary>
    /// <param name="input">The <see cref="BString"/> containing the textual representation.</param>
    /// <param name="output">
    /// When this method returns <c>true</c>, contains the parsed <see cref="double"/> value;
    /// otherwise, the default value of <see cref="double"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value was successfully parsed;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool TryDeserialize(BString input, out double output)
    {
        var s = Encoding.ASCII.GetString(input.Value);
        return double.TryParse(s,
                               NumberStyles.Float,
                               CultureInfo.InvariantCulture,
                               out output);
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="double"/> value into the provided
    /// <see cref="PipeWriter"/> in Bencode format by converting it to a string
    /// using ASCII encoding.
    /// </summary>
    /// <param name="input">
    /// The <see cref="double"/> value to serialize.
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
    public override async Task WriteToPipeAsync(double input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        if (!FloatingPointNumberFormatter.TryFormat(input, out var s))
            throw new BencodeFormatException($"Unable to format double value {input}.");

        await PipeWriterUtils.WriteStringAsync(s, Encoding.ASCII, writer, cancellationToken);
    }
}

/// <summary>
/// Provides serialization and deserialization support for the <see cref="decimal"/> type
/// using a Bencode string representation.
/// </summary>
/// <remarks>
/// <see cref="decimal"/> values are serialized as ASCII strings using a canonical,
/// culture-invariant textual format produced by
/// <see cref="FloatingPointNumberFormatter"/>.
///
/// <para>
/// Unlike binary floating-point types, <see cref="decimal"/> preserves base-10
/// precision. This serializer ensures that the exact decimal value is preserved
/// across serialization and deserialization when produced by this formatter.
/// </para>
///
/// <para>
/// Deserialization uses <see cref="CultureInfo.InvariantCulture"/> and
/// <see cref="NumberStyles.Float"/> to guarantee deterministic parsing behavior
/// independent of the current culture.
/// </para>
/// </remarks>
public sealed class DecimalBencodeSerializer : UnmanagedTypeBencodeSerializer<decimal, BString>
{
    /// <summary>
    /// Attempts to serialize a <see cref="decimal"/> value into a <see cref="BString"/>.
    /// </summary>
    /// <param name="input">The <see cref="decimal"/> value to serialize.</param>
    /// <param name="output">
    /// When this method returns <c>true</c>, contains the resulting <see cref="BString"/>
    /// representing the formatted decimal value; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value was successfully formatted and serialized;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool TrySerialize(decimal input, out BString? output)
    {
        output = default;
        if (FloatingPointNumberFormatter.TryFormat(input, out var s))
        {
            output = new BString(s, Encoding.ASCII);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="BString"/> into a <see cref="decimal"/> value.
    /// </summary>
    /// <param name="input">The <see cref="BString"/> containing the textual representation.</param>
    /// <param name="output">
    /// When this method returns <c>true</c>, contains the parsed <see cref="decimal"/> value;
    /// otherwise, the default value of <see cref="decimal"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value was successfully parsed;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool TryDeserialize(BString input, out decimal output)
    {
        var s = Encoding.ASCII.GetString(input.Value);
        return decimal.TryParse(s,
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out output);
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="decimal"/> value into the provided
    /// <see cref="PipeWriter"/> in Bencode format by converting it to a string
    /// using ASCII encoding.
    /// </summary>
    /// <param name="input">
    /// The <see cref="decimal"/> value to serialize.
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
    public override async Task WriteToPipeAsync(decimal input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        if (!FloatingPointNumberFormatter.TryFormat(input, out var s))
            throw new BencodeFormatException($"Unable to format decimal value {input}.");

        await PipeWriterUtils.WriteStringAsync(s, Encoding.ASCII, writer, cancellationToken);
    }
}
