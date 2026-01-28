using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

/// <summary>
/// Defines a builder responsible for incrementally constructing
/// a Bencode object during decoding.
/// </summary>
/// <remarks>
/// Implementations of this interface are used internally by the decoder
/// to assemble complex Bencode objects (such as lists and dictionaries)
/// from a streaming input source.
/// <para>
/// A builder encapsulates the mutable state required during decoding
/// and produces an immutable <see cref="IBObject"/> when construction
/// is complete.
/// </para>
/// <para>
/// Implementations are expected to be single-use and not thread-safe.
/// </para>
/// </remarks>
internal interface IBobjectBuilder : IDisposable
{
    /// <summary>
    /// Finalizes construction and returns the built Bencode object.
    /// </summary>
    /// <returns>
    /// The fully constructed <see cref="IBObject"/>.
    /// </returns>
    /// <remarks>
    /// This method should only be called once the builder has received
    /// all required data. Calling this method multiple times or after
    /// disposal results in undefined behavior.
    /// </remarks>
    IBObject ToBobject();
}
