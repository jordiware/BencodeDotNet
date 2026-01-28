using Jordiware.BencodeDotNet.Builders;
using Jordiware.BencodeDotNet.Objects;
using System.Buffers;

namespace Jordiware.BencodeDotNet;

/// <summary>
/// Provides the core Bencode parsing logic used by derived types.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BencodeIO"/> is an abstract base class that encapsulates the parsing logic
/// for Bencoded data. It exposes a <see cref="TryParseBencode"/> method for derived classes
/// to incrementally parse bytes from a <see cref="SequenceReader{Byte}"/> and construct
/// <see cref="IBObject"/> instances using builder types.
/// </para>
/// <para>
/// Derived types are responsible for managing the input source (e.g., stream) and for
/// consuming the parsed objects. <see cref="BencodeIO"/> does not handle asynchronous
/// enumeration or top-level object emission.
/// </para>
/// </remarks>
public abstract class BencodeIO
{
    protected readonly BencodeOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeIO"/> base class with the specified options.
    /// </summary>
    /// <param name="options">
    /// Optional <see cref="BencodeOptions"/> instance that defines maximum depth,
    /// validation limits, and other parsing constraints. If <c>null</c>, a default
    /// instance is created.
    /// </param>
    protected BencodeIO(BencodeOptions? options = default)
    {
        _options = options ?? new();
    }

    /// <summary>
    /// Attempts to parse Bencoded data from a <see cref="SequenceReader{Byte}"/> and constructs
    /// <see cref="IBObject"/> instances as they are completed.
    /// </summary>
    /// <param name="reader">The sequence reader supplying the bytes to parse.</param>
    /// <param name="stack">A stack of <see cref="BobjectBuilder"/> instances used for nested structures.</param>
    /// <param name="value">
    /// When this method returns <c>true</c>, contains the top-level <see cref="IBObject"/>
    /// that was fully parsed; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a top-level object was successfully parsed and is ready to be yielded;
    /// otherwise, <c>false</c>. Partial parsing may have occurred, in which case
    /// the stack reflects the current state.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is intended to be called repeatedly as more bytes are available.
    /// It handles integers, strings, lists, and dictionaries according to the Bencode
    /// specification.
    /// </para>
    /// <para>
    /// If a top-level object is completed, <paramref name="value"/> will be set and the
    /// caller can yield it. Nested objects are attached to their parents automatically
    /// via the builder stack.
    /// </para>
    /// <para>
    /// Parsing respects the maximum nesting depth defined by <see cref="_options.MaxDepth"/>.
    /// Unexpected characters or malformed input result in <see cref="FormatException"/>
    /// or <see cref="InvalidOperationException"/> being thrown.
    /// </para>
    /// </remarks>
    private protected bool TryParseBencode(ref SequenceReader<byte> reader, ref Stack<BobjectBuilder> stack, out IBObject? value)
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
                    throw new BencodeFormatException($"Unexpected character {(char)b}");
                }
            }

            switch (b)
            {
                case Bencode.TerminationCharacter:
                    if (stack.Count == 0)
                        throw new BencodeFormatException("Unexpected 'e'");

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
                        throw new BencodeValidationException("Maximum nesting depth exceeded");

                    stack.Push(new BlistBuilder());
                    break;
                case Bencode.DictionaryBeginCharacter:
                    if (stack.Count >= _options.MaxDepth)
                        throw new BencodeValidationException("Maximum nesting depth exceeded");

                    stack.Push(new BdictionaryBuilder());
                    break;
                case >= Bencode.MinNumberCharacter and <= Bencode.MaxNumberCharacter:
                    var strBuilder = new BstringBuilder();
                    strBuilder.PushLengthDigit(b);
                    stack.Push(strBuilder);
                    break;
                default:
                    throw new BencodeFormatException($"Unexpected character {(char)b}");
            }
        }

        return false;
    }

    private bool TryReadBintegerByte(ref BintegerBuilder bintegerBuilder, byte b, ref Stack<BobjectBuilder> stack, out IBObject? value)
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

    private bool TryReadBstringByte(ref BstringBuilder bstringBuilder, byte b, ref Stack<BobjectBuilder> stack, out IBObject? value)
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
                throw new BencodeFormatException($"Unexpected character {(char)b}");

            bstringBuilder.FinishLength();

            TryCloseStringBuilder(ref bstringBuilder, ref stack, out value);
            return true;
        }
        return false;
    }

    private bool TryCloseStringBuilder(ref BstringBuilder bstringBuilder, ref Stack<BobjectBuilder> stack, out IBObject? value)
    {
        value = null;
        if (bstringBuilder.IsCompleted)
        {
            var sb = (BstringBuilder)stack.Pop();
            var bstring = (BString)sb.ToBobject();

            if (AttachOrReturn(bstring, ref stack, out value))
                return true;
        }
        return false;
    }

    private bool AttachOrReturn(IBObject obj, ref Stack<BobjectBuilder> stack, out IBObject? value)
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

    private void AttachToParent(BobjectBuilder parent, IBObject obj)
    {
        switch (parent)
        {
            case BlistBuilder lb:
                lb.PushObject(obj);
                break;
            case BdictionaryBuilder db:
                if (db.IsExpectingKey)
                {
                    if (obj is BString bstring)
                        db.PushKey(bstring);
                    else
                        throw new BencodeFormatException("String key expected");

                    break;
                }

                if (db.IsExpectingValue)
                {
                    db.PushValue(obj);
                    break;
                }

                throw new BencodeFormatException("Unexpected builder state");
            default:
                throw new BencodeFormatException("Unexpected object");
        }
    }
}
