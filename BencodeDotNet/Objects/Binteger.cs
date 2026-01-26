using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

    /// <summary>
    /// Computes the encoded length of this integer in Bencode format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Integers are encoded as <c>i&lt;value&gt;e</c>, where &lt;value&gt; is the
    /// base-10 string representation of the integer.
    /// </para>
    /// <para>
    /// The encoded length is therefore equal to:
    /// </para>
    /// <list type="bullet">
    ///   <item>1 byte for the leading <c>'i'</c></item>
    ///   <item>The number of digits in the integer value (including a leading '-' if negative)</item>
    ///   <item>1 byte for the trailing <c>'e'</c></item>
    /// </list>
    /// </remarks>
    /// <returns>
    /// The number of bytes required to encode this integer.
    /// </returns>
    public int GetEncodedLength()
    {
        if (_value == 0)
            return 3;

        var length = 3 + (int)Math.Floor(Math.Log10(Math.Abs(_value)));
        if (_value < 0)
            length++;

        return length;
    }

    /// <summary>
    /// Writes the Bencode integer representation of this value to the specified <see cref="PipeWriter"/>.
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
    /// The integer is encoded using the standard Bencode format:
    /// <c>i&lt;digits&gt;e</c>, where the numeric value is written using
    /// an invariant culture representation.
    /// </para>
    /// </remarks>
    public async Task WriteToPipeAsync(PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await writer.WriteAsync(new[] { Bencode.IntegerBeginCharacter }, cancellationToken);
        await writer.WriteAsync(Encoding.ASCII.GetBytes(Value.ToString(CultureInfo.InvariantCulture)), cancellationToken);
        await writer.WriteAsync(new[] { Bencode.TerminationCharacter }, cancellationToken);
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
