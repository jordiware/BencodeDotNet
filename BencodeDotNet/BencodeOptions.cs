using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet;

/// <summary>
/// Defines structural and size constraints applied during Bencode encoding and validation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BencodeOptions"/> represents a validation policy that limits resource usage
/// and protects against malformed or malicious Bencode payloads.
/// </para>
/// <para>
/// These options are enforced by decoders, serializers, and explicit calls to
/// <see cref="Validate(IBobject)"/>.
/// </para>
/// </remarks>
public struct BencodeOptions
{
    /// <summary>
    /// The default maximum allowed nesting depth for Bencode containers.
    /// </summary>
    /// <remarks>
    /// The root object has a depth of <c>1</c>.
    /// </remarks>
    public const int DefaultMaxDepth = 1024;

    /// <summary>
    /// The default maximum encoded payload size, in bytes.
    /// </summary>
    /// <remarks>
    /// This value represents the total number of bytes required to encode the
    /// complete Bencode object graph.
    /// </remarks>
    public const int DefaultMaxPayloadLength = 64 * 1024 * 1024;

    /// <summary>
    /// The default maximum number of items allowed in a single list or dictionary.
    /// </summary>
    public const int DefaultMaxContainerItems = 1024;

    /// <summary>
    /// Gets the maximum allowed nesting depth for Bencode containers.
    /// </summary>
    /// <remarks>
    /// Lists and dictionaries contribute to nesting depth.
    /// Primitive values do not.
    /// </remarks>
    public readonly int MaxDepth;

    /// <summary>
    /// Gets the maximum allowed encoded payload size, in bytes.
    /// </summary>
    /// <remarks>
    /// This limit applies to the fully encoded representation of the Bencode object,
    /// including container delimiters and length prefixes.
    /// </remarks>
    public readonly int MaxPayloadLength;

    /// <summary>
    /// Gets the maximum number of items allowed in any single list or dictionary.
    /// </summary>
    /// <remarks>
    /// For dictionaries, this value applies to the number of key/value pairs.
    /// </remarks>
    public readonly int MaxContainerItems;

    /// <summary>
    /// Initializes a new <see cref="BencodeOptions"/> instance with the specified constraints.
    /// </summary>
    /// <param name="maxDepth">
    /// The maximum allowed nesting depth for lists and dictionaries.
    /// </param>
    /// <param name="maxPayloadLength">
    /// The maximum allowed encoded payload size, in bytes.
    /// </param>
    /// <param name="maxContainerItems">
    /// The maximum number of items allowed in any list or dictionary.
    /// </param>
    /// <remarks>
    /// <para>
    /// All parameters are optional and default to conservative, safe limits.
    /// </para>
    /// <para>
    /// No validation is performed at construction time; invalid configurations
    /// will be enforced when validation occurs.
    /// </para>
    /// </remarks>
    public BencodeOptions(int maxDepth = DefaultMaxDepth,
                          int maxPayloadLength = DefaultMaxPayloadLength,
                          int maxContainerItems = DefaultMaxContainerItems)
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
    /// Thrown when any configured validation constraint is violated.
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
