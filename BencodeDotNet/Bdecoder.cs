using Jordiware.BencodeDotNet.Builders;
using Jordiware.BencodeDotNet.Objects;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet;

/// <summary>
/// Factory methods for creating <see cref="Bdecoder{TStream}"/> instances
/// from common input sources.
/// </summary>
/// <remarks>
/// These helpers create a readable stream from the provided input and
/// return a decoder configured to read from it. Stream ownership rules
/// depend on the underlying stream type.
/// </remarks>
public static partial class Bdecoder
{
    /// <summary>
    /// Creates a decoder that reads Bencode data from a byte array.
    /// </summary>
    /// <param name="bytes">The byte array containing Bencode-encoded data.</param>
    /// <param name="options">Optional decoding options.</param>
    /// <returns>A configured <see cref="Bdecoder{MemoryStream}"/>.</returns>
    public static Bdecoder<MemoryStream> FromBytes(byte[] bytes, BencodeOptions? options = default)
    {
        var stream = new MemoryStream(bytes);
        var decoder = new Bdecoder<MemoryStream>(ref stream, options);
        return decoder;
    }

    /// <summary>
    /// Creates a decoder that reads Bencode data from a string using
    /// the specified encoding.
    /// </summary>
    /// <param name="s">The string containing Bencode-encoded data.</param>
    /// <param name="encoding">The encoding used to convert the string to bytes.</param>
    /// <param name="options">Optional decoding options.</param>
    /// <returns>A configured <see cref="Bdecoder{MemoryStream}"/>.</returns>
    public static Bdecoder<MemoryStream> FromString(string s, Encoding encoding, BencodeOptions? options = default)
    {
        var bytes = encoding.GetBytes(s);
        var stream = new MemoryStream(bytes);
        var decoder = new Bdecoder<MemoryStream>(ref stream, options);
        return decoder;
    }

    /// <summary>
    /// Creates a decoder that reads Bencode data from a file.
    /// </summary>
    /// <param name="filePath">Path to the file containing Bencode data.</param>
    /// <param name="options">Optional decoding options.</param>
    /// <returns>A configured <see cref="Bdecoder{FileStream}"/>.</returns>
    /// <exception cref="IOException">
    /// Thrown if the file cannot be opened for reading.
    /// </exception>
    public static Bdecoder<FileStream> FromFile(string filePath, BencodeOptions? options = default)
    {
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        try
        {
            var decoder = new Bdecoder<FileStream>(ref stream, options);
            return decoder;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }
}

/// <summary>
/// Asynchronously decodes a single Bencode object from a readable stream.
/// </summary>
/// <typeparam name="TStream">
/// The concrete stream type used as the decoding source.
/// </typeparam>
/// <remarks>
/// <para>
/// The decoder processes the input as a forward-only byte stream and
/// constructs the corresponding Bencode object using an explicit builder
/// stack.
/// </para>
/// <para>
/// Decoding is performed incrementally using <see cref="PipeReader"/> and
/// does not require the entire input to be buffered in memory.
/// </para>
/// <para>
/// A single call to <see cref="DecodeAsync"/> decodes exactly one top-level
/// Bencode object. Trailing data is not consumed.
/// </para>
/// </remarks>
public sealed class Bdecoder<TStream> : IDisposable where TStream : Stream
{
    private readonly TStream _stream;
    private readonly BencodeOptions _options;
    private readonly Stack<BobjectBuilder> _stack = new();

    /// <summary>
    /// Initializes a new decoder that reads from the provided stream.
    /// </summary>
    /// <param name="stream">
    /// A readable stream containing Bencode-encoded data.
    /// </param>
    /// <param name="options">
    /// Optional decoding options; if <c>null</c>, default options are used.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if the stream does not support reading.
    /// </exception>
    public Bdecoder(ref TStream stream, BencodeOptions? options = default)
    {
        if (!stream.CanRead)
            throw new ArgumentException("Stream can not be read");

        _stream = stream;
        _options = options ?? new();
    }

    /// <summary>
    /// Asynchronously decodes a single Bencode object from the stream.
    /// </summary>
    /// <param name="ct">A cancellation token used to cancel the operation.</param>
    /// <returns>The decoded <see cref="IBobject"/>.</returns>
    /// <exception cref="FormatException">
    /// Thrown if the input does not represent a valid or complete Bencode object.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if decoding is cancelled.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method clears any previous decoder state and processes the stream
    /// until a complete top-level object has been produced.
    /// </para>
    /// <para>
    /// Nested structures are tracked using a stack of internal builders,
    /// enforcing maximum depth, container size limits, and ordering rules.
    /// </para>
    /// </remarks>
    public async Task<IBobject> DecodeAsync(CancellationToken ct = default)
    {
        _stack.Clear();

        var reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));

        IBobject? bobject = default;
        ReadResult result = default!;
        while (!result.IsCompleted)
        {
            result = await reader.ReadAsync(ct);
            if (result.IsCanceled)
                throw new OperationCanceledException();

            var seqReader = new SequenceReader<byte>(result.Buffer);
            if (TryParseBencode(ref seqReader, out var element))
            {
                ct.ThrowIfCancellationRequested();

                bobject = element;
            }

            reader.AdvanceTo(seqReader.Position);
        }

        await reader.CompleteAsync();

        if (bobject is null
            && _stack.TryPop(out var builder)
            && builder is BstringBuilder bstringBuilder
            && bstringBuilder.IsCompleted)
            bobject = bstringBuilder.ToBobject();

        if (bobject is null)
            throw new FormatException("Incomplete or invalid bencode object");

        return bobject;
    }

    /// <summary>
    /// Attempts to parse Bencode elements from the provided byte sequence.
    /// </summary>
    /// <param name="reader">
    /// A <see cref="SequenceReader{Byte}"/> positioned at the current read offset.
    /// </param>
    /// <param name="value">
    /// When this method returns <c>true</c>, contains the completed top-level object.
    /// </param>
    /// <returns>
    /// <c>true</c> if a complete top-level object has been decoded; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method drives the core decoding state machine. It interprets input bytes,
    /// updates the active builder stack, and assembles completed objects as soon as
    /// sufficient data becomes available.
    /// </para>
    /// <para>
    /// Partial input is supported; the reader position is advanced only for
    /// successfully consumed bytes.
    /// </para>
    /// </remarks>
    private bool TryParseBencode(ref SequenceReader<byte> reader, out IBobject? value)
    {
        value = null;

        while (reader.TryRead(out byte b))
        {
            if (_stack.TryPeek(out var builder))
            {
                switch (builder)
                {
                    case BstringBuilder sb:
                        if (b is >= Bencode.MinNumberCharacter and <= Bencode.MaxNumberCharacter && !sb.IsLengthFinished)
                        {
                            sb.PushLengthDigit(b);

                            continue;
                        }
                        if (b == Bencode.StringPaddingCharacter)
                        {
                            if (sb.IsLengthFinished)
                                throw new FormatException($"Unexpected character {(char)b}");

                            sb.FinishLength();

                            if (sb.IsCompleted)
                            {
                                var stringBuilder = (BstringBuilder)_stack.Pop();
                                var bstring = (Bstring)stringBuilder.ToBobject();

                                if (AttachOrReturn(bstring, out value))
                                    return true;
                            }

                            continue;
                        }
                        if (sb.IsLengthFinished && !sb.IsCompleted)
                        {
                            sb.PushByte(b);

                            if (sb.IsCompleted)
                            {
                                var stringBuilder = (BstringBuilder)_stack.Pop();
                                var bstring = (Bstring)stringBuilder.ToBobject();

                                if (AttachOrReturn(bstring, out value))
                                    return true;
                            }

                            continue;
                        }
                        break;
                    case BintegerBuilder ib:
                        if (b >= Bencode.MinNumberCharacter && b <= Bencode.MaxNumberCharacter)
                        {
                            ib.PushDigit(b);
                            continue;
                        }
                        if (b == (byte)'-')
                        {
                            ib.IsPositive = false;
                            continue;
                        }
                        break;
                }
            }


            switch (b)
            {
                case Bencode.TerminationCharacter:
                    if (_stack.Count == 0)
                        throw new FormatException("Unexpected 'e'");

                    builder = _stack.Pop();
                    var completed = builder.ToBobject();

                    if (AttachOrReturn(completed, out value))
                        return true;
                    break;
                case Bencode.IntegerBeginCharacter:
                    _stack.Push(new BintegerBuilder());
                    break;
                case Bencode.ListBeginCharacter:
                    if (_stack.Count >= _options.MaxDepth)
                        throw new InvalidOperationException("Maximum nesting depth exceeded");

                    _stack.Push(new BlistBuilder());
                    break;
                case Bencode.DictionaryBeginCharacter:
                    if (_stack.Count >= _options.MaxDepth)
                        throw new InvalidOperationException("Maximum nesting depth exceeded");

                    _stack.Push(new BdictionaryBuilder());
                    break;
                case >= Bencode.MinNumberCharacter and <= Bencode.MaxNumberCharacter:
                    var strBuilder = new BstringBuilder();
                    strBuilder.PushLengthDigit(b);
                    _stack.Push(strBuilder);
                    break;
                default:
                    throw new FormatException($"Unexpected character {(char)b}");
            }
        }

        return false;
    }

    /// <summary>
    /// Attaches a completed object to its parent builder or returns it
    /// as the final decoding result.
    /// </summary>
    /// <param name="obj">The completed object.</param>
    /// <param name="value">
    /// Receives the final decoded object if no parent builder exists.
    /// </param>
    /// <returns>
    /// <c>true</c> if decoding is complete; otherwise <c>false</c>.
    /// </returns>
    private bool AttachOrReturn(IBobject obj, out IBobject? value)
    {
        value = null;

        if (_stack.Count == 0)
        {
            value = obj;
            return true;
        }

        AttachToParent(_stack.Peek(), obj);
        return false;
    }

    /// <summary>
    /// Attaches a completed object to the specified parent builder.
    /// </summary>
    /// <param name="parent">The parent builder.</param>
    /// <param name="obj">The object to attach.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the object cannot be attached due to invalid builder state.
    /// </exception>
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

    /// <summary>
    /// Clears all internal decoder state.
    /// </summary>
    /// <remarks>
    /// This method does not dispose the underlying stream.
    /// </remarks>
    public void Dispose()
    {
        _stack.Clear();
    }
}
