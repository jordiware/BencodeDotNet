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

    public static Bdecoder<FileStream> FromFile(string filePath)
    {
        var stream = new FileStream(filePath, FileMode.Open);
        var decoder = new Bdecoder<FileStream>(ref stream);
        return decoder;
    }
}

public sealed class Bdecoder<TStream> : IDisposable where TStream : Stream
{
    private readonly TStream _stream;

    public Bdecoder(ref TStream stream)
    {
        if (!stream.CanRead)
            throw new ArgumentException("Stream can not be read");

        _stream = stream;
    }

    public async Task<IBobject> DecodeAsync(CancellationToken ct = default)
    {
        IBobject? bobject = default;
        var reader = PipeReader.Create(_stream);

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
        return bobject!;
    }

    private bool TryParseBencode(ref ReadOnlySequence<byte> buffer, out IBobject value)
    {
        if (buffer.IsEmpty)
        {
            value = default!;
            return false;
        }

        byte prefix = buffer.FirstSpan[0];

        return prefix switch
        {
            Bencode.IntegerBeginCharacter => TryParseInteger(ref buffer, out value),
            Bencode.ListBeginCharacter => TryParseList(ref buffer, out value),
            Bencode.DictionaryBeginCharacter => TryParseDictionary(ref buffer, out value),
            >= Bencode.MinNumberCharacter and <= Bencode.MaxNumberCharacter => TryParseString(ref buffer, out value),
            _ => throw new FormatException("Invalid bencode data")
        };
    }

    private bool TryParseInteger(ref ReadOnlySequence<byte> buffer, out IBobject value)
    {
        value = default!;
        var reader = new SequenceReader<byte>(buffer);

        if (!reader.TryRead(out byte i) || i != Bencode.IntegerBeginCharacter)
            throw new FormatException();

        if (!reader.TryReadTo(out ReadOnlySpan<byte> digits, Bencode.TerminationCharacter))
            return false;

        long number = long.Parse(Encoding.ASCII.GetString(digits));
        value = new Binteger(number);

        buffer = buffer.Slice(reader.Position);
        return true;
    }

    private bool TryParseString(ref ReadOnlySequence<byte> buffer, out IBobject value)
    {
        value = default!;

        var reader = new SequenceReader<byte>(buffer);

        if (!reader.TryReadTo(out ReadOnlySpan<byte> lengthBytes, Bencode.StringPaddingCharacter))
            return false; // need more data

        if (!int.TryParse(Encoding.ASCII.GetString(lengthBytes), out int length))
            throw new FormatException("Invalid string length");

        if (reader.Remaining < length)
            return false; // string not fully available yet

        ReadOnlySequence<byte> strBytes = buffer.Slice(reader.Position, length);

        value = new Bstring(strBytes.ToArray());
        buffer = buffer.Slice(reader.Position).Slice(length);

        return true;
    }

    private bool TryParseList(ref ReadOnlySequence<byte> buffer, out IBobject value)
    {
        value = default!;

        var reader = new SequenceReader<byte>(buffer);

        // Need at least the 'l'
        if (!reader.TryRead(out byte start) || start != Bencode.ListBeginCharacter)
            throw new FormatException("Invalid list start");

        var items = new List<IBobject>();

        while (true)
        {
            // Need at least one byte to decide
            if (reader.End)
                return false;

            // End of list?
            if (reader.CurrentSpan[reader.CurrentSpanIndex] == Bencode.TerminationCharacter)
            {
                reader.Advance(1); // consume 'e'
                buffer = buffer.Slice(reader.Position);
                value = new Blist(items);
                return true;
            }

            // Parse next element
            ReadOnlySequence<byte> remaining = buffer.Slice(reader.Position);

            if (!TryParseBencode(ref remaining, out var element))
                return false;

            items.Add(element);

            // Advance reader to where the nested parser stopped
            reader = new SequenceReader<byte>(remaining);
        }
    }

    private bool TryParseDictionary(ref ReadOnlySequence<byte> buffer, out IBobject value)
    {
        value = default!;

        var reader = new SequenceReader<byte>(buffer);

        // Need at least the 'd'
        if (!reader.TryRead(out byte start) || start != Bencode.DictionaryBeginCharacter)
            throw new FormatException("Invalid dictionary start");

        var dict = new Dictionary<Bstring, IBobject>();

        while (true)
        {
            if (reader.End)
                return false;

            // End of dictionary?
            if (reader.CurrentSpan[reader.CurrentSpanIndex] == Bencode.TerminationCharacter)
            {
                reader.Advance(1); // consume 'e'
                buffer = buffer.Slice(reader.Position);
                value = new Bdictionary(dict);
                return true;
            }

            // --- Parse key (must be string) ---
            ReadOnlySequence<byte> keyBuffer = buffer.Slice(reader.Position);

            if (!TryParseString(ref keyBuffer, out var keyValue))
                return false;

            if (keyValue is not Bstring keyString)
                throw new FormatException("Dictionary key must be a string");

            // Advance reader past key
            reader = new SequenceReader<byte>(keyBuffer);

            // --- Parse value ---
            ReadOnlySequence<byte> valueBuffer = buffer.Slice(reader.Position);

            if (!TryParseBencode(ref valueBuffer, out var element))
                return false;

            dict[keyString] = element;

            // Advance reader past value
            reader = new SequenceReader<byte>(valueBuffer);
        }
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}
