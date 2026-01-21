using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet;

/// <summary>
/// Provides facilities for encoding CLR objects into their Bencode representation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Bencoder"/> is responsible for orchestrating the encoding process by:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>Resolving an appropriate Bencode serializer for the runtime type of the input object.</description>
///   </item>
///   <item>
///     <description>Invoking the serializer to produce an <see cref="IBobject"/> representation.</description>
///   </item>
///   <item>
///     <description>Validating the resulting Bencode object against the configured <see cref="BencodeOptions"/>.</description>
///   </item>
/// </list>
/// </remarks>
public sealed class Bencoder
{
    private readonly BencodeOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bencoder"/> class using the specified encoding options.
    /// </summary>
    /// <param name="options">
    /// The <see cref="BencodeOptions"/> instance that defines validation limits for encoded payloads.
    /// If <see langword="null"/>, a new instance with default values is used.
    /// </param>
    public Bencoder(BencodeOptions? options = default)
    {
        _options = options ?? new();
    }

    /// <summary>
    /// Encodes a CLR object into its corresponding Bencode representation.
    /// </summary>
    /// <param name="value">
    /// The object to encode. The runtime type of this object is used to resolve
    /// an appropriate Bencode serializer.
    /// </param>
    /// <returns>
    /// An <see cref="IBobject"/> representing the Bencode-encoded form of <paramref name="value"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when no Bencode serializer is registered or declared for the runtime type
    /// of <paramref name="value"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the resolved serializer fails to serialize the object, produces a
    /// <see langword="null"/> Bencode result, or when the encoded output violates the
    /// configured <see cref="BencodeOptions"/> constraints.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method enforces all validation rules defined by the configured <see cref="BencodeOptions"/>,
    /// including maximum payload length, maximum nesting depth, and container size limits.
    /// </para>
    /// <para>
    /// The returned <see cref="IBobject"/> is guaranteed to be non-<see langword="null"/> and
    /// to satisfy all validation constraints if this method completes successfully.
    /// </para>
    /// </remarks>
    public IBobject Encode(object? value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var type = value.GetType();

        if (!BencodeSerializer.TryGetSerializerForType(type, out var serializer) || serializer is null)
            throw new NotSupportedException($"No Bencode serializer is registered or declared for type '{type}'.");

        if (!serializer.TrySerialize(value, out var result))
            throw new InvalidOperationException($"The serializer '{serializer.GetType()}' failed to serialize an instance of '{type}'.");

        if (result is null)
            throw new InvalidOperationException($"The serializer '{serializer.GetType()}' produced a null Bencode object.");

        _options.Validate(result);

        return result;
    }

    /// <summary>
    /// Encodes a CLR object into its corresponding Bencode representation using a
    /// explicitly provided serializer.
    /// </summary>
    /// <typeparam name="TType">
    /// The CLR type of the value being encoded.
    /// </typeparam>
    /// <typeparam name="TTarget">
    /// The concrete <see cref="IBobject"/> type produced by the serializer.
    /// </typeparam>
    /// <param name="value">
    /// The object to encode.
    /// </param>
    /// <param name="serializer">
    /// The <see cref="BencodeSerializer{TOrigin, TTarget}"/> instance to use for encoding
    /// <paramref name="value"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IBobject"/> representing the Bencode-encoded form of <paramref name="value"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> or <paramref name="serializer"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the provided serializer fails to serialize the value, produces a
    /// <see langword="null"/> Bencode object, or when the resulting payload violates
    /// the configured <see cref="BencodeOptions"/> constraints.
    /// </exception>
    public IBobject Encode<TType, TTarget>(TType value, BencodeSerializer<TType, TTarget> serializer)
        where TTarget : IBobject
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (serializer is null)
            throw new ArgumentNullException(nameof(serializer));

        if (!serializer.TrySerialize(value, out var result))
            throw new InvalidOperationException($"The serializer '{serializer.GetType()}' failed to serialize an instance of '{typeof(TType)}'.");

        if (result is null)
            throw new InvalidOperationException($"The serializer '{serializer.GetType()}' produced a null Bencode object.");

        _options.Validate(result);

        return result;
    }
}
