using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

/// <summary>
/// Provides a base class for Bencode object builders used during decoding.
/// </summary>
/// <remarks>
/// This abstract class implements common lifecycle management and option
/// handling for builders that incrementally construct immutable
/// <see cref="IBObject"/> instances.
/// <para>
/// Builders derived from <see cref="BObjectBuilder"/> are intended to be
/// used exclusively by the decoding pipeline and are not thread-safe.
/// </para>
/// <para>
/// The typical lifecycle of a builder is:
/// <list type="number">
/// <item><description>Construction with decoding options</description></item>
/// <item><description>Incremental population of internal state</description></item>
/// <item><description>Finalization via <see cref="ToBobject"/></description></item>
/// <item><description>Disposal</description></item>
/// </list>
/// </para>
/// </remarks>
internal abstract class BObjectBuilder : IBObjectBuilder
{
    private bool _isDisposed = false;
    private readonly BencodeOptions _options;

    /// <summary>
    /// Gets the decoding options associated with this builder.
    /// </summary>
    /// <remarks>
    /// Accessing this property after the builder has been disposed
    /// results in an <see cref="ObjectDisposedException"/>.
    /// </remarks>
    protected BencodeOptions Options
    {
        get
        {
            ThrowIfDisposed();
            return _options;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BObjectBuilder"/> class
    /// with the specified decoding options.
    /// </summary>
    /// <param name="options">
    /// The decoding options to apply. If <see langword="null"/>, default
    /// options are used.
    /// </param>
    protected BObjectBuilder(BencodeOptions? options)
    {
        _options = options ?? new();
    }

    /// <summary>
    /// Finalizes construction and returns the built Bencode object.
    /// </summary>
    /// <returns>
    /// The fully constructed <see cref="IBObject"/>.
    /// </returns>
    /// <remarks>
    /// Implementations should validate that all required data has been
    /// provided before returning the resulting object.
    /// <para>
    /// Calling this method after the builder has been disposed results
    /// in undefined behavior unless explicitly guarded by the implementation.
    /// </para>
    /// </remarks>
    public abstract IBObject ToBobject();

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the builder
    /// has already been disposed.
    /// </summary>
    /// <returns>
    /// Always returns <see langword="false"/> if the builder is not disposed.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has already been disposed.
    /// </exception>
    protected bool ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().FullName);

        return false;
    }

    /// <summary>
    /// Releases resources associated with the builder and marks it
    /// as disposed.
    /// </summary>
    /// <remarks>
    /// After disposal, any attempt to access the builder or its derived
    /// state should result in an <see cref="ObjectDisposedException"/>.
    /// </remarks>
    public virtual void Dispose()
    {
        ThrowIfDisposed();

        _isDisposed = true;
    }
}
