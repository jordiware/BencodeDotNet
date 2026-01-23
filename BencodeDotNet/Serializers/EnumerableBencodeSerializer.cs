using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides serialization and deserialization support for <see cref="IEnumerable{T}"/>
/// using the Bencode list representation.
/// </summary>
/// <typeparam name="TType">
/// The element type contained in the enumerable.
/// </typeparam>
/// <remarks>
/// <para>
/// Each element in the enumerable is serialized individually using a serializer
/// resolved for <typeparamref name="TType"/>. Serializer resolution is delegated to
/// <see cref="BencodeSerializer.TryGetSerializerForType(Type, out IBencodeSerializer?)"/>,
/// which applies attribute-based resolution first (via <c>BencodeSerializerAttribute</c>)
/// and falls back to the global serializer registry for non-extensible or primitive types.
/// </para>
/// <para>
/// Serialization produces a <see cref="Blist"/> containing the serialized representation
/// of each element in enumeration order. Deserialization materializes the result eagerly
/// into a concrete collection to avoid deferred execution and lifetime issues.
/// </para>
/// <para>
/// This serializer is strict: serialization or deserialization fails if the enumerable
/// is <c>null</c>, if no compatible serializer for <typeparamref name="TType"/> can be
/// resolved, or if any individual element fails to serialize or deserialize.
/// </para>
/// </remarks>
public sealed class EnumerableBencodeSerializer<TType> : ReferenceTypeBencodeSerializer<IEnumerable<TType>, Blist>
{
    private readonly BencodeOptions _options;

    public EnumerableBencodeSerializer()
    {
        _options = new();
    }

    public EnumerableBencodeSerializer(BencodeOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Attempts to serialize an <see cref="IEnumerable{T}"/> into a <see cref="Blist"/>.
    /// </summary>
    /// <param name="input">
    /// The enumerable to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <c>true</c>, contains the resulting <see cref="Blist"/>
    /// representation of the enumerable; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the enumerable and all of its elements were successfully serialized;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Serialization fails if <paramref name="input"/> is <c>null</c>, if no compatible
    /// serializer for <typeparamref name="TType"/> can be resolved, or if serialization
    /// of any individual element fails.
    /// </remarks>
    public override bool TrySerialize(IEnumerable<TType> input, out Blist? output)
    {
        output = default;
        if (input is null)
            return false;

        if (!BencodeSerializer.TryGetSerializerForType(typeof(TType), _options, out var serializer))
            return false;

        var objects = new List<IBobject>();
        foreach (var item in input)
        {
            if (!serializer!.TrySerialize(item!, out var serialized))
                return false;
            objects.Add(serialized!);
        }

        output = new Blist(objects!);
        return true;
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="Blist"/> into an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Blist"/> to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <c>true</c>, contains a materialized enumerable of
    /// <typeparamref name="TType"/> instances; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the list and all of its elements were successfully deserialized;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Deserialization resolves a serializer for <typeparamref name="TType"/> using the same
    /// attribute-first, registry-fallback strategy as serialization. Each element in the
    /// <see cref="Blist"/> must be compatible with the resolved serializer.
    /// </para>
    /// <para>
    /// The resulting enumerable is eagerly materialized into a concrete collection to ensure
    /// deterministic behavior and to decouple the result from the lifetime of the underlying
    /// <see cref="Blist"/>.
    /// </para>
    /// </remarks>
    public override bool TryDeserialize(Blist input, out IEnumerable<TType>? output)
    {
        output = default;
        if (input is null)
            return false;

        if (!BencodeSerializer.TryGetSerializerForType(typeof(TType), _options, out var serializer))
            return false;

        var objects = new List<TType>();
        foreach (var item in input)
        {
            if (!serializer!.TryDeserialize(item!, out var deserialized))
                return false;
            objects.Add((TType)deserialized!);
        }

        output = objects!;
        return true;
    }
}
