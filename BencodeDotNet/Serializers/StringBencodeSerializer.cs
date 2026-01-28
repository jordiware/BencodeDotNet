using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Utils;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for <see cref="string"/> values
/// using the Bencode string (<see cref="BString"/>) representation.
/// </summary>
/// <remarks>
/// <para>
/// Strings are serialized by encoding their character data into a byte sequence
/// using a specified <see cref="Encoding"/> and wrapping the result in a
/// <see cref="BString"/>.
/// </para>
/// <para>
/// Bencode does not define or carry encoding metadata. As a result, deserialization
/// must use the same <see cref="Encoding"/> instance that was used during serialization
/// in order to guarantee a correct round-trip.
/// </para>
/// <para>
/// By default, UTF-8 encoding is used. Alternative encodings may be supplied via
/// the constructor.
/// </para>
/// <para>
/// Null strings are not supported, as Bencode does not define a null value.
/// Empty strings are serialized as empty Bencode strings.
/// </para>
/// </remarks>
public sealed class StringBencodeSerializer : ReferenceTypeBencodeSerializer<string, BString>
{
    /// <summary>
    /// The default character encoding used by the serializer when none is specified.
    /// </summary>
    /// <remarks>
    /// UTF-8 is the de facto standard encoding for Bencode strings and is therefore
    /// used as the default.
    /// </remarks>
    public static readonly Encoding DefaultEncoding = Encoding.UTF8;

    private readonly Encoding _encoding;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringBencodeSerializer"/> class
    /// using the default character encoding.
    /// </summary>
    /// <remarks>
    /// This constructor configures the serializer to use
    /// <see cref="DefaultEncoding"/>, which is UTF-8.
    /// UTF-8 is the de facto standard encoding for Bencode strings and ensures
    /// maximum interoperability across implementations.
    /// </remarks>
    public StringBencodeSerializer()
    {
        _encoding = DefaultEncoding;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringBencodeSerializer"/> class
    /// using the specified <see cref="Encoding"/>.
    /// </summary>
    /// <param name="encoding">
    /// The character encoding to use for string serialization and deserialization.
    /// If <see langword="null"/>, <see cref="DefaultEncoding"/> is used.
    /// </param>
    public StringBencodeSerializer(Encoding encoding)
    {
        _encoding = encoding ?? DefaultEncoding;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringBencodeSerializer"/> class
    /// using the encoding identified by the specified name.
    /// </summary>
    /// <param name="encodingName">
    /// The name of the character encoding to use.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="encodingName"/> is not a valid encoding name.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if the specified encoding is not supported on the current platform.
    /// </exception>
    public StringBencodeSerializer(string encodingName)
    {
        _encoding = Encoding.GetEncoding(encodingName);
    }

    /// <summary>
    /// Attempts to serialize a <see cref="string"/> value into a <see cref="BString"/>.
    /// </summary>
    /// <param name="input">
    /// The string value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="BString"/> representation of <paramref name="input"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if serialization succeeded; otherwise,
    /// <see langword="false"/> if <paramref name="input"/> is <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// Empty strings are serialized as empty Bencode strings.
    /// </remarks>
    public override bool TrySerialize(string input, out BString? output)
    {
        output = default;

        if (input is null)
            return false;

        if (input == string.Empty)
        {
            output = new BString([]);
            return true;
        }

        var bytes = _encoding.GetBytes(input);
        output = new BString(bytes);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="BString"/> into a <see cref="string"/>.
    /// </summary>
    /// <param name="input">
    /// The <see cref="BString"/> value to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="string"/> value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if deserialization succeeded; otherwise,
    /// <see langword="false"/> if <paramref name="input"/> is <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// Deserialization assumes that the <see cref="BString"/> was originally produced
    /// using the same <see cref="Encoding"/> instance configured for this serializer.
    /// Empty Bencode strings are deserialized as <see cref="string.Empty"/>.
    /// </remarks>
    public override bool TryDeserialize(BString input, out string? output)
    {
        output = default;

        if (input is null)
            return false;

        if (input.Count == 0)
        {
            output = string.Empty;
            return true;
        }

        output = _encoding.GetString(input.Value);
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="string"/> value into the provided
    /// <see cref="PipeWriter"/> as a Bencode byte string (<c>&lt;length&gt;:&lt;raw-bytes&gt;</c>),
    /// using the configured <see cref="Encoding"/>.
    /// </summary>
    /// <param name="input">
    /// The string value to serialize. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the encoded bytes will be written.
    /// The writer is owned by the caller and must not be completed, flushed, or disposed.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    public override async Task WriteToPipeAsync(string input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await PipeWriterUtils.WriteStringAsync(input, _encoding, writer, cancellationToken);
    }
}
