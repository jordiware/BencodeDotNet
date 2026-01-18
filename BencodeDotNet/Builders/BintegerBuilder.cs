using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

/// <summary>
/// Incrementally builds a Bencode integer during decoding.
/// </summary>
/// <remarks>
/// This builder is responsible for validating and assembling the numeric
/// value of a Bencode integer according to the Bencode specification.
/// <para>
/// Validation rules enforced by this builder include:
/// <list type="bullet">
/// <item><description>No leading zero padding</description></item>
/// <item><description>No negative zero (<c>i-0e</c>)</description></item>
/// <item><description>Only ASCII digits (<c>0</c>–<c>9</c>) are allowed</description></item>
/// </list>
/// </para>
/// <para>
/// The builder accumulates the numeric value digit-by-digit and produces
/// a <see cref="Binteger"/> once construction is complete.
/// </para>
/// </remarks>
internal sealed class BintegerBuilder : BobjectBuilder
{
    private long? _value = null;
    private bool _isPositive = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="BintegerBuilder"/> class.
    /// </summary>
    /// <param name="options">
    /// Optional decoding options.
    /// </param>
    public BintegerBuilder(BencodeOptions? options = default) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets whether the integer being built is positive.
    /// </summary>
    /// <remarks>
    /// This property is typically set immediately after encountering
    /// a minus sign (<c>'-'</c>) in the encoded input.
    /// <para>
    /// Attempting to mark the integer as negative more than once
    /// or after digits have been processed may result in a
    /// <see cref="FormatException"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if an invalid sign transition is attempted.
    /// </exception>
    public bool IsPositive
    {
        get
        {
            ThrowIfDisposed();
            return !_isPositive;
        }
        set
        {
            ThrowIfDisposed();

            if (!_isPositive && !value)
                throw new FormatException("Value is already negative");

            _isPositive = value;
        }
    }

    /// <summary>
    /// Appends a single digit to the integer being built.
    /// </summary>
    /// <param name="digit">
    /// The ASCII byte representing a digit (<c>'0'</c>–<c>'9'</c>).
    /// </param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if the digit is outside the valid range or violates
    /// Bencode integer formatting rules.
    /// </exception>
    /// <remarks>
    /// This method enforces the following Bencode constraints:
    /// <list type="bullet">
    /// <item><description>No leading zero padding</description></item>
    /// <item><description>No negative zero</description></item>
    /// <item><description>Digits must be ASCII <c>0</c>–<c>9</c></description></item>
    /// </list>
    /// </remarks>
    public void PushDigit(byte digit)
    {
        ThrowIfDisposed();

        if (digit < Bencode.MinNumberCharacter || digit > Bencode.MaxNumberCharacter)
            throw new FormatException("Digit outside the 0-9 range");

        if (!_isPositive && digit == Bencode.MinNumberCharacter && !(_value.HasValue && _value.Value > 0))
            throw new FormatException("Unallowed '0' padding");

        if (_value.HasValue && _value.Value == 0)
            throw new FormatException("Unallowed '0' padding");

        if (_value.HasValue)
            _value = checked((_value.Value * 10) + (digit - Bencode.MinNumberCharacter));
        else
            _value = (digit - Bencode.MinNumberCharacter);
    }

    /// <summary>
    /// Finalizes the integer and returns the corresponding
    /// <see cref="Binteger"/> instance.
    /// </summary>
    /// <returns>
    /// A fully constructed <see cref="Binteger"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if no digits have been provided.
    /// </exception>
    public override IBobject ToBobject()
    {
        ThrowIfDisposed();

        if (!_value.HasValue)
            throw new FormatException("Builder has no value");

        var value = _isPositive ? _value : -_value;
        return new Binteger(value.Value);
    }

    /// <summary>
    /// Disposes the builder and resets its internal state.
    /// </summary>
    /// <remarks>
    /// After disposal, the builder must not be used again.
    /// </remarks>
    public override void Dispose()
    {
        base.Dispose();

        _value = 0;
        _isPositive = true;
    }
}
