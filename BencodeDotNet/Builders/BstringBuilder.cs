using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

internal sealed class BstringBuilder : BobjectBuilder
{
    private long _length = 0;
    private long _offset = 0;
    private byte[]? _bytes = null;

    public BstringBuilder(BdecodingOptions options = default!) : base(options)
    {
    }

    public bool IsLengthFinished => !ThrowIfDisposed() && _bytes is not null;

    public void PushLengthDigit(byte digit)
    {
        ThrowIfDisposed();

        if (digit < Bencode.MinNumberCharacter || digit > Bencode.MaxNumberCharacter)
            throw new ArgumentOutOfRangeException(nameof(digit));

        if (IsLengthFinished)
            throw new InvalidOperationException("Finished length value");

        _length = checked((_length * 10) + digit);

        if (_length > Options.MaxStringLength)
            throw new InvalidOperationException("Max capacity reached");
    }

    public void FinishLength()
    {
        ThrowIfDisposed();

        _bytes = new byte[_length];
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
