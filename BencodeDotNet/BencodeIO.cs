using Jordiware.BencodeDotNet.Builders;
using Jordiware.BencodeDotNet.Objects;
using System.Buffers;

namespace Jordiware.BencodeDotNet;

public abstract class BencodeIO
{
    protected readonly BencodeOptions _options;

    protected BencodeIO(BencodeOptions? options = default)
    {
        _options = options ?? new();
    }

    private protected bool TryParseBencode(ref SequenceReader<byte> reader, ref Stack<BobjectBuilder> stack, out IBobject? value)
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
