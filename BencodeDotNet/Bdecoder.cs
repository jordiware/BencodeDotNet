using Jordiware.BencodeDotNet.Objects;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet;

public static class Bdecoder
{
    public static Bdecoder<MemoryStream> FromBytes(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        var decoder = new Bdecoder<MemoryStream>(ref stream);
        return decoder;
    }

    public static Bdecoder<MemoryStream> FromString(string s, Encoding encoding)
    {
        var bytes = encoding.GetBytes(s);
        var stream = new MemoryStream(bytes);
        var decoder = new Bdecoder<MemoryStream>(ref stream);
        return decoder;
    }

    public static Bdecoder<FileStream> FromFile(string filePath)
    {
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        try
        {
            var decoder = new Bdecoder<FileStream>(ref stream);
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
    private readonly Stack<Frame> _stack = new();

    public Bdecoder(ref TStream stream)
    {
        if (!stream.CanRead)
            throw new ArgumentException("Stream can not be read");

        _stream = stream;
    }

    public async Task<IBobject> DecodeAsync(CancellationToken ct = default)
    {
        _stack.Clear();

        IBobject? bobject = default;
        var reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));

        while (true)
        {
            var result = await reader.ReadAsync(ct);
            var buffer = result.Buffer;

            while (TryParseBencode(ref buffer, out var element))
            {
                bobject = element;
                break;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
                break;
        }

        await reader.CompleteAsync();

        if (bobject is null)
            throw new FormatException("Incomplete or invalid bencode object");

        return bobject;
    }

    private bool TryParseBencode(ref ReadOnlySequence<byte> buffer, out IBobject value)
    {
        value = default!;
        var reader = new SequenceReader<byte>(buffer);

        while (true)
        {
            if (!reader.TryPeek(out byte prefix))
                return false;

            // --- termination ---
            if (prefix == Bencode.TerminationCharacter)
            {
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
                    if (!df.ExpectingKey)
                        throw new FormatException("Dictionary missing value");

                    completed = new Bdictionary(df.Items);
                }
                else
                {
                    throw new InvalidOperationException();
                }

                if (_stack.Count == 0)
                {
                    buffer = buffer.Slice(reader.Position);
                    value = completed;
                    return true;
                }

                AttachToParent(_stack.Peek(), completed);
                continue;
            }

            // --- integer ---
            if (prefix == Bencode.IntegerBeginCharacter)
            {
                if (!TryReadInteger(ref reader, out var i))
                    return false;

                AttachOrReturn(ref buffer, reader.Position, i, out value);
                if (value is not null)
                    return true;

                continue;
            }

            // --- string ---
            if (prefix is >= Bencode.MinNumberCharacter and <= Bencode.MaxNumberCharacter)
            {
                if (!TryReadString(ref reader, out var s))
                    return false;

                AttachOrReturn(ref buffer, reader.Position, s, out value);
                if (value is not null)
                    return true;

                continue;
            }

            // --- list ---
            if (prefix == Bencode.ListBeginCharacter)
            {
                BeginList(ref reader);
                continue;
            }

            // --- dictionary ---
            if (prefix == Bencode.DictionaryBeginCharacter)
            {
                BeginDictionary(ref reader);
                continue;
            }

            throw new FormatException($"Invalid prefix {(char)prefix}");
        }
    }

    private bool TryReadInteger(ref SequenceReader<byte> reader, out IBobject value)
    {
        value = default!;

        if (!reader.TryRead(out byte prefix) ||
            prefix != Bencode.IntegerBeginCharacter)
            throw new FormatException("Invalid integer start");

        if (!reader.TryReadTo(out ReadOnlySpan<byte> digits, Bencode.TerminationCharacter))
            return false;

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

            number = number * 10 + (c - Bencode.MinNumberCharacter);
        }

        if (negative) 
            number = -number;

        value = new Binteger(number);
        return true;
    }

    private bool TryReadString(ref SequenceReader<byte> reader, out IBobject value)
    {
        value = default!;

        if (!reader.TryReadTo(out ReadOnlySpan<byte> lengthBytes, Bencode.StringPaddingCharacter))
            return false;

        if (lengthBytes.Length > 1 && lengthBytes[0] == (byte)'0')
            throw new FormatException("Leading zero in string length");

        if (!int.TryParse(Encoding.ASCII.GetString(lengthBytes), out int length) || length < 0)
            throw new FormatException("Invalid string length");

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
        reader.Advance(1); // consume 'l'
        _stack.Push(new ListFrame()
        {
            Items = new()
        });
    }

    private void BeginDictionary(ref SequenceReader<byte> reader)
    {
        reader.Advance(1); // consume 'd'
        _stack.Push(new DictFrame()
        {
            Items = new(),
            LastKey = null,
            ExpectingKey = true
        });
    }

    private bool AttachOrReturn(ref ReadOnlySequence<byte> buffer,
                                SequencePosition pos,
                                IBobject obj,
                                out IBobject? value)
    {
        value = null;

        if (_stack.Count == 0)
        {
            buffer = buffer.Slice(pos);
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
                lf.Items.Add(obj);
                break;

            case DictFrame df:
                if (df.ExpectingKey)
                {
                    if (obj is not Bstring key)
                        throw new FormatException("Dictionary key must be string");

                    if (df.LastKey is not null &&
                        key.CompareTo(df.LastKey) <= 0)
                        throw new FormatException("Dictionary keys must be sorted");

                    df.LastKey = key;
                    df.ExpectingKey = false;
                }
                else
                {
                    df.Items[df.LastKey!] = obj;
                    df.ExpectingKey = true;
                }
                break;
        }
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    private abstract class Frame;

    private sealed class ListFrame : Frame
    {
        public List<IBobject> Items = default!;
    }

    private sealed class DictFrame : Frame
    {
        public Dictionary<Bstring, IBobject> Items = default!;
        public Bstring? LastKey;
        public bool ExpectingKey;
    }
}
