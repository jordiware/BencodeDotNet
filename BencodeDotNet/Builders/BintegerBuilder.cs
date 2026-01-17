using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

internal sealed class BintegerBuilder : BobjectBuilder
{
    private long? _value = null;
    private bool _isPositive = true;

    public BintegerBuilder(BencodeOptions? options = default) : base(options)
    {
    }

    public bool IsPositive
    {
        get
        {
            ThrowIfDisposed();
            return !_isPositive;
        }
        set
        {
            ThrowIfDisposed();

            if (!_isPositive && !value)
                throw new FormatException("Value is already negative");

            _isPositive = value;
        }
    }

    public void PushDigit(byte digit)
    {
        ThrowIfDisposed();

        if (digit < Bencode.MinNumberCharacter || digit > Bencode.MaxNumberCharacter)
            throw new FormatException("Digit outside the 0-9 range");

        if (!_isPositive && digit == Bencode.MinNumberCharacter && !(_value.HasValue && _value.Value > 0))
            throw new FormatException("Unallowed '0' padding");

        if (_value.HasValue && _value.Value == 0)
            throw new FormatException("Unallowed '0' padding");

        if (_value.HasValue)
            _value = checked((_value.Value * 10) + (digit - Bencode.MinNumberCharacter));
        else
            _value = (digit - Bencode.MinNumberCharacter);
    }

    public override IBobject ToBobject()
    {
        ThrowIfDisposed();

        if (!_value.HasValue)
            throw new FormatException("Builder has no value");

        var value = _isPositive ? _value : -_value;
        return new Binteger(value.Value);
    }

    public override void Dispose()
    {
        base.Dispose();

        _value = 0;
        _isPositive = true;
    }
}
