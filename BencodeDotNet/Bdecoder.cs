using Jordiware.BencodeDotNet.Builders;
using Jordiware.BencodeDotNet.Objects;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet;

public static partial class Bdecoder
{
    public static Bdecoder<MemoryStream> FromBytes(byte[] bytes, BencodeOptions? options = default)
    {
        var stream = new MemoryStream(bytes);
        var decoder = new Bdecoder<MemoryStream>(ref stream, options);
        return decoder;
    }

    public static Bdecoder<MemoryStream> FromString(string s, Encoding encoding, BencodeOptions? options = default)
    {
        var bytes = encoding.GetBytes(s);
        var stream = new MemoryStream(bytes);
        var decoder = new Bdecoder<MemoryStream>(ref stream, options);
        return decoder;
    }

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

public sealed class Bdecoder<TStream> : IDisposable where TStream : Stream
{
    private readonly TStream _stream;
    private readonly BencodeOptions _options;
    private readonly Stack<BobjectBuilder> _stack = new();

    public Bdecoder(ref TStream stream, BencodeOptions? options = default)
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

    public void Dispose()
    {
        _stack.Clear();
    }
}
