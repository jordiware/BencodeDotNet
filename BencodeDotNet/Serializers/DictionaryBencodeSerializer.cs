using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Utils;
using System.Collections.Immutable;
using System.IO.Pipelines;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for
/// <see cref="IDictionary{TKey, TValue}"/> values using the Bencode
/// dictionary representation (<see cref="Bdictionary"/>).
/// </summary>
/// <remarks>
/// Both keys and values are serialized using serializers resolved via
/// <see cref="BencodeSerializer"/>. Dictionary keys are encoded as
/// <see cref="Bstring"/> instances, as required by the Bencode specification.
/// </remarks>
public sealed class DictionaryBencodeSerializer<TKey, TValue> : ReferenceTypeBencodeSerializer<IDictionary<TKey, TValue>, Bdictionary>
{
    private readonly BencodeOptions _options;

    /// <summary>
    /// Initializes a new <see cref="DictionaryBencodeSerializer{TKey, TValue}"/> instance
    /// using the default <see cref="BencodeOptions"/> configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This constructor creates a serializer configured with a new
    /// <see cref="BencodeOptions"/> instance using default constraint values
    /// and text encoding.
    /// </para>
    /// <para>
    /// The resulting serializer applies the standard validation and encoding
    /// policies defined by <see cref="BencodeOptions"/> when serializing or
    /// deserializing dictionary values.
    /// </para>
    /// </remarks>
    public DictionaryBencodeSerializer()
    {
        _options = new();
    }

    /// <summary>
    /// Initializes a new <see cref="DictionaryBencodeSerializer{TKey, TValue}"/> instance
    /// using the specified <see cref="BencodeOptions"/> configuration.
    /// </summary>
    /// <param name="options">
    /// The <see cref="BencodeOptions"/> instance that defines validation limits
    /// and encoding behavior for this serializer.
    /// </param>
    /// <remarks>
    /// <para>
    /// The provided <paramref name="options"/> instance is retained and used
    /// for all serialization and deserialization operations performed by this
    /// serializer.
    /// </para>
    /// </remarks>
    public DictionaryBencodeSerializer(BencodeOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Attempts to serialize an <see cref="IDictionary{TKey, TValue}"/> into a
    /// <see cref="Bdictionary"/>.
    /// </summary>
    /// <remarks>
    /// Dictionary keys and values are serialized using serializers resolved via
    /// <see cref="BencodeSerializer"/>. Serialized keys are always encoded as
    /// <see cref="Bstring"/> instances by writing their full Bencode binary
    /// representation.
    /// </remarks>
    /// <param name="input">The dictionary to serialize.</param>
    /// <param name="output">
    /// When this method returns, contains the resulting <see cref="Bdictionary"/>
    /// if serialization succeeded; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the dictionary was successfully serialized;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TrySerialize(IDictionary<TKey, TValue> input, out Bdictionary? output)
    {
        output = default;
        if (input is null)
            return false;

        if (input.Count == 0)
        {
            output = new Bdictionary(ImmutableDictionary<Bstring, IBobject>.Empty);
            return true;
        }

        if (!BencodeSerializer.TryGetSerializerForType(typeof(TKey), _options, out var keySerializer))
            return false;

        if (!BencodeSerializer.TryGetSerializerForType(typeof(TValue), _options, out var valueSerializer))
            return false;

        var result = new Dictionary<Bstring, IBobject>();
        foreach (var (key, value) in input)
        {
            if (key is null || value is null)
                return false;

            if (!keySerializer!.TrySerialize(key, out var serializedKey))
                return false;

            if (!valueSerializer!.TrySerialize(value, out var serializedValue))
                return false;

            var bkey = new Bstring(serializedKey!.ToBinaryEncoding());

            if (!result.TryAdd(bkey, serializedValue!))
                return false;
        }

        output = new Bdictionary(result);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="Bdictionary"/> into an
    /// <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// Dictionary keys are decoded by interpreting their raw byte contents as a
    /// complete Bencode value and deserializing the resulting object into
    /// <typeparamref name="TKey"/>. Values are deserialized directly using the
    /// resolved value serializer.
    /// </remarks>
    /// <param name="input">The <see cref="Bdictionary"/> to deserialize.</param>
    /// <param name="output">
    /// When this method returns, contains the resulting dictionary if
    /// deserialization succeeded; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the dictionary was successfully deserialized;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Bdictionary input, out IDictionary<TKey, TValue>? output)
    {
        output = default;
        if (input is null)
            return false;

        if (input.Count == 0)
        {
            output = new Dictionary<TKey, TValue>();
            return true;
        }

        if (!BencodeSerializer.TryGetSerializerForType(typeof(TKey), _options, out var keySerializer))
            return false;

        if (!BencodeSerializer.TryGetSerializerForType(typeof(TValue), _options, out var valueSerializer))
            return false;

        var result = new Dictionary<TKey, TValue>();
        foreach (var (bkey, bvalue) in input)
        {
            if (!TryDecodeKey(bkey, _options, out var decodedKey))
                return false;

            if (!keySerializer!.TryDeserialize(decodedKey!, out var key))
                return false;

            if (!valueSerializer!.TryDeserialize(bvalue, out var value))
                return false;

            if (key is null || value is null)
                return false;

            if (!result.TryAdd((TKey)key, (TValue)value))
                return false;
        }

        output = result;
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a dictionary to the provided <see cref="PipeWriter"/> in Bencode format.
    /// </summary>
    /// <param name="input">
    /// The dictionary to serialize. Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the serialized dictionary will be written.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    /// <remarks>
    /// Keys are serialized using the resolved serializer for <typeparamref name="TKey"/> 
    /// and converted to Bencode strings. The resulting dictionary is sorted lexicographically
    /// by these Bencode-encoded keys, as required by the Bencode specification.  
    /// Values are serialized using the resolved serializer for <typeparamref name="TValue"/>.  
    /// Null keys or values are skipped.  
    /// </remarks>
    public override async Task WriteToPipeAsync(IDictionary<TKey, TValue> input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        if (!BencodeSerializer.TryGetSerializerForType(typeof(TKey), _options, out var keySerializer) || keySerializer is null)
            throw new NotSupportedException($"No Bencode serializer is registered for element type '{typeof(TKey)}'.");

        if (!BencodeSerializer.TryGetSerializerForType(typeof(TValue), _options, out var valueSerializer) || valueSerializer is null)
            throw new NotSupportedException($"No Bencode serializer is registered for element type '{typeof(TValue)}'.");

        var orderedDictionary = input.Where(kvp => kvp.Key is not null && kvp.Value is not null).Select(kvp =>
        {
            keySerializer!.TrySerialize(kvp.Key!, out var serializedKey);
            return (new Bstring(serializedKey!.ToBinaryEncoding()), kvp.Key!);
        }).ToDictionary().ToImmutableSortedDictionary();

        await writer.WriteAsync(new byte[] { Bencode.DictionaryBeginCharacter }, cancellationToken);

        foreach (var bkey in orderedDictionary.Keys)
        {
            var value = input[orderedDictionary[bkey]];

            await BencodePipeWriter.WriteBytesAsync(bkey.ToBinaryEncoding(), writer, cancellationToken);
            await valueSerializer.WriteToPipeAsync(value!, writer, cancellationToken);
        }

        await writer.WriteAsync(new byte[] { Bencode.TerminationCharacter }, cancellationToken);
    }

    /// <summary>
    /// Decodes a dictionary key back into its original
    /// <see cref="IBobject"/> representation.
    /// </summary>
    /// <remarks>
    /// Dictionary keys are stored as raw byte strings. This method interprets
    /// the key contents as a complete Bencode value and decodes it using
    /// <see cref="BencodeDecoder"/> so it can be deserialized into
    /// <typeparamref name="TKey"/>.
    /// </remarks>
    private static bool TryDecodeKey(Bstring key, BencodeOptions options, out IBobject value)
    {
        value = default!;

        try
        {
            var decoder = new BencodeDecoder(options);
            value = decoder.Decode(key.Value);
            return true;
        }
        catch
        {
            value = default!;
            return false;
        }
    }
}
