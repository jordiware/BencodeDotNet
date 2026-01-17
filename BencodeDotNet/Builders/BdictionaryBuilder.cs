using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

internal sealed class BdictionaryBuilder : BobjectBuilder
{
    private Dictionary<Bstring, IBobject>? _objects = new();
    private Bstring? _pendingKey;
    private Bstring? _lastKey;

    public BdictionaryBuilder(BdecodingOptions options = default!) : base(options)
    {
    }

    public bool IsExpectingKey => !ThrowIfDisposed() && _pendingKey is null;
    public bool IsExpectingValue => !ThrowIfDisposed() && _pendingKey is not null;

    public void PushKey(Bstring key)
    {
        ThrowIfDisposed();

        if (IsExpectingValue)
            throw new InvalidOperationException("Value expected");

        if (_objects!.Count >= Options.MaxContainerItems)
            throw new InvalidOperationException("Max capacity reached");

        if (_lastKey is not null
            && key.CompareTo(_lastKey) <= 0)
            throw new FormatException("Dictionary keys must be sorted");

        _pendingKey = key;
    }

    public void PushValue(IBobject value)
    {
        ThrowIfDisposed();

        if (IsExpectingKey)
            throw new InvalidOperationException("Key expected");

        _objects![_pendingKey!] = value;
        _lastKey = _pendingKey;
        _pendingKey = null;
    }

    public override IBobject ToBobject()
    {
        ThrowIfDisposed();

        if (IsExpectingValue)
            throw new InvalidOperationException("Value expected");

        return new Bdictionary(_objects!);
    }

    public override void Dispose()
    {
        base.Dispose();

        _objects!.Clear();
        _objects = null; 
        _pendingKey = null;
        _lastKey = null;
    }
}
