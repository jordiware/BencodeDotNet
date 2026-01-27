using Jordiware.BencodeDotNet.Objects;
using System.IO.Pipelines;

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
    /// Serializes a CLR value directly into a <see cref="PipeWriter"/> in Bencode format asynchronously.
    /// </summary>
    /// <param name="input">
    /// The CLR value to serialize. Implementations are responsible for validating
    /// that the runtime type of <paramref name="input"/> is supported.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the Bencoded bytes will be written.
    /// The writer is owned by the caller and must not be completed, flushed, or disposed
    /// by the implementation.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to cancel the operation. If cancellation
    /// is observed, <see cref="OperationCanceledException"/> should be thrown.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous serialization operation.
    /// </returns>
    /// <remarks>
    /// This method writes Bencode data directly into the provided buffer without
    /// creating intermediate <see cref="IBobject"/> instances. It is intended for
    /// high-performance scenarios and may produce partial output if cancelled or
    /// if an unexpected failure occurs.
    /// </remarks>
    Task WriteToPipeAsync(object input, PipeWriter writer, CancellationToken cancellationToken = default);
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
/// The <see cref="WriteToPipeAsync(TOrigin, Stream, CancellationToken)"/> method
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
    /// Asynchronously serializes the specified <typeparamref name="TOrigin"/> value
    /// into the provided <see cref="PipeWriter"/> using Bencode format, as a fallback
    /// implementation.
    /// </summary>
    /// <param name="input">
    /// The CLR value to serialize. Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the Bencoded bytes will be written.
    /// The writer is owned by the caller and must not be completed, flushed, or disposed
    /// by this method.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous serialization operation.
    /// </returns>
    /// <remarks>
    /// This fallback implementation serializes <paramref name="input"/> to a
    /// <typeparamref name="TTarget"/> using <see cref="TrySerialize(TOrigin,out TTarget)"/>,
    /// then writes the complete binary encoding to the <see cref="PipeWriter"/>.
    /// It allocates an intermediate buffer and should be overridden by
    /// high-performance serializers.
    /// </remarks>
    public async virtual Task WriteToPipeAsync(TOrigin input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (!TrySerialize(input, out var bobject) || bobject is null)
            throw new BencodeSerializerException($"Serialization of {typeof(TOrigin)} failed; no Bencode object was produced.");

        await bobject.WriteToPipeAsync(writer, cancellationToken);
    }

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
    async Task IBencodeSerializer.WriteToPipeAsync(object input, PipeWriter writer, CancellationToken cancellationToken)
    {
        if (input is not TOrigin typedInput)
            throw new ArgumentException($"Input is not of type '{typeof(TOrigin)}'");

        await WriteToPipeAsync(typedInput, writer, cancellationToken);
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
    public abstract override Task WriteToPipeAsync(TOrigin input, PipeWriter writer, CancellationToken cancellationToken = default);
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
    public abstract override Task WriteToPipeAsync(TOrigin input, PipeWriter writer, CancellationToken cancellationToken = default);
}
