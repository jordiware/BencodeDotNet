using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

internal sealed class BlistBuilder : BobjectBuilder
{
    private List<IBobject>? _objects = new();

    public BlistBuilder(BencodeOptions? options = default) : base(options)
    {
    }

    public void PushObject(IBobject obj)
    {
        ThrowIfDisposed();

        if (_objects!.Count >= Options.MaxContainerItems)
            throw new InvalidOperationException("Max capacity reached");

        _objects!.Add(obj);
    }

    public override IBobject ToBobject()
    {
        ThrowIfDisposed();

        return new Blist(_objects!);
    }

    public override void Dispose()
    {
        base.Dispose();

        _objects!.Clear();
        _objects = null;
    }
}
