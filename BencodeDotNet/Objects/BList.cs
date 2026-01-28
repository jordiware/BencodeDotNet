using System.Collections;
using System.Collections.Immutable;
using System.IO.Pipelines;

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
public sealed class BList : IBObject, IReadOnlyList<IBObject>, IEquatable<BList>
{
    private readonly ImmutableArray<IBObject> _objects;

    /// <summary>
    /// Initializes a new instance of the <see cref="BList"/> class
    /// from the specified sequence of Bencode objects.
    /// </summary>
    /// <param name="objects">
    /// The objects contained in the list, in iteration order.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="objects"/> is <see langword="null"/>.
    /// </exception>
    public BList(IEnumerable<IBObject> objects)
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
    public IBObject this[int index] => _objects[index];

    /// <summary>
    /// Gets the number of elements contained in the list.
    /// </summary>
    public int Count => _objects.Length;

    /// <summary>
    /// Returns an enumerator that iterates through the elements
    /// of the Bencode list.
    /// </summary>
    public IEnumerator<IBObject> GetEnumerator()
    {
        return _objects.AsEnumerable().GetEnumerator();
    }

    /// <summary>
    /// Determines whether the current <see cref="BList"/> is equal to
    /// another <see cref="BList"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="BList"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if both lists contain the same number
    /// of elements and all corresponding elements are equal;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Equality is order-sensitive and is evaluated element-by-element.
    /// </remarks>
    public bool Equals(BList? other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Enumerable.SequenceEqual(this, other);
    }

    /// <summary>
    /// Serializes the current <see cref="BList"/> into its binary
    /// Bencode representation.
    /// </summary>
    /// <returns>
    /// A byte array containing the canonical Bencode encoding
    /// of this list.
    /// </returns>
    public byte[] ToBinaryEncoding()
    {
        var encoded = new List<byte>([ Bencode.ListBeginCharacter ]);
        foreach (IBObject o in _objects)
        {
            encoded.AddRange(o.ToBinaryEncoding());
        }
        encoded.Add(Bencode.TerminationCharacter);
        return encoded.ToArray();
    }

    /// <summary>
    /// Computes the encoded length of this list in Bencode format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lists are encoded as <c>l&lt;items&gt;e</c>, where each item is encoded
    /// sequentially.
    /// </para>
    /// <para>
    /// The encoded length is therefore equal to:
    /// </para>
    /// <list type="bullet">
    ///   <item>1 byte for the leading <c>'l'</c></item>
    ///   <item>The sum of the encoded lengths of all contained items</item>
    ///   <item>1 byte for the trailing <c>'e'</c></item>
    /// </list>
    /// </remarks>
    /// <returns>
    /// The number of bytes required to encode this list.
    /// </returns>
    public int GetEncodedLength()
    {
        var length = 2;
        
        foreach (var o in _objects)
            length += o.GetEncodedLength();

        return length;
    }

    /// <summary>
    /// Writes the Bencode list representation of this value to the specified <see cref="PipeWriter"/>.
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
    /// The list is encoded using the standard Bencode format:
    /// <c>l&lt;item1&gt;&lt;item2&gt;...e</c>.
    /// </para>
    /// <para>
    /// Each contained <see cref="IBObject"/> is written in sequence using its own
    /// <see cref="IBObject.WriteToPipeAsync"/> implementation.
    /// </para>
    /// </remarks>
    public async Task WriteToPipeAsync(PipeWriter writer, CancellationToken cancellationToken = default)
    {
        Span<byte> span = writer.GetSpan(1);
        span[0] = Bencode.ListBeginCharacter;
        writer.Advance(1);

        foreach (var item in _objects)
        {
            await item.WriteToPipeAsync(writer, cancellationToken);
        }

        span = writer.GetSpan(1);
        span[0] = Bencode.TerminationCharacter;
        writer.Advance(1);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is BList other && Equals(other);
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
