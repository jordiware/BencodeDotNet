using Jordiware.BencodeDotNet.Objects;

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
    /// Gets the maximum allowed length, in bytes, of a Bencode object.
    /// </summary>
    /// <remarks>
    /// This limit applies to the declared byte length of the object, not
    /// its decoded character count.
    /// </remarks>
    public readonly int MaxPayloadLength = 64 * 1024 * 1024;

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
    /// <param name="maxPayloadLength">The maximum allowed string length in bytes.</param>
    /// <param name="maxContainerItems">The maximum number of elements allowed in a container.</param>
    public BencodeOptions(int maxDepth, int maxPayloadLength, int maxContainerItems) : this()
    {
        MaxDepth = maxDepth;
        MaxPayloadLength = maxPayloadLength;
        MaxContainerItems = maxContainerItems;
    }

    /// <summary>
    /// Validates a Bencode object against the configured structural and size constraints.
    /// </summary>
    /// <param name="root">
    /// The root <see cref="IBobject"/> to validate.
    /// </param>
    /// <remarks>
    /// <para>
    /// <see cref="Validate(IBobject)"/> enforces all policy constraints defined by this
    /// <see cref="BencodeOptions"/> instance, including maximum payload length, maximum
    /// nesting depth, and maximum container cardinality.
    /// </para>
    /// <para>
    /// The following constraints are enforced:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="MaxPayloadLength"/> — the total number of bytes required to encode
    ///       the Bencode object must not exceed this value.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="MaxDepth"/> — the maximum nesting depth of lists and dictionaries.
    ///       The root object has a depth of 1.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <see cref="MaxContainerItems"/> — the maximum number of items allowed in any
    ///       list or dictionary.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="root"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any configured validation constraint is violated, including payload
    /// length, nesting depth, or container size limits.
    /// </exception>
    public void Validate(IBobject root)
    {
        int encodedLength = 0;
        ValidateInternal(root, depth: 1, ref encodedLength);

        if (encodedLength > MaxPayloadLength)
            throw new InvalidOperationException($"Encoded payload length ({encodedLength}) exceeds the configured maximum ({MaxPayloadLength}).");
    }

    private void ValidateInternal(IBobject node, int depth, ref int encodedLength)
    {
        if (depth > MaxDepth)
            throw new InvalidOperationException($"Maximum Bencode depth ({MaxDepth}) exceeded.");

        switch (node)
        {
            case Blist list:
                if (list.Count > MaxContainerItems)
                    throw new InvalidOperationException($"Bencode list contains {list.Count} items, exceeding the configured maximum ({MaxContainerItems}).");

                encodedLength += 2;
                if (encodedLength > MaxPayloadLength)
                    throw new InvalidOperationException($"Encoded payload length ({encodedLength}) exceeds the configured maximum ({MaxPayloadLength}).");

                foreach (var item in list)
                    ValidateInternal(item, depth + 1, ref encodedLength);
                break;

            case Bdictionary dict:
                if (dict.Count > MaxContainerItems)
                    throw new InvalidOperationException($"Bencode dictionary contains {dict.Count} entries, exceeding the configured maximum ({MaxContainerItems}).");

                encodedLength += 2;
                if (encodedLength > MaxPayloadLength)
                    throw new InvalidOperationException($"Encoded payload length ({encodedLength}) exceeds the configured maximum ({MaxPayloadLength}).");

                foreach (var (key, value) in dict)
                {
                    encodedLength += key.GetEncodedLength();
                    if (encodedLength > MaxPayloadLength)
                        throw new InvalidOperationException($"Encoded payload length ({encodedLength}) exceeds the configured maximum ({MaxPayloadLength}).");

                    ValidateInternal(value, depth + 1, ref encodedLength);
                }
                break;

            default:
                encodedLength += node.GetEncodedLength();
                if (encodedLength > MaxPayloadLength)
                    throw new InvalidOperationException($"Encoded payload length ({encodedLength}) exceeds the configured maximum ({MaxPayloadLength}).");

                break;
        }
    }
}
