using Jordiware.BencodeDotNet.Objects;
using System.Collections.Immutable;

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
/// Provides a centralized registry and factory for resolving Bencode serializers
/// based on CLR types.
/// </summary>
/// <remarks>
/// <para>
/// This class defines the canonical mapping between supported CLR primitive
/// and framework types and their corresponding Bencode serializer
/// implementations.
/// </para>
/// <para>
/// The registry is immutable, thread-safe, and intentionally explicit. No
/// reflection-based discovery or automatic registration is performed. Only
/// serializers listed in <see cref="TypeSerializers"/> are considered valid
/// built-in serializers.
/// </para>
/// <para>
/// Serializer instances are created on demand using
/// <see cref="Activator.CreateInstance(Type, object?[]?)"/>. All construction
/// failures are handled gracefully and reported via a <see langword="false"/>
/// return value, in accordance with the non-throwing design principles of
/// BencodeDotNet.
/// </para>
/// </remarks>
public static class BencodeSerializer
{
    /// <summary>
    /// Gets the immutable mapping between CLR types and their corresponding
    /// Bencode serializer implementation types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each entry maps a supported CLR type (such as <see cref="int"/> or
    /// <see cref="DateTime"/>) to a concrete serializer type implementing
    /// <see cref="IBencodeSerializer"/>.
    /// </para>
    /// <para>
    /// The values stored in this dictionary are expected to derive from
    /// <see cref="BencodeSerializer{TOrigin, TTarget}"/> and to correctly
    /// implement the serialization and deserialization semantics for the
    /// associated CLR type.
    /// </para>
    /// <para>
    /// This dictionary is immutable and must not be modified at runtime.
    /// </para>
    /// </remarks>
    public static readonly ImmutableDictionary<Type, Type> TypeSerializers = new Dictionary<Type, Type>()
    {
        [typeof(bool)] = typeof(BoolBencodeSerializer),
        [typeof(byte)] = typeof(NumericByteBencodeSerializer),
        [typeof(sbyte)] = typeof(SbyteBencodeSerializer),
        [typeof(short)] = typeof(ShortBencodeSerializer),
        [typeof(ushort)] = typeof(UshortBencodeSerializer),
        [typeof(int)] = typeof(IntBencodeSerializer),
        [typeof(uint)] = typeof(UintBencodeSerializer),
        [typeof(long)] = typeof(LongBencodeSerializer),
        [typeof(ulong)] = typeof(UlongBencodeSerializer),
        [typeof(char)] = typeof(CharBencodeSerializer),
        [typeof(float)] = typeof(FloatBencodeSerializer),
        [typeof(double)] = typeof(DoubleBencodeSerializer),
        [typeof(decimal)] = typeof(DecimalBencodeSerializer),
        [typeof(Guid)] = typeof(GuidBencodeSerializer),
        [typeof(DateTime)] = typeof(DateTimeBencodeSerializer),
        [typeof(DateOnly)] = typeof(DateOnlyBencodeSerializer),
        [typeof(TimeOnly)] = typeof(TimeOnlyBencodeSerializer),
        [typeof(TimeSpan)] = typeof(TimeSpanBencodeSerializer),
        [typeof(string)] = typeof(StringBencodeSerializer)
    }.ToImmutableDictionary();

    /// <summary>
    /// Attempts to resolve and instantiate a Bencode serializer for the specified
    /// CLR type.
    /// </summary>
    /// <param name="type">
    /// The CLR type for which a serializer is requested.
    /// </param>
    /// <param name="instance">
    /// When this method returns <see langword="true"/>, contains an instance of
    /// <see cref="IBencodeSerializer"/> capable of handling the specified
    /// <paramref name="type"/>; otherwise, <see langword="null"/>.
    /// </param>
    /// <param name="args">
    /// Optional constructor arguments forwarded to the serializer's constructor.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a serializer was found and successfully
    /// instantiated; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// </para>
    /// <list type="number">
    /// <item>Looks up the requested <paramref name="type"/> in <see cref="TypeSerializers"/>.</item>
    /// <item>Verifies that the mapped serializer type implements <see cref="IBencodeSerializer"/>.</item>
    /// <item>Attempts to construct an instance using the provided <paramref name="args"/>.</item>
    /// </list>
    /// <para>
    /// Any failure during lookup, type validation, or construction results in a
    /// <see langword="false"/> return value. No exceptions are propagated to the
    /// caller.
    /// </para>
    /// </remarks>
    public static bool TryGetSerializerInstanceForType(Type type, out IBencodeSerializer? instance, params object?[]? args)
    {
        instance = default;
        if (TypeSerializers.TryGetValue(type, out var serializer))
        {
            if (!typeof(IBencodeSerializer).IsAssignableFrom(serializer))
                return false;

            try
            {
                instance = Activator.CreateInstance(serializer, args) as IBencodeSerializer;
                return instance is not null;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }
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
