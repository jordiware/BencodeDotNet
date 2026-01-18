using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

/// <summary>
/// Incrementally builds a <see cref="Blist"/> by collecting fully parsed child objects.
/// </summary>
/// <remarks>
/// <para>
/// This builder is used by the decoder once a list start marker (<c>'l'</c>) has
/// been encountered. Child objects are pushed in encounter order and preserved
/// verbatim in the resulting <see cref="Blist"/>.
/// </para>
/// <para>
/// Structural framing (list start and termination characters) is handled externally
/// by the decoder; this builder only manages element accumulation and capacity
/// enforcement.
/// </para>
/// </remarks>
internal sealed class BlistBuilder : BobjectBuilder
{
    private List<IBobject>? _objects = new();

    /// <summary>
    /// Initializes a new <see cref="BlistBuilder"/>.
    /// </summary>
    /// <param name="options">
    /// Optional decoding options.
    /// </param>
    public BlistBuilder(BencodeOptions? options = default) : base(options)
    {
    }

    /// <summary>
    /// Appends a fully constructed child object to the list.
    /// </summary>
    /// <param name="obj">The object to add to the list.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the maximum number of container items defined by
    /// <see cref="BencodeOptions.MaxContainerItems"/> has been reached.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    public void PushObject(IBobject obj)
    {
        ThrowIfDisposed();

        if (_objects!.Count >= Options.MaxContainerItems)
            throw new InvalidOperationException("Max capacity reached");

        _objects!.Add(obj);
    }

    /// <summary>
    /// Finalizes the builder and produces a <see cref="Blist"/> containing all
    /// accumulated child objects.
    /// </summary>
    /// <returns>The constructed <see cref="Blist"/>.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    public override IBobject ToBobject()
    {
        ThrowIfDisposed();

        return new Blist(_objects!);
    }

    /// <summary>
    /// Disposes the builder and releases all accumulated state.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();

        _objects!.Clear();
        _objects = null;
    }
}
