namespace Jordiware.BencodeDotNet.Objects;

/// <summary>
/// Represents a Bencode object.
/// </summary>
/// <remarks>
/// All Bencode data types (integer, byte string, list, and dictionary)
/// implement this interface. Implementations must be able to serialize
/// themselves into their canonical Bencode binary representation.
/// </remarks>
public interface IBobject
{
    /// <summary>
    /// Serializes the current Bencode object into its binary Bencode encoding.
    /// </summary>
    /// <returns>
    /// A byte array containing the canonical Bencode representation
    /// of the object.
    /// </returns>
    byte[] ToBinaryEncoding();
}
