using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

internal abstract class BobjectBuilder : IBobjectBuilder
{
    private bool _isDisposed = false;
    private readonly BencodeOptions _options;

    protected BencodeOptions Options
    {
        get
        {
            ThrowIfDisposed();
            return _options;
        }
    }

    protected BobjectBuilder(BencodeOptions? options)
    {
        _options = options ?? new();
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
