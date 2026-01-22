using Jordiware.BencodeDotNet.Builders;
using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet;

/// <summary>
/// Provides synchronous and asynchronous decoding of Bencode-encoded data
/// into strongly-typed <see cref="IBobject"/> representations.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Bdecoder"/> supports decoding from in-memory buffers
/// (<see cref="byte[]"/>, <see cref="ReadOnlySpan{T}"/>, and strings)
/// as well as streaming sources via <see cref="Stream"/> using
/// <see cref="System.IO.Pipelines.PipeReader"/>.
/// </para>
/// <para>
/// Decoding is performed in a single forward pass using
/// <see cref="SequenceReader{T}"/> and a stack of <see cref="BobjectBuilder"/>
/// instances, ensuring linear-time parsing with no backtracking.
/// </para>
/// <para>
/// The decoder enforces strict Bencode validation rules, including:
/// <list type="bullet">
/// <item><description>Exactly one top-level object</description></item>
/// <item><description>No trailing data after the root object</description></item>
/// <item><description>Proper container termination</description></item>
/// <item><description>Maximum nesting depth limits</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class Bdecoder
{
    private readonly BencodeOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bdecoder"/> class
    /// with the specified decoding options.
    /// </summary>
    /// <param name="options">
    /// Optional decoding options controlling validation behavior,
    /// such as maximum nesting depth.
    /// If <c>null</c>, default options are used.
    /// </param>
    public Bdecoder(BencodeOptions? options = default)
    {
        _options = options ?? new();
    }

    /// <summary>
    /// Decodes a complete Bencode object from a byte array.
    /// </summary>
    /// <param name="bytes">
    /// A byte array containing exactly one Bencode-encoded value.
    /// </param>
    /// <returns>
    /// The decoded <see cref="IBobject"/> instance.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown if the input does not contain a valid Bencode object,
    /// contains multiple top-level objects, or includes trailing data.
    /// </exception>
    public IBobject Decode(byte[] bytes)
    {
        var rom = new ReadOnlyMemory<byte>(bytes);
        return Decode(rom);
    }

    /// <summary>
    /// Decodes a complete Bencode object from a string using the specified encoding.
    /// </summary>
    /// <param name="s">
    /// The string containing Bencode-encoded data.
    /// </param>
    /// <param name="encoding">
    /// The text encoding used to convert the string into bytes.
    /// </param>
    /// <returns>
    /// The decoded <see cref="IBobject"/> instance.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown if the encoded data does not represent a valid Bencode object
    /// or contains trailing data.
    /// </exception>
    public IBobject Decode(string s, Encoding encoding)
    {
        var bytes = encoding.GetBytes(s);
        var rom = new ReadOnlyMemory<byte>(bytes);
        return Decode(rom);
    }

    /// <summary>
    /// Decodes a complete Bencode object from a contiguous span of bytes.
    /// </summary>
    /// <param name="data">
    /// A read-only span containing exactly one Bencode-encoded value.
    /// </param>
    /// <returns>
    /// The decoded <see cref="IBobject"/> instance.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown if the input does not represent a valid Bencode object
    /// or contains trailing data.
    /// </exception>
    public IBobject Decode(ReadOnlySpan<byte> data)
    {
        var rom = new ReadOnlyMemory<byte>(data.ToArray());
        return Decode(rom);
    }

    private IBobject Decode(ReadOnlyMemory<byte> rom)
    {
        var stack = new Stack<BobjectBuilder>();

        var sequence = new ReadOnlySequence<byte>(rom);
        var reader = new SequenceReader<byte>(sequence);

        if (!TryParseBencode(ref reader, ref stack, out var value) || stack.Count != 0)
            throw new FormatException("Incomplete or invalid bencode object");

        if (reader.Remaining > 0)
            throw new FormatException("Trailing data after top-level object");

        _options.Validate(value!);

        return value!;
    }

    /// <summary>
    /// Decodes a Bencode-encoded byte array into a CLR value of type <typeparamref name="TResult" />.
    /// </summary>
    /// <typeparam name="TResult">
    /// The CLR type to deserialize the decoded Bencode value into.
    /// </typeparam>
    /// <param name="bytes">
    /// A byte array containing a complete Bencode-encoded value.
    /// </param>
    /// <returns>
    /// The deserialized CLR value of type <typeparamref name="TResult" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="bytes"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// No Bencode serializer is registered or declared for <typeparamref name="TResult" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The decoded Bencode value cannot be deserialized into <typeparamref name="TResult" />.
    /// </exception>
    public TResult Decode<TResult>(byte[] bytes)
    {
        var rom = new ReadOnlyMemory<byte>(bytes);
        return Decode<TResult>(rom);
    }

    /// <summary>
    /// Decodes a Bencode-encoded string into a CLR value of type <typeparamref name="TResult" />.
    /// </summary>
    /// <typeparam name="TResult">
    /// The CLR type to deserialize the decoded Bencode value into.
    /// </typeparam>
    /// <param name="s">
    /// A string containing a complete Bencode-encoded value.
    /// </param>
    /// <param name="encoding">
    /// The character encoding used to convert the string into its byte representation.
    /// </param>
    /// <returns>
    /// The deserialized CLR value of type <typeparamref name="TResult" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="s"/> or <paramref name="encoding"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// No Bencode serializer is registered or declared for <typeparamref name="TResult" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The decoded Bencode value cannot be deserialized into <typeparamref name="TResult" />.
    /// </exception>
    public TResult Decode<TResult>(string s, Encoding encoding)
    {
        var bytes = encoding.GetBytes(s);
        var rom = new ReadOnlyMemory<byte>(bytes);
        return Decode<TResult>(rom);
    }

    /// <summary>
    /// Decodes a Bencode-encoded byte span into a CLR value of type <typeparamref name="TResult" />.
    /// </summary>
    /// <typeparam name="TResult">
    /// The CLR type to deserialize the decoded Bencode value into.
    /// </typeparam>
    /// <param name="data">
    /// A span containing a complete Bencode-encoded value.
    /// </param>
    /// <returns>
    /// The deserialized CLR value of type <typeparamref name="TResult" />.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// No Bencode serializer is registered or declared for <typeparamref name="TResult" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The decoded Bencode value cannot be deserialized into <typeparamref name="TResult" />.
    /// </exception>
    public TResult Decode<TResult>(ReadOnlySpan<byte> data)
    {
        var rom = new ReadOnlyMemory<byte>(data.ToArray());
        return Decode<TResult>(rom);
    }

    private TResult Decode<TResult>(ReadOnlyMemory<byte> rom)
    {
        if (!BencodeSerializer.TryGetSerializerForType(typeof(TResult), out var serializer) || serializer is null)
            throw new NotSupportedException($"No Bencode serializer is registered or declared for type '{typeof(TResult)}'.");

        var decoded = Decode(rom);
        if (!serializer.TryDeserialize(decoded, out var result))
            throw new InvalidOperationException($"Decoded Bencode value cannot be deserialized into '{typeof(TResult)}'.");

        return (TResult)result!;
    }

    /// <summary>
    /// Decodes a Bencode-encoded byte array into a CLR value using the specified serializer.
    /// </summary>
    /// <typeparam name="TResult">
    /// The CLR type to deserialize the decoded Bencode value into.
    /// </typeparam>
    /// <typeparam name="TBobject">
    /// The expected Bencode object type produced by the decoder.
    /// </typeparam>
    /// <param name="bytes">
    /// The byte array containing the Bencode-encoded data.
    /// </param>
    /// <param name="serializer">
    /// The serializer responsible for deserializing the decoded Bencode object into
    /// a <typeparamref name="TResult"/> instance.
    /// </param>
    /// <returns>
    /// The deserialized <typeparamref name="TResult"/> value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="bytes"/> or <paramref name="serializer"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The decoded Bencode value cannot be deserialized into <typeparamref name="TResult"/>,
    /// or the serializer produced a <see langword="null"/> result.
    /// </exception>
    public TResult Decode<TResult, TBobject>(byte[] bytes, BencodeSerializer<TResult, TBobject> serializer)
        where TBobject : IBobject
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));

        if (serializer is null)
            throw new ArgumentNullException(nameof(serializer));

        var rom = new ReadOnlyMemory<byte>(bytes);
        return Decode(rom, serializer);
    }

    /// <summary>
    /// Decodes a Bencode-encoded string into a CLR value using the specified serializer
    /// and character encoding.
    /// </summary>
    /// <typeparam name="TResult">
    /// The CLR type to deserialize the decoded Bencode value into.
    /// </typeparam>
    /// <typeparam name="TBobject">
    /// The expected Bencode object type produced by the decoder.
    /// </typeparam>
    /// <param name="s">
    /// The string containing the Bencode-encoded data.
    /// </param>
    /// <param name="encoding">
    /// The character encoding used to convert the string into bytes.
    /// </param>
    /// <param name="serializer">
    /// The serializer responsible for deserializing the decoded Bencode object into
    /// a <typeparamref name="TResult"/> instance.
    /// </param>
    /// <returns>
    /// The deserialized <typeparamref name="TResult"/> value.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="s"/> is <see langword="null"/>, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="encoding"/> or <paramref name="serializer"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The decoded Bencode value cannot be deserialized into <typeparamref name="TResult"/>,
    /// or the serializer produced a <see langword="null"/> result.
    /// </exception>
    public TResult Decode<TResult, TBobject>(string s, Encoding encoding, BencodeSerializer<TResult, TBobject> serializer)
        where TBobject : IBobject
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentException("Input string cannot be null, empty, or whitespace.", nameof(s));
        if (encoding is null)
            throw new ArgumentNullException(nameof(encoding));

        if (serializer is null)
            throw new ArgumentNullException(nameof(serializer));

        var bytes = encoding.GetBytes(s);
        var rom = new ReadOnlyMemory<byte>(bytes);
        return Decode(rom, serializer);
    }

    /// <summary>
    /// Decodes a Bencode-encoded byte span into a CLR value using the specified serializer.
    /// </summary>
    /// <typeparam name="TResult">
    /// The CLR type to deserialize the decoded Bencode value into.
    /// </typeparam>
    /// <typeparam name="TBobject">
    /// The expected Bencode object type produced by the decoder.
    /// </typeparam>
    /// <param name="data">
    /// A read-only span containing the Bencode-encoded data.
    /// </param>
    /// <param name="serializer">
    /// The serializer responsible for deserializing the decoded Bencode object into
    /// a <typeparamref name="TResult"/> instance.
    /// </param>
    /// <returns>
    /// The deserialized <typeparamref name="TResult"/> value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="serializer"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The decoded Bencode value cannot be deserialized into <typeparamref name="TResult"/>,
    /// or the serializer produced a <see langword="null"/> result.
    /// </exception>
    public TResult Decode<TResult, TBobject>(ReadOnlySpan<byte> data, BencodeSerializer<TResult, TBobject> serializer)
        where TBobject : IBobject
    {
        if (serializer is null)
            throw new ArgumentNullException(nameof(serializer));

        var rom = new ReadOnlyMemory<byte>(data.ToArray());
        return Decode(rom, serializer);
    }

    private TResult Decode<TResult, TBobject>(ReadOnlyMemory<byte> rom, BencodeSerializer<TResult, TBobject> serializer)
        where TBobject : IBobject
    {
        var decoded = Decode(rom);
        if (decoded is not TBobject bobject || !serializer.TryDeserialize(bobject, out var result))
            throw new InvalidOperationException($"Decoded Bencode value cannot be deserialized into '{typeof(TResult)}'.");

        if (result is null)
            throw new InvalidOperationException($"The serializer '{serializer.GetType()}' produced a null {typeof(TResult)} object.");

        return result;
    }

    /// <summary>
    /// Asynchronously decodes a complete Bencode object from a file.
    /// </summary>
    /// <param name="filePath">
    /// The path to the file containing Bencode-encoded data.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to cancel the decode operation.
    /// </param>
    /// <returns>
    /// A task that completes with the decoded <see cref="IBobject"/>.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown if the file does not contain exactly one valid Bencode object
    /// or contains trailing data.
    /// </exception>
    public async Task<IBobject> DecodeAsync(string filePath, CancellationToken ct = default)
    {
        using var stream = File.OpenRead(filePath);
        return await DecodeAsync(stream, ct);
    }

    /// <summary>
    /// Asynchronously decodes a Bencode-encoded file into a CLR value of type
    /// <typeparamref name="TResult" />.
    /// </summary>
    /// <typeparam name="TResult">
    /// The CLR type to deserialize the decoded Bencode value into.
    /// </typeparam>
    /// <param name="filePath">
    /// The path to a file containing a complete Bencode-encoded value.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to cancel the asynchronous decode operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous decode operation. The task result
    /// contains the deserialized CLR value of type <typeparamref name="TResult" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="filePath"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="filePath"/> is empty or consists only of whitespace.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// No Bencode serializer is registered or declared for <typeparamref name="TResult" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The decoded Bencode value cannot be deserialized into <typeparamref name="TResult" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled via <paramref name="ct"/>.
    /// </exception>
    public async Task<TResult> DecodeAsync<TResult>(string filePath, CancellationToken ct = default)
    {
        if (!BencodeSerializer.TryGetSerializerForType(typeof(TResult), out var serializer) || serializer is null)
            throw new NotSupportedException($"No Bencode serializer is registered or declared for type '{typeof(TResult)}'.");

        using var stream = File.OpenRead(filePath);
        var decoded = await DecodeAsync(stream, ct);
        if (!serializer.TryDeserialize(decoded, out var result))
            throw new InvalidOperationException($"Decoded Bencode value cannot be deserialized into '{typeof(TResult)}'.");

        return (TResult)result!;
    }

    /// <summary>
    /// Asynchronously decodes a Bencode-encoded file into a CLR value using the specified serializer.
    /// </summary>
    /// <typeparam name="TResult">
    /// The CLR type to deserialize the decoded Bencode value into.
    /// </typeparam>
    /// <typeparam name="TBobject">
    /// The expected Bencode object type produced by the decoder.
    /// </typeparam>
    /// <param name="filePath">
    /// The path to the file containing the Bencode-encoded data.
    /// </param>
    /// <param name="serializer">
    /// The serializer responsible for deserializing the decoded Bencode object into
    /// a <typeparamref name="TResult"/> instance.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the asynchronous decode operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous decode operation. The task result contains
    /// the deserialized <typeparamref name="TResult"/> value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="serializer"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The decoded Bencode value cannot be deserialized into <typeparamref name="TResult"/>,
    /// or the serializer produced a <see langword="null"/> result.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled via <paramref name="ct"/>.
    /// </exception>
    public async Task<TResult> DecodeAsync<TResult, TBobject>(string filePath, BencodeSerializer<TResult, TBobject> serializer, CancellationToken ct = default)
        where TBobject : IBobject
    {
        if (serializer is null)
            throw new ArgumentNullException(nameof(serializer));

        using var stream = File.OpenRead(filePath);
        var decoded = await DecodeAsync(stream, ct);
        if (decoded is not TBobject bobject || !serializer.TryDeserialize(bobject, out var result))
            throw new InvalidOperationException($"Decoded Bencode value cannot be deserialized into '{typeof(TResult)}'.");

        if (result is null)
            throw new InvalidOperationException($"The serializer '{serializer.GetType()}' produced a null {typeof(TResult)} object.");

        return result;
    }

    /// <summary>
    /// Asynchronously decodes a complete Bencode object from a readable stream.
    /// </summary>
    /// <param name="stream">
    /// A readable stream containing Bencode-encoded data.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to cancel the decode operation.
    /// </param>
    /// <returns>
    /// A task that completes with the decoded <see cref="IBobject"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the stream does not support reading.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if the stream does not contain exactly one valid Bencode object
    /// or contains trailing data.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses <see cref="PipeReader"/> to incrementally process
    /// the input stream without requiring it to be fully buffered in memory.
    /// </para>
    /// <para>
    /// Parsing state is preserved across reads, allowing correct handling
    /// of arbitrarily segmented input.
    /// </para>
    /// </remarks>
    public async Task<IBobject> DecodeAsync(Stream stream, CancellationToken ct = default)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanRead)
            throw new ArgumentException("Stream can not be read");

        var stack = new Stack<BobjectBuilder>();

        var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
        IBobject? bobject = default;
        ReadResult result = default!;

        try
        {
            while (!result.IsCompleted)
            {
                result = await reader.ReadAsync(ct);

                var seqReader = new SequenceReader<byte>(result.Buffer);
                if (TryParseBencode(ref seqReader, ref stack, out var element))
                {
                    if (bobject is not null)
                        throw new FormatException("Multiple top-level bencode objects");

                    bobject = element;

                    if (seqReader.Remaining > 0)
                        throw new FormatException("Trailing data after top-level object");
                }
                else if (bobject is not null && seqReader.Remaining > 0)
                {
                    throw new FormatException("Trailing data after top-level object");
                }

                reader.AdvanceTo(seqReader.Position, result.Buffer.End);
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }

        if (bobject is null)
            throw new FormatException("Incomplete or invalid bencode object");

        _options.Validate(bobject);

        return bobject;
    }

    /// <summary>
    /// Asynchronously decodes a Bencode-encoded stream into a CLR value of type
    /// <typeparamref name="TResult" />.
    /// </summary>
    /// <typeparam name="TResult">
    /// The CLR type to deserialize the decoded Bencode value into.
    /// </typeparam>
    /// <param name="stream">
    /// A readable stream containing a complete Bencode-encoded value.
    /// </param>
    /// <param name="ct">
    /// A cancellation token used to cancel the asynchronous decode operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous decode operation. The task result
    /// contains the deserialized CLR value of type <typeparamref name="TResult" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stream"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// No Bencode serializer is registered or declared for <typeparamref name="TResult" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The decoded Bencode value cannot be deserialized into <typeparamref name="TResult" />.
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled via <paramref name="ct"/>.
    /// </exception>
    public async Task<TResult> DecodeAsync<TResult>(Stream stream, CancellationToken ct = default)
    {
        if (!BencodeSerializer.TryGetSerializerForType(typeof(TResult), out var serializer) || serializer is null)
            throw new NotSupportedException($"No Bencode serializer is registered or declared for type '{typeof(TResult)}'.");

        var decoded = await DecodeAsync(stream, ct);
        if (!serializer.TryDeserialize(decoded, out var result))
            throw new InvalidOperationException($"Decoded Bencode value cannot be deserialized into '{typeof(TResult)}'.");

        return (TResult)result!;
    }

    /// <summary>
    /// Asynchronously decodes a Bencode-encoded stream into a CLR value using the specified serializer.
    /// </summary>
    /// <typeparam name="TResult">
    /// The CLR type to deserialize the decoded Bencode value into.
    /// </typeparam>
    /// <typeparam name="TBobject">
    /// The expected Bencode object type produced by the decoder.
    /// </typeparam>
    /// <param name="stream">
    /// The stream containing the Bencode-encoded data.
    /// </param>
    /// <param name="serializer">
    /// The serializer responsible for deserializing the decoded Bencode object into
    /// a <typeparamref name="TResult"/> instance.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the asynchronous decode operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous decode operation. The task result contains
    /// the deserialized <typeparamref name="TResult"/> value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="serializer"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The decoded Bencode value cannot be deserialized into <typeparamref name="TResult"/>,
    /// or the serializer produced a <see langword="null"/> result.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled via <paramref name="ct"/>.
    /// </exception>
    public async Task<TResult> DecodeAsync<TResult, TBobject>(Stream stream, BencodeSerializer<TResult, TBobject> serializer, CancellationToken ct = default)
        where TBobject : IBobject
    {
        if (serializer is null)
            throw new ArgumentNullException(nameof(serializer));

        var decoded = await DecodeAsync(stream, ct);
        if (decoded is not TBobject bobject || !serializer.TryDeserialize(bobject, out var result))
            throw new InvalidOperationException($"Decoded Bencode value cannot be deserialized into '{typeof(TResult)}'.");

        if (result is null)
            throw new InvalidOperationException($"The serializer '{serializer.GetType()}' produced a null {typeof(TResult)} object.");

        return result;
    }

    private bool TryParseBencode(ref SequenceReader<byte> reader, ref Stack<BobjectBuilder> stack, out IBobject? value)
    {
        value = null;

        while (reader.TryRead(out byte b))
        {
            if (stack.TryPeek(out var builder))
            {
                if (builder is BintegerBuilder ib)
                {
                    if (TryReadBintegerByte(ref ib, b, ref stack, out value))
                    {
                        if (value is not null)
                            return true;
                        continue;
                    }
                }

                if (builder is BstringBuilder sb)
                {
                    if (TryReadBstringByte(ref sb, b, ref stack, out value))
                    {
                        if (value is not null)
                            return true;
                        continue;
                    }
                    throw new FormatException($"Unexpected character {(char)b}");
                }
            }

            switch (b)
            {
                case Bencode.TerminationCharacter:
                    if (stack.Count == 0)
                        throw new FormatException("Unexpected 'e'");

                    builder = stack.Pop();
                    var completed = builder.ToBobject();

                    if (AttachOrReturn(completed, ref stack, out value))
                        return true;
                    break;
                case Bencode.IntegerBeginCharacter:
                    stack.Push(new BintegerBuilder());
                    break;
                case Bencode.ListBeginCharacter:
                    if (stack.Count >= _options.MaxDepth)
                        throw new InvalidOperationException("Maximum nesting depth exceeded");

                    stack.Push(new BlistBuilder());
                    break;
                case Bencode.DictionaryBeginCharacter:
                    if (stack.Count >= _options.MaxDepth)
                        throw new InvalidOperationException("Maximum nesting depth exceeded");

                    stack.Push(new BdictionaryBuilder());
                    break;
                case >= Bencode.MinNumberCharacter and <= Bencode.MaxNumberCharacter:
                    var strBuilder = new BstringBuilder();
                    strBuilder.PushLengthDigit(b);
                    stack.Push(strBuilder);
                    break;
                default:
                    throw new FormatException($"Unexpected character {(char)b}");
            }
        }

        return false;
    }

    private bool TryReadBintegerByte(ref BintegerBuilder bintegerBuilder, byte b, ref Stack<BobjectBuilder> stack, out IBobject? value)
    {
        value = null;
        switch (b)
        {
            case (byte)'-':
                bintegerBuilder.IsPositive = false;
                return true;
            case Bencode.TerminationCharacter:
                var builder = stack.Pop();
                var completed = builder.ToBobject();
                AttachOrReturn(completed, ref stack, out value);
                return true;
            case >= Bencode.MinNumberCharacter and <= Bencode.MaxNumberCharacter:
                bintegerBuilder.PushDigit(b);
                return true;
        }
        return false;
    }

    private bool TryReadBstringByte(ref BstringBuilder bstringBuilder, byte b, ref Stack<BobjectBuilder> stack, out IBobject? value)
    {
        value = null;
        if (b is >= Bencode.MinNumberCharacter and <= Bencode.MaxNumberCharacter && !bstringBuilder.IsLengthFinished)
        {
            bstringBuilder.PushLengthDigit(b);

            return true;
        }
        if (bstringBuilder.IsLengthFinished && !bstringBuilder.IsCompleted)
        {
            bstringBuilder.PushByte(b);

            TryCloseStringBuilder(ref bstringBuilder, ref stack, out value);
            return true;
        }
        if (b == Bencode.StringPaddingCharacter)
        {
            if (bstringBuilder.IsLengthFinished)
                throw new FormatException($"Unexpected character {(char)b}");

            bstringBuilder.FinishLength();

            TryCloseStringBuilder(ref bstringBuilder, ref stack, out value);
            return true;
        }
        return false;
    }

    private bool TryCloseStringBuilder(ref BstringBuilder bstringBuilder, ref Stack<BobjectBuilder> stack, out IBobject? value)
    {
        value = null;
        if (bstringBuilder.IsCompleted)
        {
            var sb = (BstringBuilder)stack.Pop();
            var bstring = (Bstring)sb.ToBobject();

            if (AttachOrReturn(bstring, ref stack, out value))
                return true;
        }
        return false;
    }

    private bool AttachOrReturn(IBobject obj, ref Stack<BobjectBuilder> stack, out IBobject? value)
    {
        value = null;

        if (stack.Count == 0)
        {
            value = obj;
            return true;
        }

        AttachToParent(stack.Peek(), obj);
        return false;
    }

    private void AttachToParent(BobjectBuilder parent, IBobject obj)
    {
        switch (parent)
        {
            case BlistBuilder lb:
                lb.PushObject(obj);
                break;
            case BdictionaryBuilder db:
                if (db.IsExpectingKey)
                {
                    if (obj is Bstring bstring)
                        db.PushKey(bstring);
                    else
                        throw new InvalidOperationException("String key expected");

                    break;
                }

                if (db.IsExpectingValue)
                {
                    db.PushValue(obj);
                    break;
                }

                throw new InvalidOperationException("Unexpected builder state");
            default:
                throw new InvalidOperationException("Unexpected object");
        }
    }
}
