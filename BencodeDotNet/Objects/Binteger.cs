using System.Text;

namespace Jordiware.BencodeDotNet.Objects;

/// <summary>
/// Represents a Bencode integer.
/// </summary>
/// <remarks>
/// A Bencode integer is encoded as the ASCII character <c>'i'</c>,
/// followed by a base-10 signed integer, and terminated by <c>'e'</c>.
/// <para>
/// Examples:
/// <list type="bullet">
/// <item><description><c>i0e</c></description></item>
/// <item><description><c>i42e</c></description></item>
/// <item><description><c>i-123e</c></description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class Binteger : IBobject, IEquatable<Binteger>, IComparable<Binteger>
{
    private readonly long _value;

    /// <summary>
    /// Gets the numeric value of the Bencode integer.
    /// </summary>
    public long Value => _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Binteger"/> class
    /// with the specified numeric value.
    /// </summary>
    /// <param name="value">
    /// The integer value to encode.
    /// </param>
    public Binteger(long value)
    {
        _value = value;
    }

    #region Interfaces implementation
    /// <summary>
    /// Compares the current <see cref="Binteger"/> with another
    /// <see cref="Binteger"/> instance.
    /// </summary>
    /// <param name="other">
    /// The <see cref="Binteger"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// A value less than zero if this instance is less than <paramref name="other"/>,
    /// zero if they are equal, or a value greater than zero if this instance
    /// is greater than <paramref name="other"/>.
    /// </returns>
    public int CompareTo(Binteger? other)
    {
        if (other == null) return 1;
        
        return _value.CompareTo(other._value);
    }

    /// <summary>
    /// Determines whether the current <see cref="Binteger"/> is equal to
    /// another <see cref="Binteger"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="Binteger"/> to compare with this instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the values are equal; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals(Binteger? other)
    {
        if (other == null) return false;

        return _value.Equals(other._value);
    }

    /// <summary>
    /// Serializes the current <see cref="Binteger"/> into its binary
    /// Bencode representation.
    /// </summary>
    /// <returns>
    /// A byte array containing the canonical Bencode encoding
    /// of this integer.
    /// </returns>
    public byte[] ToBinaryEncoding()
    {
        return Encoding.ASCII.GetBytes(ToString());
    }
    #endregion

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Binteger other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    /// <summary>
    /// Returns the canonical Bencode string representation of this integer.
    /// </summary>
    /// <returns>
    /// A string in the form <c>i&lt;value&gt;e</c>.
    /// </returns>
    public override string ToString()
    {
        return $"i{_value}e";
    }
}
