using System.Collections;
using System.Collections.Immutable;

namespace Jordiware.BencodeDotNet.Objects;

/// <summary>
/// Represents a Bencode list.
/// </summary>
/// <remarks>
/// A Bencode list is an ordered collection of Bencode objects,
/// encoded as the ASCII character <c>'l'</c>, followed by the
/// Bencode encoding of each element in sequence, and terminated
/// by the character <c>'e'</c>.
/// <para>
/// Lists may contain elements of any Bencode type, including
/// other lists and dictionaries.
/// </para>
/// <para>
/// Example:
/// <c>l4:spami42ee</c>
/// </para>
/// </remarks>
public sealed class Blist : IBobject, IReadOnlyList<IBobject>, IEquatable<Blist>
{
    private readonly ImmutableArray<IBobject> _objects;

    /// <summary>
    /// Initializes a new instance of the <see cref="Blist"/> class
    /// from the specified sequence of Bencode objects.
    /// </summary>
    /// <param name="objects">
    /// The objects contained in the list, in iteration order.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="objects"/> is <see langword="null"/>.
    /// </exception>
    public Blist(IEnumerable<IBobject> objects)
    {
        _objects = objects.ToImmutableArray();
    }

    #region Interfaces implementation
    /// <summary>
    /// Gets the Bencode object at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the element to retrieve.
    /// </param>
    public IBobject this[int index] => _objects[index];

    /// <summary>
    /// Gets the number of elements contained in the list.
    /// </summary>
    public int Count => _objects.Length;

    /// <summary>
    /// Returns an enumerator that iterates through the elements
    /// of the Bencode list.
    /// </summary>
    public IEnumerator<IBobject> GetEnumerator()
    {
        return _objects.AsEnumerable().GetEnumerator();
    }

    /// <summary>
    /// Determines whether the current <see cref="Blist"/> is equal to
    /// another <see cref="Blist"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="Blist"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if both lists contain the same number
    /// of elements and all corresponding elements are equal;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Equality is order-sensitive and is evaluated element-by-element.
    /// </remarks>
    public bool Equals(Blist? other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Enumerable.SequenceEqual(this, other);
    }

    /// <summary>
    /// Serializes the current <see cref="Blist"/> into its binary
    /// Bencode representation.
    /// </summary>
    /// <returns>
    /// A byte array containing the canonical Bencode encoding
    /// of this list.
    /// </returns>
    public byte[] ToBinaryEncoding()
    {
        var encoded = new List<byte>([ Bencode.ListBeginCharacter ]);
        foreach (IBobject o in _objects)
        {
            encoded.AddRange(o.ToBinaryEncoding());
        }
        encoded.Add(Bencode.TerminationCharacter);
        return encoded.ToArray();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Blist other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var o in _objects)
        {
            hash.Add(o);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns the canonical Bencode string representation of the list.
    /// </summary>
    /// <remarks>
    /// This method concatenates the string representations of the
    /// contained objects and is intended primarily for diagnostics
    /// and debugging.
    /// </remarks>
    /// <returns>
    /// A string in the form <c>l&lt;items&gt;e</c>.
    /// </returns>
    public override string ToString()
    {
        return $"l{string.Join(string.Empty, _objects)}e";
    }
}
