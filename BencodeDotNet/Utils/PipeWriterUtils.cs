using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace Jordiware.BencodeDotNet.Utils;

internal static class PipeWriterUtils
{
    /// <summary>
    /// Writes an integer value to the provided <see cref="PipeWriter"/> using the
    /// Bencode integer format (<c>'i' ... 'e'</c>).
    /// </summary>
    /// <typeparam name="T">
    /// Supported integer types: <see cref="byte"/>, <see cref="sbyte"/>,
    /// <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>,
    /// <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>.
    /// </typeparam>
    /// <param name="value">
    /// The integer value to write.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the encoded bytes will be written.
    /// The writer is owned by the caller and must not be completed, flushed, or disposed.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to cancel the operation.
    /// </param>
    public static async Task WriteIntegerAsync<T>(T value, PipeWriter writer, CancellationToken cancellationToken = default)
        where T : struct, IConvertible
    {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        // Write 'i'
        await writer.WriteAsync(new byte[] { Bencode.IntegerBeginCharacter }, cancellationToken);

        ulong absValue;
        bool negative = false;

        switch (value)
        {
            case sbyte sb:
                negative = sb < 0;
                absValue = (ulong)(negative ? -sb : sb);
                break;
            case byte b:
                absValue = b;
                break;
            case short s:
                negative = s < 0;
                absValue = (ulong)(negative ? -s : s);
                break;
            case ushort us:
                absValue = us;
                break;
            case int i:
                negative = i < 0;
                absValue = (ulong)(negative ? -i : i);
                break;
            case uint ui:
                absValue = ui;
                break;
            case long l:
                negative = l < 0;
                absValue = (ulong)(negative ? -l : l);
                break;
            case ulong ul:
                absValue = ul;
                break;
            case char c:
                absValue = c;
                break;
            default:
                throw new NotSupportedException($"Type {typeof(T)} is not supported.");
        }

        // Special case 0
        if (absValue == 0)
        {
            await writer.WriteAsync(new byte[] { Bencode.MinNumberCharacter }, cancellationToken);
        }
        else
        {
            // Max digits for ulong is 20 + 1 for sign
            Span<byte> buffer = stackalloc byte[21];
            int pos = buffer.Length;

            while (absValue > 0)
            {
                buffer[--pos] = (byte)(Bencode.MinNumberCharacter + (absValue % 10));
                absValue /= 10;
            }

            if (negative)
            {
                buffer[--pos] = (byte)'-';
            }

            await writer.WriteAsync(buffer[pos..].ToArray(), cancellationToken);
        }

        // Write 'e'
        await writer.WriteAsync(new byte[] { Bencode.TerminationCharacter }, cancellationToken);
    }

    /// <summary>
    /// Asynchronously writes a Bencode byte string (<c>&lt;length&gt;:&lt;raw-bytes&gt;</c>)
    /// into a <see cref="PipeWriter"/>, supporting arbitrarily large data.
    /// </summary>
    /// <param name="data">
    /// The raw bytes to encode as a Bencode byte string.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the bytes are written.
    /// The writer is owned by the caller and must not be completed, flushed, or disposed.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to cancel the operation.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task WriteBytesAsync(ReadOnlyMemory<byte> data, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        cancellationToken.ThrowIfCancellationRequested();

        // 1. Write length prefix
        Span<byte> prefixBuffer = stackalloc byte[11];
        int pos = prefixBuffer.Length;
        int length = data.Length;
        do
        {
            prefixBuffer[--pos] = (byte)('0' + (length % 10));
            length /= 10;
        } while (length > 0);

        prefixBuffer[--pos] = (byte)':';
        int prefixLength = prefixBuffer.Length - pos;

        var span = writer.GetSpan(prefixLength);
        prefixBuffer.Slice(pos, prefixLength).CopyTo(span);
        writer.Advance(prefixLength);

        // 2. Write data in chunks
        const int chunkSize = 8192;
        ReadOnlyMemory<byte> remaining = data;
        while (!remaining.IsEmpty)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int writeSize = Math.Min(remaining.Length, chunkSize);
            var slice = remaining.Slice(0, writeSize);

            var target = writer.GetMemory(writeSize);
            slice.CopyTo(target);
            writer.Advance(writeSize);

            remaining = remaining.Slice(writeSize);

            // allow PipeWriter to flush asynchronously
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes a <see cref="string"/> as a Bencode byte string (<c>&lt;length&gt;:&lt;raw-bytes&gt;</c>)
    /// directly into a <see cref="PipeWriter"/>, streaming and allocation-free.
    /// </summary>
    /// <param name="input">The string to encode. Must not be null.</param>
    /// <param name="encoding">The <see cref="Encoding"/> used to convert the string to bytes.</param>
    /// <param name="writer">The <see cref="PipeWriter"/> to which the encoded bytes will be written.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task WriteStringAsync(string input, Encoding encoding, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));
        if (encoding is null)
            throw new ArgumentNullException(nameof(encoding));
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        cancellationToken.ThrowIfCancellationRequested();

        // Compute byte length for length prefix
        int byteCount = encoding.GetByteCount(input);

        // Write length prefix
        Span<byte> prefixBuffer = stackalloc byte[11];
        int pos = prefixBuffer.Length;
        int len = byteCount;
        do
        {
            prefixBuffer[--pos] = (byte)('0' + (len % 10));
            len /= 10;
        } while (len > 0);
        prefixBuffer[--pos] = (byte)':';
        int prefixLength = prefixBuffer.Length - pos;

        var span = writer.GetSpan(prefixLength);
        prefixBuffer.Slice(pos, prefixLength).CopyTo(span);
        writer.Advance(prefixLength);

        // Encode in chunks
        Encoder encoder = encoding.GetEncoder();
        const int bufferSize = 8192;
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int charsProcessed = 0;

            while (charsProcessed < input.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int charsRemaining = input.Length - charsProcessed;
                int bytesAvailable = rentedBuffer.Length;

                encoder.Convert(input.AsSpan(charsProcessed, charsRemaining),
                                rentedBuffer,
                                flush: false,
                                out var charsUsed,
                                out var bytesUsed,
                                out var completed);

                span = writer.GetSpan(bytesUsed);
                rentedBuffer.AsSpan(0, bytesUsed).CopyTo(span);
                writer.Advance(bytesUsed);

                charsProcessed += charsUsed;

                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            // Flush any remaining encoder state
            int finalBytes = encoder.GetBytes(ReadOnlySpan<char>.Empty, rentedBuffer, flush: true);
            if (finalBytes > 0)
            {
                var finalSpan = writer.GetSpan(finalBytes);
                rentedBuffer.AsSpan(0, finalBytes).CopyTo(finalSpan);
                writer.Advance(finalBytes);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }
}
