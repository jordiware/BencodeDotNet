using Jordiware.BencodeDotNet.Builders;
using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Buffers;
using System.IO.Pipelines;
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
public sealed class BencodeReader : BencodeIO
{
    /// <summary>
    /// Initializes a new <see cref="BencodeReader"/> with the specified options.
    /// </summary>
    /// <param name="options">
    /// The <see cref="BencodeOptions"/> that define validation limits and decoding
    /// behavior for all objects read by this instance. If <see langword="null"/>,
    /// a new default options instance is created.
    /// </param>
    public BencodeReader(BencodeOptions? options = default) : base(options)
    {
    }

    /// <summary>
    /// Asynchronously reads and decodes Bencode objects from the specified stream.
    /// </summary>
    /// <param name="stream">
    /// A readable stream containing one or more Bencode-encoded objects.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to cancel the asynchronous enumeration.
    /// </param>
    /// <returns>
    /// An asynchronous sequence of decoded <see cref="IBobject"/> instances, yielded
    /// as soon as each top-level object is fully parsed and validated.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="stream"/> does not support reading.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the input stream contains malformed Bencode data, when validation
    /// fails, or when the stream ends unexpectedly while an object is being parsed.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method incrementally reads from the stream and may yield multiple Bencode
    /// objects during a single enumeration. Objects are only yielded once fully parsed
    /// and validated according to the configured <see cref="BencodeOptions"/>.
    /// </para>
    /// <para>
    /// Enumeration completes when the end of the stream is reached. If the stream ends
    /// while a Bencode object is only partially read, a <see cref="FormatException"/> is thrown.
    /// </para>
    /// </remarks>
    public async IAsyncEnumerable<IBobject> ReadAsync(Stream stream, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (stream is null) 
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanRead)
            throw new ArgumentException("Stream can not be read");

        var stack = new Stack<BobjectBuilder>();

        var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
        ReadResult result = default!;

        try
        {
            while (!result.IsCompleted)
            {
                result = await reader.ReadAsync(ct);

                foreach (var element in ParseBuffer(result.Buffer, stack))
                {
                    try
                    {
                        _options.Validate(element);
                    }
                    catch
                    {
                        throw new FormatException("Validation failed for decoded object.");
                    }

                    yield return element;
                }

                reader.AdvanceTo(result.Buffer.End);
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }

        if (stack.Count != 0)
            throw new FormatException("Unexpected end of stream while parsing Bencode object.");
    }

    /// <summary>
    /// Asynchronously reads and decodes Bencode objects from the specified stream and
    /// deserializes them into values of type <typeparamref name="TType"/>.
    /// </summary>
    /// <typeparam name="TType">
    /// The target type to deserialize each decoded Bencode object into.
    /// </typeparam>
    /// <param name="stream">
    /// A readable stream containing one or more Bencode-encoded objects.
    /// </param>
    /// <param name="serializer">
    /// An optional serializer used to convert decoded <see cref="IBobject"/> instances
    /// into values of type <typeparamref name="TType"/>. If <c>null</c>, a serializer is
    /// resolved using the configured serializer discovery mechanism.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to cancel the asynchronous enumeration.
    /// </param>
    /// <returns>
    /// An asynchronous sequence of deserialized values of type <typeparamref name="TType"/>.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when no compatible serializer can be resolved for <typeparamref name="TType"/>.
    /// </exception>
    /// <exception cref="SerializationException">
    /// Thrown when a decoded Bencode object cannot be deserialized into
    /// <typeparamref name="TType"/>.
    /// </exception>
    public async IAsyncEnumerable<TType> ReadAsync<TType>(Stream stream, 
                                                          IBencodeSerializer? serializer = default,
                                                          [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanRead)
            throw new ArgumentException("Stream can not be read");

        if (serializer is null)
        {
            if (!BencodeSerializer.TryGetSerializerForType(typeof(TType), _options, out var resolvedSerializer) 
                || resolvedSerializer is null)
                throw new NotSupportedException($"No Bencode serializer is registered or declared for type '{typeof(TType)}'.");

            serializer = resolvedSerializer;
        }

        await foreach (var bobject in ReadAsync(stream, ct))
        {
            if (!serializer.TryDeserialize(bobject, out var value))
                throw new SerializationException($"Failed to deserialize Bencode object to type '{typeof(TType)}'.");

            yield return (TType)value!;
        }
    }

    private IEnumerable<IBobject> ParseBuffer(ReadOnlySequence<byte> buffer, Stack<BobjectBuilder> stack)
    {
        var seqReader = new SequenceReader<byte>(buffer);
        var results = new List<IBobject>();

        while (TryParseBencode(ref seqReader, ref stack, out var element))
        {
            if (element is not null)
                results.Add(element);
        }

        return results;
    }
}
