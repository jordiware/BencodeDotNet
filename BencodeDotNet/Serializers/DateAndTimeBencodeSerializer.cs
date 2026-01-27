using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Utils;
using System.IO.Pipelines;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for the <see cref="DateTime"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// <see cref="DateTime"/> values are serialized using
/// <see cref="DateTime.ToBinary"/>, preserving both the timestamp and
/// <see cref="DateTimeKind"/> information.
/// 
/// Deserialization uses <see cref="DateTime.FromBinary(long)"/> and is always
/// lossless for values produced by this serializer.
/// </remarks>
public sealed class DateTimeBencodeSerializer : UnmanagedTypeBencodeSerializer<DateTime, Binteger>
{
    /// <summary>
    /// Serializes a <see cref="DateTime"/> value into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="DateTime"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>.
    /// </returns>
    public override bool TrySerialize(DateTime input, out Binteger? output)
    {
        output = new Binteger(input.ToBinary());
        return true;
    }

    /// <summary>
    /// Deserializes a Bencode integer into a <see cref="DateTime"/> value.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="DateTime"/> value.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out DateTime output)
    {
        output = DateTime.FromBinary(input.Value);
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="DateTime"/> value to the provided
    /// <see cref="PipeWriter"/> in Bencode format.
    /// </summary>
    /// <param name="input">
    /// The <see cref="DateTime"/> value to serialize.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the serialized value will be written.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    /// <remarks>
    /// The <see cref="DateTime"/> is serialized using <see cref="DateTime.ToBinary"/>,
    /// preserving the full value and <see cref="DateTimeKind"/> information, and written
    /// as a Bencode integer.
    /// </remarks>
    public override async Task WriteToPipeAsync(DateTime input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await PipeWriterUtils.WriteIntegerAsync(input.ToBinary(), writer, cancellationToken);
    }
}

/// <summary>
/// Provides serialization and deserialization support for the <see cref="DateOnly"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// <see cref="DateOnly"/> values are serialized using their
/// <see cref="DateOnly.DayNumber"/> representation.
/// 
/// Deserialization performs strict range validation and fails if the encoded value
/// does not represent a valid <see cref="DateOnly"/>.
/// </remarks>
public sealed class DateOnlyBencodeSerializer : UnmanagedTypeBencodeSerializer<DateOnly, Binteger>
{
    /// <summary>
    /// Serializes a <see cref="DateOnly"/> value into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="DateOnly"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>.
    /// </returns>
    public override bool TrySerialize(DateOnly input, out Binteger? output)
    {
        output = new Binteger(input.DayNumber);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into a <see cref="DateOnly"/> value.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="DateOnly"/> value; otherwise, <see cref="DateOnly.MinValue"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value represents a valid date;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out DateOnly output)
    {
        output = default;

        if (input.Value > DateOnly.MaxValue.DayNumber)
            return false;

        if (input.Value < DateOnly.MinValue.DayNumber)
            return false;

        output = DateOnly.FromDayNumber((int)input.Value);
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="DateOnly"/> value to the provided
    /// <see cref="PipeWriter"/> in Bencode format.
    /// </summary>
    /// <param name="input">
    /// The <see cref="DateOnly"/> value to serialize.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the serialized value will be written.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    /// <remarks>
    /// The value is serialized using <see cref="DateOnly.DayNumber"/> and written
    /// as a Bencode integer, mirroring the behavior of the corresponding
    /// <c>TrySerialize</c> implementation.
    /// </remarks>
    public override async Task WriteToPipeAsync(DateOnly input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await PipeWriterUtils.WriteIntegerAsync(input.DayNumber, writer, cancellationToken);
    }
}

/// <summary>
/// Provides serialization and deserialization support for the <see cref="TimeOnly"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// <see cref="TimeOnly"/> values are serialized using their
/// <see cref="TimeOnly.Ticks"/> representation.
/// 
/// Deserialization validates that the encoded value represents a valid time of day.
/// </remarks>
public sealed class TimeOnlyBencodeSerializer : UnmanagedTypeBencodeSerializer<TimeOnly, Binteger>
{
    /// <summary>
    /// Serializes a <see cref="TimeOnly"/> value into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="TimeOnly"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>.
    /// </returns>
    public override bool TrySerialize(TimeOnly input, out Binteger? output)
    {
        output = new Binteger(input.Ticks);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into a <see cref="TimeOnly"/> value.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="TimeOnly"/> value; otherwise, <see cref="TimeOnly.MinValue"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value represents a valid time of day;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out TimeOnly output)
    {
        output = default;

        if (input.Value > TimeOnly.MaxValue.Ticks)
            return false;

        if (input.Value < TimeOnly.MinValue.Ticks)
            return false;

        output = new TimeOnly(input.Value);
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="TimeOnly"/> value to the provided
    /// <see cref="PipeWriter"/> in Bencode format.
    /// </summary>
    /// <param name="input">
    /// The <see cref="TimeOnly"/> value to serialize.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the serialized value will be written.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    /// <remarks>
    /// The value is serialized using <see cref="TimeOnly.Ticks"/> and written
    /// as a Bencode integer, mirroring the behavior of the corresponding
    /// <c>TrySerialize</c> implementation.
    /// </remarks>

    public override async Task WriteToPipeAsync(TimeOnly input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await PipeWriterUtils.WriteIntegerAsync(input.Ticks, writer, cancellationToken);
    }
}

/// <summary>
/// Provides serialization and deserialization support for the <see cref="TimeSpan"/> type
/// using the Bencode integer representation.
/// </summary>
/// <remarks>
/// <see cref="TimeSpan"/> values are serialized using their
/// <see cref="TimeSpan.Ticks"/> representation.
/// 
/// Deserialization performs strict range validation to ensure the encoded value
/// represents a valid <see cref="TimeSpan"/>.
/// </remarks>
public sealed class TimeSpanBencodeSerializer : UnmanagedTypeBencodeSerializer<TimeSpan, Binteger>
{
    /// <summary>
    /// Serializes a <see cref="TimeSpan"/> value into a Bencode integer.
    /// </summary>
    /// <param name="input">
    /// The <see cref="TimeSpan"/> value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="Binteger"/> instance.
    /// </param>
    /// <returns>
    /// Always returns <see langword="true"/>.
    /// </returns>
    public override bool TrySerialize(TimeSpan input, out Binteger? output)
    {
        output = new Binteger(input.Ticks);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a Bencode integer into a <see cref="TimeSpan"/> value.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Binteger"/> instance to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// <see cref="TimeSpan"/> value; otherwise, <see cref="TimeSpan.Zero"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the encoded value represents a valid
    /// <see cref="TimeSpan"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool TryDeserialize(Binteger input, out TimeSpan output)
    {
        output = default;

        if (input.Value > TimeSpan.MaxValue.Ticks)
            return false;

        if (input.Value < TimeSpan.MinValue.Ticks)
            return false;

        output = TimeSpan.FromTicks(input.Value);
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="TimeSpan"/> value to the provided
    /// <see cref="PipeWriter"/> in Bencode format.
    /// </summary>
    /// <param name="input">
    /// The <see cref="TimeSpan"/> value to serialize.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the serialized value will be written.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    /// <remarks>
    /// The value is serialized using <see cref="TimeSpan.Ticks"/> and written
    /// as a Bencode integer, mirroring the behavior of the corresponding
    /// <c>TrySerialize</c> implementation.
    /// </remarks>
    public override async Task WriteToPipeAsync(TimeSpan input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        await PipeWriterUtils.WriteIntegerAsync(input.Ticks, writer, cancellationToken);
    }
}
