using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

/// <summary>
/// Incrementally builds a <see cref="Bdictionary"/> by alternating key and value insertion.
/// </summary>
/// <remarks>
/// <para>
/// This builder enforces the structural and ordering rules required by the
/// Bencode specification:
/// <list type="bullet">
/// <item><description>Keys and values must be provided in alternating order</description></item>
/// <item><description>Dictionary keys must be sorted lexicographically</description></item>
/// <item><description>Container size is bounded by decoding options</description></item>
/// </list>
/// </para>
/// <para>
/// Structural framing (<c>'d'</c> and <c>'e'</c>) is handled externally by the decoder;
/// this builder is responsible solely for content validation and accumulation.
/// </para>
/// </remarks>
internal sealed class BdictionaryBuilder : BobjectBuilder
{
    private Dictionary<Bstring, IBobject>? _objects = new();
    private Bstring? _pendingKey;
    private Bstring? _lastKey;

    /// <summary>
    /// Initializes a new <see cref="BdictionaryBuilder"/>.
    /// </summary>
    /// <param name="options">
    /// Optional decoding options.
    /// </param>
    public BdictionaryBuilder(BencodeOptions? options = default) : base(options)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the builder is currently expecting a dictionary key.
    /// </summary>
    /// <remarks>
    /// When this property is <c>true</c>, the next operation must be
    /// <see cref="PushKey(Bstring)"/>.
    /// </remarks>
    public bool IsExpectingKey => !ThrowIfDisposed() && _pendingKey is null;

    /// <summary>
    /// Gets a value indicating whether the builder is currently expecting a dictionary value.
    /// </summary>
    /// <remarks>
    /// When this property is <c>true</c>, the next operation must be
    /// <see cref="PushValue(IBobject)"/>.
    /// </remarks>
    public bool IsExpectingValue => !ThrowIfDisposed() && _pendingKey is not null;

    /// <summary>
    /// Adds a key to the dictionary and transitions the builder to value-accepting state.
    /// </summary>
    /// <param name="key">The dictionary key.</param>
    /// <exception cref="BencodeFormatException">
    /// Thrown if the key ordering violates the Bencode requirement for sorted keys.
    /// </exception>
    /// <exception cref="BencodeValidationException">
    /// Thrown if the maximum container size is exceeded.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    public void PushKey(Bstring key)
    {
        ThrowIfDisposed();

        if (IsExpectingValue)
            throw new BencodeFormatException("Value expected");

        if (_objects!.Count >= Options.MaxContainerItems)
            throw new BencodeValidationException("Max capacity reached");

        if (_lastKey is not null
            && key.CompareTo(_lastKey) <= 0)
            throw new BencodeFormatException("Dictionary keys must be sorted");

        _pendingKey = key;
    }

    /// <summary>
    /// Adds a value associated with the previously provided key.
    /// </summary>
    /// <param name="value">The value to associate with the current key.</param>
    /// <exception cref="BencodeFormatException">
    /// Thrown if a key is expected instead of a value.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    public void PushValue(IBobject value)
    {
        ThrowIfDisposed();

        if (IsExpectingKey)
            throw new BencodeFormatException("Key expected");

        _objects![_pendingKey!] = value;
        _lastKey = _pendingKey;
        _pendingKey = null;
    }

    /// <summary>
    /// Finalizes the builder and produces a <see cref="Bdictionary"/> containing
    /// all accumulated key/value pairs.
    /// </summary>
    /// <returns>The constructed <see cref="Bdictionary"/>.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown if a value is still pending for the last key.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    public override IBobject ToBobject()
    {
        ThrowIfDisposed();

        if (IsExpectingValue)
            throw new BencodeFormatException("Value expected");

        return new Bdictionary(_objects!);
    }

    /// <summary>
    /// Disposes the builder and clears all accumulated state.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();

        _objects!.Clear();
        _objects = null; 
        _pendingKey = null;
        _lastKey = null;
    }
}
