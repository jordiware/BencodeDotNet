namespace Jordiware.BencodeDotNet;

/// <summary>
/// Specifies limits and safety constraints applied during Bencode decoding.
/// </summary>
/// <remarks>
/// <para>
/// These options are used to protect the decoder from malformed or
/// malicious input by enforcing upper bounds on nesting depth, string
/// sizes, and container cardinality.
/// </para>
/// <para>
/// All limits are enforced during decoding and violations result in
/// runtime exceptions.
/// </para>
/// </remarks>
public struct BencodeOptions()
{
    /// <summary>
    /// Gets the maximum allowed nesting depth for lists and dictionaries.
    /// </summary>
    /// <remarks>
    /// This limit applies to the combined depth of nested containers
    /// (lists and dictionaries). Exceeding this value results in an
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    public readonly int MaxDepth = 1024;

    /// <summary>
    /// Gets the maximum allowed length, in bytes, of a Bencode string.
    /// </summary>
    /// <remarks>
    /// This limit applies to the declared byte length of the string, not
    /// its decoded character count.
    /// </remarks>
    public readonly int MaxStringLength = 64 * 1024 * 1024;

    /// <summary>
    /// Gets the maximum number of items allowed in a list or dictionary.
    /// </summary>
    /// <remarks>
    /// This limit applies to:
    /// <list type="bullet">
    /// <item><description>List elements</description></item>
    /// <item><description>Dictionary key/value pairs</description></item>
    /// </list>
    /// Exceeding this value results in an <see cref="InvalidOperationException"/>.
    /// </remarks>
    public readonly int MaxContainerItems = 1024;

    /// <summary>
    /// Initializes a new <see cref="BencodeOptions"/> instance with custom limits.
    /// </summary>
    /// <param name="maxDepth">The maximum allowed nesting depth.</param>
    /// <param name="maxStringLength">The maximum allowed string length in bytes.</param>
    /// <param name="maxContainerItems">The maximum number of elements allowed in a container.</param>
    public BencodeOptions(int maxDepth, int maxStringLength, int maxContainerItems) : this()
    {
        MaxDepth = maxDepth;
        MaxStringLength = maxStringLength;
        MaxContainerItems = maxContainerItems;
    }
}
