using System.Buffers.Text;
using System.Collections;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet.Objects;

/// <summary>
/// Represents a Bencode byte string.
/// </summary>
/// <remarks>
/// A Bencode string is an arbitrary sequence of bytes, encoded as
/// its byte length in base-10 ASCII, followed by a colon (<c>':'</c>),
/// and then the raw byte sequence.
/// <para>
/// Bencode strings are <em>byte-oriented</em> and do not imply any
/// character encoding. Any textual interpretation is the responsibility
/// of the caller.
/// </para>
/// <para>
/// Examples:
/// <list type="bullet">
/// <item><description><c>4:spam</c></description></item>
/// <item><description><c>0:</c></description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class Bstring : IBobject, IReadOnlyList<byte>, IEquatable<Bstring>, IComparable<Bstring>
{
    private readonly ImmutableArray<byte> _bytes;

    /// <summary>
    /// Gets the raw byte value of the Bencode string.
    /// </summary>
    /// <remarks>
    /// A new array is returned on each access to preserve immutability.
    /// </remarks>
    public byte[] Value => _bytes.ToArray();

    /// <summary>
    /// Initializes a new <see cref="Bstring"/> from a raw byte array.
    /// </summary>
    /// <param name="bytes">
    /// The byte sequence represented by the Bencode string.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="bytes"/> is <see langword="null"/>.
    /// </exception>
    public Bstring(byte[] bytes)
    {
        _bytes = bytes.ToImmutableArray();
    }

    /// <summary>
    /// Initializes a new <see cref="Bstring"/> from a string using
    /// the specified text encoding.
    /// </summary>
    /// <param name="s">
    /// The string to encode as a Bencode byte string.
    /// </param>
    /// <param name="encoding">
    /// The encoding used to convert <paramref name="s"/> into bytes.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="s"/> or <paramref name="encoding"/> is
    /// <see langword="null"/>.
    /// </exception>
    public Bstring(string s, Encoding encoding)
    {
        _bytes = encoding.GetBytes(s).ToImmutableArray();
    }

    #region Interfaces implementation
    /// <summary>
    /// Gets the byte at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the byte to retrieve.
    /// </param>
    public byte this[int index] => _bytes[index];

    /// <summary>
    /// Gets the number of bytes contained in the Bencode string.
    /// </summary>
    public int Count => _bytes.Length;

    /// <summary>
    /// Compares the current <see cref="Bstring"/> with another
    /// <see cref="Bstring"/> using lexicographical byte ordering.
    /// </summary>
    /// <param name="other">
    /// The <see cref="Bstring"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// A value less than zero if this instance precedes <paramref name="other"/>,
    /// zero if they are equal, or a value greater than zero if this instance
    /// follows <paramref name="other"/>.
    /// </returns>
    /// <remarks>
    /// Comparison is performed byte-by-byte. If all compared bytes are equal,
    /// the shorter string is considered smaller.
    /// </remarks>
    public int CompareTo(Bstring? other)
    {
        if (other == null) return 1;
        if (ReferenceEquals(this, other)) return 0;

        var minLength = Math.Min(_bytes.Length, other._bytes.Length);
        for (int i = 0; i < minLength; i++)
        {
            var comparison = _bytes[i].CompareTo(other._bytes[i]);
            if (comparison != 0)
                return comparison;
        }
        return _bytes.Length.CompareTo(other._bytes.Length);
    }

    /// <summary>
    /// Determines whether the current <see cref="Bstring"/> is equal to
    /// another <see cref="Bstring"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="Bstring"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the underlying byte sequences are equal;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(Bstring? other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;

        return _bytes.SequenceEqual(other._bytes);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the bytes
    /// of the Bencode string.
    /// </summary>
    public IEnumerator<byte> GetEnumerator()
    {
        return _bytes.AsEnumerable().GetEnumerator();
    }

    /// <summary>
    /// Serializes the current <see cref="Bstring"/> into its binary
    /// Bencode representation.
    /// </summary>
    /// <returns>
    /// A byte array containing the canonical Bencode encoding
    /// of this string.
    /// </returns>
    public byte[] ToBinaryEncoding()
    {
        var length = Encoding.ASCII.GetBytes($"{_bytes.Length}:");
        var bytes = new byte[length.Length + _bytes.Length];
        length.CopyTo(bytes, 0);
        _bytes.CopyTo(bytes, length.Length);
        return bytes;
    }

    /// <summary>
    /// Computes the encoded length of this string in Bencode format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Strings are encoded as <c>&lt;length&gt;:&lt;data&gt;</c>, where
    /// &lt;length&gt; is the decimal byte length of the string data.
    /// </para>
    /// <para>
    /// The encoded length is therefore equal to:
    /// </para>
    /// <list type="bullet">
    ///   <item>The number of digits required to encode the byte length</item>
    ///   <item>1 byte for the <c>':'</c> separator</item>
    ///   <item>The number of bytes in the string data</item>
    /// </list>
    /// </remarks>
    /// <returns>
    /// The number of bytes required to encode this string.
    /// </returns>
    public int GetEncodedLength()
    {
        if (_bytes.Length == 0)
            return 2;

        var length = _bytes.Length;
        length += (int)Math.Floor(Math.Log10(length));
        length += 2;
        return length;
    }

    /// <summary>
    /// Writes the Bencode string representation of this value to the specified <see cref="PipeWriter"/>.
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
    /// The string is encoded using the standard Bencode format:
    /// <c>&lt;length&gt;:&lt;bytes&gt;</c>, where <c>length</c> represents the number
    /// of bytes written, not characters.
    /// </para>
    /// </remarks>
    public async Task WriteToPipeAsync(PipeWriter writer, CancellationToken cancellationToken = default)
    {
        Span<byte> span = writer.GetSpan(32);

        int written = 0;
        if (!Utf8Formatter.TryFormat(_bytes.Length, span, out int lengthDigits))
            throw new InvalidOperationException("Failed to format string length.");

        written += lengthDigits;

        span[written++] = (byte)':';

        writer.Advance(written);

        await writer.WriteAsync(_bytes.AsMemory(), cancellationToken);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Bstring other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var b in _bytes)
        {
            hash.Add(b);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns a human-readable representation of the Bencode string.
    /// </summary>
    /// <remarks>
    /// The byte payload is decoded using ISO-8859-1 (Latin-1) to ensure
    /// a lossless one-to-one byte-to-character mapping.
    /// This method is intended for debugging and diagnostics only.
    /// </remarks>
    /// <returns>
    /// A string in the form <c>&lt;length&gt;:&lt;data&gt;</c>.
    /// </returns>
    public override string ToString()
    {
        return $"{_bytes.Length}:{Encoding.Latin1.GetString(_bytes.ToArray())}";
    }

    /// <summary>
    /// Returns a hexadecimal representation of the Bencode string.
    /// </summary>
    /// <remarks>
    /// This method is intended for debugging and diagnostics.
    /// </remarks>
    /// <returns>
    /// A string containing the byte length and a hyphen-separated
    /// hexadecimal representation of the byte sequence.
    /// </returns>
    public string ToHexString()
    {
        return $"{_bytes.Length}:{BitConverter.ToString(_bytes.ToArray())}";
    }
}
