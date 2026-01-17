using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

internal sealed class BstringBuilder : BobjectBuilder
{
    private long? _length = null;
    private long _offset = 0;
    private byte[]? _bytes = null;

    public BstringBuilder(BencodeOptions? options = default) : base(options)
    {
    }

    public bool IsLengthFinished => !ThrowIfDisposed() && _length.HasValue && _bytes is not null;
    public bool IsCompleted => !ThrowIfDisposed() && _length.HasValue && _offset == _length;

    public void PushLengthDigit(byte digit)
    {
        ThrowIfDisposed();

        if (digit < Bencode.MinNumberCharacter || digit > Bencode.MaxNumberCharacter)
            throw new FormatException("Digit outside the 0-9 range");

        if (IsLengthFinished)
            throw new InvalidOperationException("Finished length value");

        if (_length.HasValue && _length.Value == 0)
            throw new FormatException("Unallowed '0' padding");

        if (_length.HasValue)
            _length = checked((_length.Value * 10) + (digit - Bencode.MinNumberCharacter));
        else
            _length = (digit - Bencode.MinNumberCharacter);

        if (_length > Options.MaxStringLength)
            throw new InvalidOperationException("Max capacity reached");
    }

    public void FinishLength()
    {
        ThrowIfDisposed();

        if (_length is null)
            throw new InvalidOperationException("Length is not set");

        _bytes = new byte[_length.Value];
        _offset = 0;
    }

    public void PushByte(byte b)
    {
        ThrowIfDisposed();

        if (!IsLengthFinished)
            throw new InvalidOperationException("Unfinished length value");

        if (_offset >= _length)
            throw new InvalidOperationException("Max capacity reached");

        _bytes![_offset] = b;
        _offset++;
    }

    public override IBobject ToBobject()
    {
        ThrowIfDisposed();

        if (_bytes is null)
            throw new InvalidOperationException("Unfinished length value");

        if (!IsCompleted)
            throw new InvalidOperationException("Unfinished value");

        return new Bstring(_bytes);
    }

    public override void Dispose()
    {
        base.Dispose();

        _length = 0;
        _offset = 0;
        _bytes = null;
    }
}
