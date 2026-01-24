using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Jordiware.BencodeDotNet;

/// <summary>
/// Provides a forward-only, streaming reader for Bencoded data that emits
/// completed top-level <see cref="IBobject"/> instances from a continuous stream.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BencodeReader"/> is designed for scenarios where a stream contains
/// multiple consecutive Bencoded objects rather than a single root value.
/// The reader incrementally consumes the underlying <see cref="Stream"/> and
/// yields each fully parsed top-level object as soon as it is completed.
/// </para>
/// <para>
/// The reader enforces the validation and safety constraints defined by the
/// associated <see cref="BencodeOptions"/> instance. These options apply
/// consistently across all objects read from the stream.
/// </para>
/// <para>
/// This type does not own the provided <see cref="Stream"/> and will not dispose it.
/// Stream lifetime management remains the responsibility of the caller.
/// </para>
/// <para>
/// Instances of <see cref="BencodeReader"/> are not thread-safe. A single reader
/// instance should not be used concurrently by multiple consumers.
/// </para>
/// </remarks>
public sealed class BencodeReader
{
    private readonly BencodeOptions _options;

    /// <summary>
    /// Initializes a new <see cref="BencodeReader"/> with the specified options.
    /// </summary>
    /// <param name="options">
    /// The <see cref="BencodeOptions"/> that define validation limits and decoding
    /// behavior for all objects read by this instance. If <see langword="null"/>,
    /// a new default options instance is created.
    /// </param>
    public BencodeReader(BencodeOptions? options = default)
    {
        _options = options ?? new();
    }

    /// <summary>
    /// Asynchronously reads a sequence of top-level Bencoded objects from the
    /// specified stream.
    /// </summary>
    /// <param name="stream">
    /// The input <see cref="Stream"/> containing one or more consecutively encoded
    /// Bencode objects.
    /// </param>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> that can be used to cancel the read operation.
    /// </param>
    /// <returns>
    /// An asynchronous sequence of <see cref="IBobject"/> instances, each representing
    /// a fully parsed top-level Bencode object.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The returned sequence is forward-only and must be consumed sequentially.
    /// Enumeration stops when the end of the stream is reached.
    /// </para>
    /// <para>
    /// Each object is yielded as soon as it is fully parsed; the reader does not
    /// buffer the entire stream or require all objects to be present in memory.
    /// </para>
    /// </remarks>
    public async IAsyncEnumerable<IBobject> ReadAsync(Stream stream,
                                                      [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (stream is null) 
            throw new ArgumentNullException(nameof(stream));

        throw new NotImplementedException();
    }

    /// <summary>
    /// Asynchronously reads a sequence of top-level Bencoded objects from the
    /// specified stream and deserializes each object to the specified target type.
    /// </summary>
    /// <typeparam name="TType">
    /// The target type to deserialize each Bencode object into.
    /// </typeparam>
    /// <param name="stream">
    /// The input <see cref="Stream"/> containing one or more consecutively encoded
    /// Bencode objects.
    /// </param>
    /// <param name="serializer">
    /// An optional <see cref="BencodeSerializer{TOrigin, TTarget}"/> used to
    /// deserialize each parsed <see cref="IBobject"/> into <typeparamref name="TType"/>.
    /// If <see langword="null"/>, a compatible serializer is resolved automatically
    /// using the configured <see cref="BencodeOptions"/>.
    /// </param>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> that can be used to cancel the read operation.
    /// </param>
    /// <returns>
    /// An asynchronous sequence of values of type <typeparamref name="TType"/>,
    /// each corresponding to a successfully deserialized top-level Bencode object.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if no compatible Bencode serializer is registered or declared for
    /// <typeparamref name="TType"/>.
    /// </exception>
    /// <exception cref="SerializationException">
    /// Thrown if a parsed Bencode object cannot be deserialized to
    /// <typeparamref name="TType"/> using the resolved serializer.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is a projection over <see cref="ReadAsync(Stream, CancellationToken)"/>.
    /// Parsing and validation are performed once, and each resulting
    /// <see cref="IBobject"/> is deserialized sequentially.
    /// </para>
    /// <para>
    /// Enumeration stops when the end of the stream is reached.
    /// </para>
    /// </remarks>
    public async IAsyncEnumerable<TType> ReadAsync<TType>(Stream stream, 
                                                          BencodeSerializer<TType, IBobject>? serializer = default,
                                                          [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (serializer is null)
        {
            if (!BencodeSerializer.TryGetSerializerForType(typeof(TType), _options, out var resolvedSerializer) 
                || resolvedSerializer is null
                || resolvedSerializer is not BencodeSerializer<TType, IBobject> typeSerializer)
                throw new NotSupportedException($"No Bencode serializer is registered or declared for type '{typeof(TType)}'.");

            serializer = typeSerializer;
        }

        await foreach (var bobject in ReadAsync(stream, ct))
        {
            if (!serializer.TryDeserialize(bobject, out var value))
                throw new SerializationException($"Failed to deserialize Bencode object to type '{typeof(TType)}'.");

            yield return value!;
        }
    }
}
