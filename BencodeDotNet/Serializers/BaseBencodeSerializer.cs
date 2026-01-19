using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Serializers;

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
public abstract class BaseBencodeSerializer<TOrigin, TTarget>
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
    : BaseBencodeSerializer<TOrigin, TTarget>
    where TOrigin : class, new()
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
    : BaseBencodeSerializer<TOrigin, TTarget>
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
