using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

internal sealed class BintegerBuilder : BobjectBuilder
{
    private long _value = 0;
    private bool _isPositive = true;

    public BintegerBuilder(BdecodingOptions options = default!) : base(options)
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
            _isPositive = value;
        }
    }

    public void PushDigit(byte digit)
    {
        ThrowIfDisposed();

        _value = checked((_value * 10) + digit);
    }

    public override IBobject ToBobject()
    {
        ThrowIfDisposed();

        var value = _isPositive ? _value : -_value;
        return new Binteger(value);
    }

    public override void Dispose()
    {
        base.Dispose();

        _value = 0;
        _isPositive = true;
    }
}
