using System.IO.Pipelines;

namespace Jordiware.BencodeDotNet.Objects;

/// <summary>
/// Represents a Bencode object.
/// </summary>
/// <remarks>
/// All Bencode data types (integer, byte string, list, and dictionary)
/// implement this interface. Implementations must be able to serialize
/// themselves into their canonical Bencode binary representation.
/// </remarks>
public interface IBObject
{
    /// <summary>
    /// Serializes the current Bencode object into its binary Bencode encoding.
    /// </summary>
    /// <returns>
    /// A byte array containing the canonical Bencode representation
    /// of the object.
    /// </returns>
    byte[] ToBinaryEncoding();

    /// <summary>
    /// Computes the exact number of bytes required to encode this Bencode object
    /// using the canonical Bencode binary format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GetEncodedLength"/> returns the size, in bytes, of the Bencode
    /// representation that would be produced by <see cref="ToBinaryEncoding"/>,
    /// without allocating any buffers or performing any serialization.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The exact number of bytes required to encode this Bencode object.
    /// </returns>
    int GetEncodedLength();

    /// <summary>
    /// Writes the Bencode representation of this object to the specified <see cref="PipeWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the Bencode data will be written.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while writing asynchronously.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method writes the raw Bencode encoding of the object directly to the provided
    /// <see cref="PipeWriter"/>. The object is assumed to already represent valid Bencode data.
    /// </para>
    /// <para>
    /// Implementations should not flush the writer. Flushing is the responsibility of the
    /// caller, typically a higher-level component such as <see cref="BencodeWriter"/>.
    /// </para>
    /// </remarks>
    Task WriteToPipeAsync(PipeWriter writer, CancellationToken cancellationToken = default);
}
