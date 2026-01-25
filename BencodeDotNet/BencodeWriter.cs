using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;
using System.IO.Pipelines;

namespace Jordiware.BencodeDotNet;

/// <summary>
/// Provides a forward-only, streaming writer for Bencoded data that writes
/// objects to a <see cref="Stream"/> using registered serializers.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BencodeWriter"/> is designed for scenarios where objects need to be
/// encoded as Bencode and written incrementally to a stream, potentially
/// containing multiple consecutive Bencoded values.
/// </para>
/// <para>
/// This type does not own the provided <see cref="Stream"/> and will not dispose it.
/// Stream lifetime management remains the responsibility of the caller.
/// </para>
/// <para>
/// Instances of <see cref="BencodeWriter"/> are not thread-safe. A single writer
/// instance should not be used concurrently by multiple consumers.
/// </para>
/// </remarks>
public sealed class BencodeWriter : BencodeIO
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeWriter"/> class with optional <see cref="BencodeOptions"/>.
    /// </summary>
    /// <param name="options">
    /// Optional <see cref="BencodeOptions"/> instance that defines encoding constraints,
    /// string encoding, maximum depth, and other validation settings.
    /// If <c>null</c>, default options are applied.
    /// </param>
    public BencodeWriter(BencodeOptions? options = default) : base(options)
    {
    }

    /// <summary>
    /// Writes a Bencode object directly to the specified <see cref="Stream"/>.
    /// </summary>
    /// <param name="value">
    /// The Bencode object to write. The object is assumed to already represent valid
    /// Bencode data and is written without additional serialization or transformation.
    /// </param>
    /// <param name="output">
    /// The <see cref="Stream"/> to which the Bencode data will be written. The stream
    /// must be writable.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while writing asynchronously.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="value"/> or <paramref name="output"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="output"/> is not writable.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is intended for scenarios where the caller already has a fully
    /// constructed Bencode object, such as values produced by <see cref="BencodeReader"/>
    /// or manually assembled Bencode trees.
    /// </para>
    /// <para>
    /// The provided <see cref="Stream"/> is not owned by this method and is not disposed.
    /// Stream lifetime management remains the responsibility of the caller.
    /// </para>
    /// </remarks>
    public async Task WriteAsync(IBobject value, Stream output, CancellationToken cancellationToken = default)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        if (output is null)
            throw new ArgumentNullException(nameof(output));
        if (!output.CanWrite)
            throw new InvalidOperationException("The output stream must be writable.");

        var writer = PipeWriter.Create(output);
        await value.WriteToPipeAsync(writer, cancellationToken);
        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Writes the specified <paramref name="value"/> as Bencode data into the provided <paramref name="output"/> <see cref="Stream"/>.
    /// </summary>
    /// <typeparam name="T">The type of the object to encode.</typeparam>
    /// <param name="value">The object to write. Cannot be <c>null</c>.</param>
    /// <param name="output">The <see cref="Stream"/> to which Bencode data will be written. Must be writable.</param>
    /// <param name="serializer">
    /// Optional serializer to use for encoding the value. If <c>null</c>, a serializer is resolved
    /// automatically via <see cref="BencodeSerializer.TryGetSerializerForType"/>. If no serializer is found, an exception is thrown.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while writing asynchronously.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous write operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="value"/> or <paramref name="output"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="output"/> is not writable, or if no serializer could be found for the specified type.
    /// </exception>
    public async Task WriteAsync<T>(T value, Stream output, IBencodeSerializer? serializer = null, CancellationToken cancellationToken = default)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        if (output is null)
            throw new ArgumentNullException(nameof(output));
        if (!output.CanWrite)
            throw new InvalidOperationException("The output stream must be writable.");

        // Resolve serializer if not provided
        serializer ??= (BencodeSerializer.TryGetSerializerForType(typeof(T), _options, out var resolved) && resolved is not null)
                       ? resolved
                       : throw new InvalidOperationException($"No serializer found for type {typeof(T)}.");

        var writer = PipeWriter.Create(output);
        await serializer.WriteToPipeAsync(value, writer, cancellationToken);
        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Writes a Bencode object directly to a file on disk.
    /// </summary>
    /// <param name="value">
    /// The Bencode object to write. The object is assumed to already represent valid
    /// Bencode data and is written without additional serialization or transformation.
    /// </param>
    /// <param name="filePath">
    /// The path of the file to write to. The path must not be <c>null</c>, empty, or
    /// consist only of whitespace.
    /// </param>
    /// <param name="overwrite">
    /// Indicates whether an existing file should be overwritten.
    /// If <c>false</c> and the file already exists, an exception is thrown.
    /// If <c>true</c>, the file is truncated or created as needed.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while writing asynchronously.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous file write operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="value"/> is <c>null</c> or if <paramref name="filePath"/>
    /// is <c>null</c>, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if the file already exists and <paramref name="overwrite"/> is <c>false</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method creates or opens a file using asynchronous I/O and writes the raw
    /// Bencode representation of the provided <see cref="IBobject"/> to it.
    /// </para>
    /// <para>
    /// The file stream is owned by this method and is disposed upon completion of the
    /// write operation.
    /// </para>
    /// </remarks>
    public async Task WriteToFileAsync(IBobject value, string filePath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;

        using var fileStream = new FileStream(filePath,
                                              mode,
                                              FileAccess.Write,
                                              FileShare.None,
                                              bufferSize: 4096,
                                              useAsync: true);

        await WriteAsync(value, fileStream, cancellationToken);
    }

    /// <summary>
    /// Writes the specified <paramref name="value"/> as Bencode data to a file on disk.
    /// </summary>
    /// <typeparam name="T">The type of the object to encode.</typeparam>
    /// <param name="value">The object to write. Cannot be <c>null</c>.</param>
    /// <param name="filePath">
    /// The path of the file to write to. The path must not be <c>null</c>, empty, or whitespace.
    /// </param>
    /// <param name="overwrite">
    /// Indicates whether an existing file should be overwritten.
    /// If <c>false</c> and the file already exists, an exception is thrown.
    /// If <c>true</c>, the file is truncated or created as needed.
    /// </param>
    /// <param name="serializer">
    /// Optional serializer to use for encoding the value. If <c>null</c>, a serializer is resolved
    /// automatically via <see cref="BencodeSerializer.TryGetSerializerForType"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while writing asynchronously.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous file write operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="value"/> is <c>null</c> or if <paramref name="filePath"/> is
    /// <c>null</c>, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if the file already exists and <paramref name="overwrite"/> is <c>false</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no suitable serializer can be resolved for the specified type.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method creates or opens a file using asynchronous I/O and writes the Bencoded
    /// representation of the provided value to it.
    /// </para>
    /// </remarks>
    public async Task WriteToFileAsync<T>(T value, string filePath, bool overwrite = false, IBencodeSerializer? serializer = null, CancellationToken cancellationToken = default)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;

        using var fileStream = new FileStream(filePath,
                                              mode,
                                              FileAccess.Write,
                                              FileShare.None,
                                              bufferSize: 4096,
                                              useAsync: true);

        await WriteAsync(value, fileStream, serializer, cancellationToken);
    }
}
