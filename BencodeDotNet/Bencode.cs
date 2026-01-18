namespace Jordiware.BencodeDotNet;

/// <summary>
/// Defines byte-level constants used by the Bencode encoding format.
/// </summary>
/// <remarks>
/// <para>
/// These constants represent the ASCII characters that form the grammar
/// of Bencode as defined by the specification.
/// </para>
/// <para>
/// All decoding and encoding logic relies on these values for
/// token recognition and validation.
/// </para>
/// </remarks>
public static class Bencode
{
    /// <summary>
    /// Marks the beginning of a Bencode integer.
    /// </summary>
    /// <remarks>
    /// Encoded integers have the form:
    /// <c>i&lt;digits&gt;e</c>
    /// </remarks>
    public const byte IntegerBeginCharacter = (byte)'i';

    /// <summary>
    /// Marks the beginning of a Bencode list.
    /// </summary>
    /// <remarks>
    /// Encoded lists have the form:
    /// <c>l&lt;elements&gt;e</c>
    /// </remarks>
    public const byte ListBeginCharacter = (byte)'l';

    /// <summary>
    /// Marks the beginning of a Bencode dictionary.
    /// </summary>
    /// <remarks>
    /// Encoded dictionaries have the form:
    /// <c>d&lt;key&gt;&lt;value&gt;...e</c>
    /// </remarks>
    public const byte DictionaryBeginCharacter = (byte)'d';

    /// <summary>
    /// Marks the termination of a Bencode integer, list, or dictionary.
    /// </summary>
    public const byte TerminationCharacter = (byte)'e';

    /// <summary>
    /// The minimum ASCII digit allowed in numeric fields.
    /// </summary>
    /// <remarks>
    /// Corresponds to the character <c>'0'</c>.
    /// </remarks>
    public const byte MinNumberCharacter = (byte)'0';

    /// <summary>
    /// The maximum ASCII digit allowed in numeric fields.
    /// </summary>
    /// <remarks>
    /// Corresponds to the character <c>'9'</c>.
    /// </remarks>
    public const byte MaxNumberCharacter = (byte)'9';

    /// <summary>
    /// Separates the length prefix of a Bencode string from its byte content.
    /// </summary>
    /// <remarks>
    /// Encoded strings have the form:
    /// <c>&lt;length&gt;:&lt;bytes&gt;</c>
    /// </remarks>
    public const byte StringPaddingCharacter = (byte)':';
}
