using Jordiware.BencodeDotNet.Objects;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet;

public static partial class Bdecoder
{
    public static Bdecoder<MemoryStream> FromBytes(byte[] bytes, BdecodingOptions? options = default)
    {
        var stream = new MemoryStream(bytes);
        var decoder = new Bdecoder<MemoryStream>(ref stream, options);
        return decoder;
    }

    public static Bdecoder<MemoryStream> FromString(string s, Encoding encoding, BdecodingOptions? options = default)
    {
        var bytes = encoding.GetBytes(s);
        var stream = new MemoryStream(bytes);
        var decoder = new Bdecoder<MemoryStream>(ref stream, options);
        return decoder;
    }

    public static Bdecoder<FileStream> FromFile(string filePath, BdecodingOptions? options = default)
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

public sealed class Bdecoder<TStream> : IDisposable where TStream : Stream
{
    private readonly TStream _stream;
    private readonly BdecodingOptions _options;
    private readonly Stack<Frame> _stack = new();

    public Bdecoder(ref TStream stream, BdecodingOptions? options = default)
    {
        if (!stream.CanRead)
            throw new ArgumentException("Stream can not be read");

        _stream = stream;
        _options = options ?? new();
    }

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
                break;
            }
            
            reader.AdvanceTo(seqReader.Position);
        }

        await reader.CompleteAsync();

        if (bobject is null)
            throw new FormatException("Incomplete or invalid bencode object");

        return bobject;
    }

    private bool TryParseBencode(ref SequenceReader<byte> reader, out IBobject? value)
    {
        value = default!;

        while (reader.Consumed < reader.Length)
        {
            if (!reader.TryPeek(out byte prefix))
            {
                return false;
            }

            switch (prefix)
            {
                case Bencode.TerminationCharacter:
                    reader.Advance(1);

                    if (_stack.Count == 0)
                        throw new FormatException("Unexpected 'e'");

                    var frame = _stack.Pop();
                    var completed = default(IBobject);
                    if (frame is ListFrame lf)
                    {
                        completed = new Blist(lf.Items);
                    }
                    else if (frame is DictFrame df)
                    {
                        if (df.PendingKey is not null)
                            throw new FormatException("Dictionary missing value");

                        completed = new Bdictionary(df.Items);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    if (_stack.Count == 0)
                    {
                        value = completed;
                        return true;
                    }
                    else
                    {
                        AttachToParent(_stack.Peek(), completed);
                    }
                    break;
                case Bencode.IntegerBeginCharacter:
                    if (!TryReadInteger(ref reader, out var i))
                        return false;

                    if (AttachOrReturn(i, out value))
                        return true;
                    break;
                case Bencode.ListBeginCharacter:
                    BeginList(ref reader);
                    break;
                case Bencode.DictionaryBeginCharacter:
                    BeginDictionary(ref reader);
                    break;
                case >= Bencode.MinNumberCharacter and <= Bencode.MaxNumberCharacter:
                    if (!TryReadString(ref reader, out var s))
                        return false;

                    if (AttachOrReturn(s, out value))
                        return true;
                    break;
                default:
                    throw new FormatException($"Invalid prefix {(char)prefix}");
            }
        }

        return false;
    }

    private bool TryReadInteger(ref SequenceReader<byte> reader, out IBobject value)
    {
        value = default!;
        var checkpoint = reader.Position;

        if (!reader.TryRead(out byte prefix) ||
            prefix != Bencode.IntegerBeginCharacter)
            throw new FormatException("Invalid integer start");

        if (!reader.TryReadTo(out ReadOnlySpan<byte> digits, Bencode.TerminationCharacter))
        {
            reader.Rewind(reader.Consumed - reader.Sequence.GetOffset(checkpoint));
            return false;
        }

        // --- strict bencode validation ---
        if (digits.Length == 0)
            throw new FormatException("Empty integer");

        if (digits.Length > 1 && digits[0] == Bencode.MinNumberCharacter)
            throw new FormatException("Leading zero");

        if (digits.Length > 1 &&
            digits[0] == (byte)'-' &&
            digits[1] == Bencode.MinNumberCharacter)
            throw new FormatException("Negative zero");

        long number = 0;
        bool negative = false;
        int i = 0;

        if (digits[0] == (byte)'-')
        {
            negative = true;
            i = 1;
        }

        for (; i < digits.Length; i++)
        {
            byte c = digits[i];
            if (c < Bencode.MinNumberCharacter || c > Bencode.MaxNumberCharacter)
                throw new FormatException();

            number = checked(number * 10 + (c - Bencode.MinNumberCharacter));
        }

        if (negative)
            number = -number;

        value = new Binteger(number);
        return true;
    }

    private bool TryReadString(ref SequenceReader<byte> reader, out IBobject value)
    {
        value = default!;
        var checkpoint = reader.Position;

        if (!reader.TryReadTo(out ReadOnlySpan<byte> lengthBytes, Bencode.StringPaddingCharacter))
        {
            reader.Rewind(reader.Consumed - reader.Sequence.GetOffset(checkpoint));
            return false;
        }

        if (lengthBytes.Length > 1 && lengthBytes[0] == (byte)'0')
            throw new FormatException("Leading zero in string length");

        int length = 0;
        foreach (var b in lengthBytes)
        {
            if (b < Bencode.MinNumberCharacter || b > Bencode.MaxNumberCharacter)
                throw new FormatException("Invalid string length");

            length = checked(length * 10 + (b - Bencode.MinNumberCharacter));
        }

        if (length > _options.MaxStringLength)
            throw new FormatException("String length exceeds limit");

        if (reader.Remaining < length)
            return false;

        ReadOnlySequence<byte> strBytes =
            reader.Sequence.Slice(reader.Position, length);

        reader.Advance(length);

        value = new Bstring(strBytes.ToArray());
        return true;
    }

    private void BeginList(ref SequenceReader<byte> reader)
    {
        if (_stack.Count > _options.MaxDepth)
        {
            throw new FormatException("Maximum nesting depth exceeded");
        }

        reader.Advance(1); // consume 'l'
        _stack.Push(new ListFrame()
        {
            Items = new()
        });
    }

    private void BeginDictionary(ref SequenceReader<byte> reader)
    {
        if (_stack.Count > _options.MaxDepth)
        {
            throw new FormatException("Maximum nesting depth exceeded");
        }

        reader.Advance(1); // consume 'd'
        _stack.Push(new DictFrame()
        {
            Items = new(),
            PendingKey = null,
        });
    }

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

    private void AttachToParent(Frame frame, IBobject obj)
    {
        switch (frame)
        {
            case ListFrame lf:
                if (lf.Items.Count >= _options.MaxContainerItems)
                    throw new FormatException("List item limit exceeded");

                lf.Items.Add(obj);
                break;
            case DictFrame df:
                if (df.Items.Count >= _options.MaxContainerItems)
                    throw new FormatException("Dictionary item limit exceeded");

                if (df.PendingKey is null)
                {
                    if (obj is not Bstring key)
                        throw new FormatException("Dictionary key must be string");

                    if (df.LastKey is not null
                        && key.CompareTo(df.LastKey) <= 0)
                        throw new FormatException("Dictionary keys must be sorted");

                    df.PendingKey = key;
                }
                else
                {
                    df.Items[df.PendingKey] = obj;
                    df.LastKey = df.PendingKey;
                    df.PendingKey = null;
                }
                break;
        }
    }

    public void Dispose()
    {
        _stack.Clear();
    }

    #region Utility types
    private abstract class Frame;

    private sealed class ListFrame : Frame
    {
        public List<IBobject> Items = default!;
    }

    private sealed class DictFrame : Frame
    {
        public Dictionary<Bstring, IBobject> Items = default!;
        public Bstring? PendingKey;
        public Bstring? LastKey;
    }
    #endregion
}
