using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

internal abstract class BobjectBuilder : IBobjectBuilder
{
    private bool _isDisposed = false;
    private readonly BdecodingOptions _options;

    protected BdecodingOptions Options
    {
        get
        {
            ThrowIfDisposed();
            return _options;
        }
    }

    protected BobjectBuilder(BdecodingOptions options)
    {
        _options = options;
    }

    public abstract IBobject ToBobject();

    protected bool ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().FullName);

        return false;
    }

    public virtual void Dispose()
    {
        ThrowIfDisposed();

        _isDisposed = true;
    }
}
