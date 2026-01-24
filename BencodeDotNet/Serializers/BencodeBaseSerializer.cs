using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Defines a non-generic contract for serializing and deserializing CLR values
/// to and from Bencode objects.
/// </summary>
/// <remarks>
/// <para>
/// This interface represents the non-generic boundary used by the serializer
/// registry and dynamic dispatch mechanisms. It intentionally erases generic
/// type information in order to allow runtime resolution of serializers based
/// on CLR <see cref="Type"/> values.
/// </para>
/// <para>
/// Implementations are expected to internally delegate to strongly typed
/// serializers and must never throw exceptions. All failure conditions must be
/// reported exclusively via the returned <see langword="bool"/> value.
/// </para>
/// <para>
/// This interface is not intended to be implemented directly by consumers.
/// Instead, implementations should derive from
/// <see cref="BencodeSerializer{TOrigin, TTarget}"/>, which provides a safe
/// and strongly typed adapter layer.
/// </para>
/// </remarks>
public interface IBencodeSerializer
{
    /// <summary>
    /// Attempts to serialize a CLR value into its corresponding Bencode object.
    /// </summary>
    /// <param name="input">
    /// The CLR value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// <see cref="IBobject"/> representation of <paramref name="input"/>;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if serialization succeeded; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    bool TrySerialize(object input, out IBobject? output);

    /// <summary>
    /// Attempts to deserialize a Bencode object into its CLR representation.
    /// </summary>
    /// <param name="input">
    /// The Bencode object to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// CLR value; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if deserialization succeeded; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    bool TryDeserialize(IBobject input, out object? output);

    /// <summary>
    /// Serializes a CLR value directly to a <see cref="Stream"/> in Bencode format asynchronously.
    /// </summary>
    /// <param name="input">
    /// The CLR value to serialize. Implementations should handle type validation internally.
    /// </param>
    /// <param name="stream">
    /// The target <see cref="Stream"/> to which the Bencoded bytes will be written.
    /// Must be writable. The method does not close or dispose the stream.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to abort the operation.  
    /// If cancellation is requested, the method should throw <see cref="OperationCanceledException"/>
    /// as soon as possible. The stream may be partially written when cancellation occurs.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous serialization operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides a streaming serialization path that avoids creating
    /// intermediate <see cref="IBobject"/> representations. It is intended to
    /// improve performance and reduce memory allocations when writing large or
    /// complex objects or collections.
    /// </para>
    /// <para>
    /// Implementations are expected to fully handle internal errors and should
    /// not throw exceptions for expected serialization failures.  
    /// Any failure to serialize the <paramref name="input"/> value must be handled gracefully;
    /// implementers may choose to write nothing or perform partial writes, depending
    /// on their design. Exceptions should only be thrown for truly unexpected conditions
    /// (e.g., invalid stream state) or cancellation.
    /// </para>
    /// <para>
    /// This method is compatible with streams that support asynchronous I/O, and
    /// callers are responsible for ensuring the stream remains valid for the
    /// duration of the operation.
    /// </para>
    /// </remarks>
    Task WriteToStreamAsync(object input, Stream stream, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the base contract for serializing and deserializing CLR values
/// to and from Bencode objects, including support for direct asynchronous
/// streaming of serialized data.
/// </summary>
/// <typeparam name="TOrigin">
/// The CLR type being serialized or deserialized.
/// </typeparam>
/// <typeparam name="TTarget">
/// The Bencode object type used as the serialized representation.
/// </typeparam>
/// <remarks>
/// <para>
/// Implementations are expected to provide symmetrical serialization and
/// deserialization semantics whenever possible, allowing round-trip conversion
/// between <typeparamref name="TOrigin"/> and <typeparamref name="TTarget"/>.
/// </para>
/// <para>
/// All methods must be non-throwing and signal failure exclusively via the
/// returned <see langword="bool"/> value, except for runtime type mismatches
/// in interface-level streaming operations, which may throw <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// The <see cref="WriteToStreamAsync(TOrigin, Stream, CancellationToken)"/> method
/// enables high-performance serialization directly to a <see cref="Stream"/>,
/// avoiding intermediate <typeparamref name="TTarget"/> allocations whenever
/// possible. Implementers may fall back to <see cref="TrySerialize(TOrigin,out TTarget)"/>
/// if a direct streaming path is not feasible. Cancellation requests must
/// be respected, and partial writes may occur if the operation is aborted.
/// </para>
/// <para>
/// Consumers of this class should rely on the strongly typed generic methods
/// when possible. The non-generic <see cref="IBencodeSerializer"/> interface
/// implementations exist to support runtime type resolution and dynamic dispatch.
/// </para>
/// </remarks>
public abstract class BencodeSerializer<TOrigin, TTarget> : IBencodeSerializer
    where TTarget : IBobject
{
    /// <summary>
    /// Attempts to serialize a CLR value into its corresponding Bencode object.
    /// </summary>
    /// <param name="input">
    /// The CLR value to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the serialized
    /// Bencode object; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if serialization succeeded; otherwise, <see langword="false"/>.
    /// </returns>
    public abstract bool TrySerialize(TOrigin input, out TTarget? output);

    /// <summary>
    /// Attempts to deserialize a Bencode object into its CLR representation.
    /// </summary>
    /// <param name="input">
    /// The Bencode object to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized
    /// CLR value; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.
    /// </returns>
    public abstract bool TryDeserialize(TTarget input, out TOrigin? output);

    /// <summary>
    /// Asynchronously serializes a <typeparamref name="TOrigin"/> value directly to a <see cref="Stream"/> in Bencode format.
    /// </summary>
    /// <param name="input">
    /// The CLR value to serialize.
    /// </param>
    /// <param name="stream">
    /// The target <see cref="Stream"/> to which the Bencoded bytes will be written.
    /// Must be writable. This method does not close or dispose the stream.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to abort the operation.
    /// If cancellation is requested, the method should throw <see cref="OperationCanceledException"/>
    /// as soon as possible. The stream may be partially written when cancellation occurs.
    /// </param>
    public abstract Task WriteToStreamAsync(TOrigin input, Stream stream, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    bool IBencodeSerializer.TrySerialize(object input, out IBobject? output)
    {
        output = null;

        if (input is not TOrigin typedInput)
            return false;

        if (!TrySerialize(typedInput, out var typedOutput))
            return false;

        output = typedOutput;
        return true;
    }

    /// <inheritdoc />
    bool IBencodeSerializer.TryDeserialize(IBobject input, out object? output)
    {
        output = null;

        if (input is not TTarget typedInput)
            return false;

        if (!TryDeserialize(typedInput, out var typedOutput))
            return false;

        output = typedOutput;
        return true;
    }

    /// <inheritdoc />
    async Task IBencodeSerializer.WriteToStreamAsync(object input, Stream stream, CancellationToken cancellationToken)
    {
        if (input is not TOrigin typedInput)
            throw new InvalidOperationException();

        await WriteToStreamAsync(typedInput, stream, cancellationToken);
    }
}

/// <summary>
/// Base class for Bencode serializers targeting reference types.
/// </summary>
/// <typeparam name="TOrigin">
/// The reference type being serialized or deserialized.
/// </typeparam>
/// <typeparam name="TTarget">
/// The Bencode object type used as the serialized representation.
/// </typeparam>
/// <remarks>
/// This specialization enforces that <typeparamref name="TOrigin"/> is a reference
/// type with a public parameterless constructor, enabling safe instantiation
/// during deserialization.
/// </remarks>
public abstract class ReferenceTypeBencodeSerializer<TOrigin, TTarget>
    : BencodeSerializer<TOrigin, TTarget>
    where TOrigin : class
    where TTarget : IBobject
{
    /// <inheritdoc />
    public abstract override bool TrySerialize(TOrigin input, out TTarget? output);

    /// <inheritdoc />
    public abstract override bool TryDeserialize(TTarget input, out TOrigin? output);

    /// <inheritdoc />
    public abstract override Task WriteToStreamAsync(TOrigin input, Stream stream, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for Bencode serializers targeting unmanaged value types.
/// </summary>
/// <typeparam name="TOrigin">
/// The unmanaged value type being serialized or deserialized.
/// </typeparam>
/// <typeparam name="TTarget">
/// The Bencode object type used as the serialized representation.
/// </typeparam>
/// <remarks>
/// This specialization is intended for primitive or blittable value types
/// where the deserialized value is always fully defined when the operation
/// succeeds.
/// </remarks>
public abstract class UnmanagedTypeBencodeSerializer<TOrigin, TTarget>
    : BencodeSerializer<TOrigin, TTarget>
    where TOrigin : unmanaged
    where TTarget : IBobject
{
    /// <inheritdoc />
    public abstract override bool TrySerialize(TOrigin input, out TTarget? output);

    /// <inheritdoc />
    public abstract override bool TryDeserialize(TTarget input, out TOrigin output);

    /// <inheritdoc />
    public abstract override Task WriteToStreamAsync(TOrigin input, Stream stream, CancellationToken cancellationToken = default);
}
