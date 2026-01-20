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
}

/// <summary>
/// Defines the base contract for serializing and deserializing CLR values
/// to and from Bencode objects.
/// </summary>
/// <typeparam name="TOrigin">
/// The CLR type being serialized or deserialized.
/// </typeparam>
/// <typeparam name="TTarget">
/// The Bencode object type used as the serialized representation.
/// </typeparam>
/// <remarks>
/// Implementations are expected to provide symmetrical serialization and
/// deserialization semantics whenever possible, allowing round-trip conversion
/// between <typeparamref name="TOrigin"/> and <typeparamref name="TTarget"/>.
/// 
/// All methods must be non-throwing and signal failure exclusively via the
/// returned <see langword="bool"/> value.
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

    /// <summary>
    /// Attempts to deserialize a Bencode object into an unmanaged CLR value.
    /// </summary>
    /// <param name="input">
    /// The Bencode object to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains the deserialized value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.
    /// </returns>
    public abstract override bool TryDeserialize(TTarget input, out TOrigin output);
}
