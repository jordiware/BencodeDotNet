using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Builders;

/// <summary>
/// Incrementally builds a Bencode byte string during decoding.
/// </summary>
/// <remarks>
/// This builder parses and validates the length-prefixed structure
/// of a Bencode string and incrementally collects its byte payload.
/// <para>
/// A Bencode string is composed of:
/// <list type="number">
/// <item><description>A decimal length prefix</description></item>
/// <item><description>A colon separator (<c>':'</c>)</description></item>
/// <item><description>An exact number of raw bytes</description></item>
/// </list>
/// </para>
/// <para>
/// The builder operates as a small state machine:
/// <list type="bullet">
/// <item><description>Length accumulation</description></item>
/// <item><description>Length finalization</description></item>
/// <item><description>Byte collection</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class BstringBuilder : BobjectBuilder
{
    private long? _length = null;
    private long _offset = 0;
    private byte[]? _bytes = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="BstringBuilder"/> class.
    /// </summary>
    /// <param name="options">
    /// Optional decoding options.
    /// </param>
    public BstringBuilder(BencodeOptions? options = default) : base(options)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the length prefix has been
    /// fully parsed and the internal buffer has been allocated.
    /// </summary>
    /// <remarks>
    /// This property becomes <see langword="true"/> after
    /// <see cref="FinishLength"/> has been successfully called.
    /// </remarks>
    public bool IsLengthFinished => !ThrowIfDisposed() && _length.HasValue && _bytes is not null;

    /// <summary>
    /// Gets a value indicating whether the string payload has been
    /// fully received.
    /// </summary>
    /// <remarks>
    /// The builder is considered complete once the number of received
    /// bytes exactly matches the declared length.
    /// </remarks>
    public bool IsCompleted => !ThrowIfDisposed() && _length.HasValue && _offset == _length;

    /// <summary>
    /// Appends a digit to the length prefix of the Bencode string.
    /// </summary>
    /// <param name="digit">
    /// The ASCII byte representing a digit (<c>'0'</c>–<c>'9'</c>).
    /// </param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the length has already been finalized.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if the digit is invalid or violates Bencode length rules.
    /// </exception>
    /// <remarks>
    /// This method enforces the following constraints:
    /// <list type="bullet">
    /// <item><description>Digits must be ASCII <c>0</c>–<c>9</c></description></item>
    /// <item><description>No leading zero padding</description></item>
    /// <item><description>Length must not exceed <see cref="BencodeOptions.MaxStringLength"/></description></item>
    /// </list>
    /// </remarks>
    public void PushLengthDigit(byte digit)
    {
        ThrowIfDisposed();

        if (digit < Bencode.MinNumberCharacter || digit > Bencode.MaxNumberCharacter)
            throw new FormatException("Digit outside the 0-9 range");

        if (IsLengthFinished)
            throw new InvalidOperationException("Finished length value");

        if (_length.HasValue && _length.Value == 0)
            throw new FormatException("Unallowed '0' padding");

        if (_length.HasValue)
            _length = checked((_length.Value * 10) + (digit - Bencode.MinNumberCharacter));
        else
            _length = (digit - Bencode.MinNumberCharacter);

        if (_length > Options.MaxStringLength)
            throw new InvalidOperationException("Max capacity reached");
    }

    /// <summary>
    /// Finalizes the length prefix and allocates the internal buffer
    /// for the string payload.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the length has not been set.
    /// </exception>
    public void FinishLength()
    {
        ThrowIfDisposed();

        if (_length is null)
            throw new InvalidOperationException("Length is not set");

        _bytes = new byte[_length.Value];
        _offset = 0;
    }

    /// <summary>
    /// Appends a single byte to the string payload.
    /// </summary>
    /// <param name="b">
    /// The byte to append.
    /// </param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the length has not been finalized or if the
    /// declared length has already been reached.
    /// </exception>
    public void PushByte(byte b)
    {
        ThrowIfDisposed();

        if (!IsLengthFinished)
            throw new InvalidOperationException("Unfinished length value");

        if (_offset >= _length)
            throw new InvalidOperationException("Max capacity reached");

        _bytes![_offset] = b;
        _offset++;
    }

    /// <summary>
    /// Finalizes the string and returns the corresponding
    /// <see cref="Bstring"/> instance.
    /// </summary>
    /// <returns>
    /// A fully constructed <see cref="Bstring"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the builder has been disposed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the length or payload is incomplete.
    /// </exception>
    public override IBobject ToBobject()
    {
        ThrowIfDisposed();

        if (_bytes is null)
            throw new InvalidOperationException("Unfinished length value");

        if (!IsCompleted)
            throw new InvalidOperationException("Unfinished value");

        return new Bstring(_bytes);
    }

    /// <summary>
    /// Disposes the builder and clears its internal state.
    /// </summary>
    /// <remarks>
    /// After disposal, the builder must not be used again.
    /// </remarks>
    public override void Dispose()
    {
        base.Dispose();

        _length = 0;
        _offset = 0;
        _bytes = null;
    }
}
