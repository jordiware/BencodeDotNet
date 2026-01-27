using Jordiware.BencodeDotNet.Attributes;
using System.Collections.Immutable;
using System.Reflection;

namespace Jordiware.BencodeDotNet.Serializers;

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
    /// <param name="options">
    /// The <see cref="BencodeOptions"/> instance providing configuration and policy
    /// information required for serializer resolution.
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
    /// <see langword="true"/> if a serializer was successfully resolved and
    /// instantiated; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method attempts serializer resolution using the following strategy,
    /// in order:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       Attempts to resolve a serializer explicitly declared on the target
    ///       <paramref name="type"/> via a serializer attribute.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Attempts to resolve a composite serializer for enumerable or dictionary
    ///       types using the provided <paramref name="options"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Looks up a registered serializer type in the internal
    ///       <see cref="TypeSerializers"/> registry and attempts to instantiate it.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// If the resolved serializer type requires configuration from
    /// <paramref name="options"/>, such as text encoding, the appropriate constructor
    /// arguments are supplied automatically.
    /// </para>
    /// <para>
    /// This method follows a non-throwing pattern: any failure during resolution,
    /// type compatibility checks, or instantiation results in a
    /// <see langword="false"/> return value. No exceptions are propagated to the
    /// caller.
    /// </para>
    /// </remarks>
    public static bool TryGetSerializerForType(Type type, BencodeOptions options, out IBencodeSerializer? instance, params object?[]? args)
    {
        instance = default;

        if (TryResolveFromAttribute(type, out instance))
            return true;

        if (TryResolveEnumerableSerializer(type, options, out instance))
            return true;

        if (TypeSerializers.TryGetValue(type, out var serializer))
        {
            if (!typeof(IBencodeSerializer).IsAssignableFrom(serializer))
                return false;

            try
            {
                if (typeof(StringBencodeSerializer).IsAssignableFrom(serializer))
                    args = [options.TextEncoding];

                instance = Activator.CreateInstance(serializer, args) as IBencodeSerializer;
                return instance is not null;
            }
            catch
            {
                return false;
            }
        }

        try
        {
            if (type == typeof(object))
                return false;

            if (type.IsAbstract ||
                type.IsInterface ||
                type.IsPrimitive ||
                type.IsEnum ||
                type.IsPointer ||
                type.IsByRef ||
                type.IsGenericTypeDefinition)
                return false;

            if (type.GetConstructor(Type.EmptyTypes) is null)
                return false;

            var serializerType = typeof(ReflectionBencodeSerializer<>).MakeGenericType(type);
            instance = Activator.CreateInstance(serializerType) as IBencodeSerializer;
            return instance is not null;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryResolveFromAttribute(Type type, out IBencodeSerializer? serializer)
    {
        serializer = default;

        var attribute = type.GetCustomAttribute<BencodeSerializerAttribute>();
        if (attribute is null)
            return false;

        return TryCreateFromAttribute(type, attribute, out serializer);
    }

    private static bool TryCreateFromAttribute(Type type, BencodeSerializerAttribute attribute, out IBencodeSerializer? serializer)
    {
        serializer = default;

        if (attribute.SerializerType.ContainsGenericParameters)
            return false;

        if (!DerivesFromBencodeSerializerForOrigin(attribute.SerializerType, type))
            return false;

        var instance = Activator.CreateInstance(attribute.SerializerType, attribute.Arguments);

        if (instance is not IBencodeSerializer typed)
            return false;

        serializer = typed;
        return true;
    }

    private static bool TryResolveEnumerableSerializer(Type type, BencodeOptions options, out IBencodeSerializer? instance)
    {
        instance = default;

        if (type == typeof(string))
            return false;

        if (type == typeof(byte[]))
        {
            instance = new ByteArraySerializer();
            return true;
        }

        var typeInterfaces = type.IsInterface ? (new Type[] { type }).Concat(type.GetInterfaces()).ToArray() : type.GetInterfaces();
        if (typeInterfaces is null || typeInterfaces.Length == 0)
            return false;

        object?[]? args = [options];

        // IDictionary<TKey, TValue>
        var dictionaryType = typeInterfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictionaryType is not null)
        {
            var dictionaryDefinition = dictionaryType.GetGenericTypeDefinition();
            var dictionaryArguments = dictionaryType.GetGenericArguments();

            var serializerType = typeof(DictionaryBencodeSerializer<,>).MakeGenericType(dictionaryArguments);

            try
            {
                instance = Activator.CreateInstance(serializerType, args) as IBencodeSerializer;
                return instance is not null;
            }
            catch
            {
                return false;
            }
        }

        var enumerableType = typeInterfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerableType is not null)
        {
            var isArray = type.BaseType == typeof(Array);
            var enumerableDefinition = enumerableType.GetGenericTypeDefinition();
            var enumerableArguments = enumerableType.GetGenericArguments();

            // IEnumerable<T>
            var serializerType = isArray ? typeof(ArrayBencodeSerializer<>).MakeGenericType(enumerableArguments)
                                         : typeof(EnumerableBencodeSerializer<>).MakeGenericType(enumerableArguments);

            try
            {
                instance = Activator.CreateInstance(serializerType, args) as IBencodeSerializer;
                return instance is not null;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static bool DerivesFromBencodeSerializerForOrigin(Type serializerType, Type targetType)
    {
        for (var current = serializerType; current is not null; current = current.BaseType)
        {
            if (!current.IsGenericType)
                continue;

            var definition = current.GetGenericTypeDefinition();
            if (definition != typeof(BencodeSerializer<,>))
                continue;

            var args = current.GetGenericArguments();
            return args[0] == targetType;
        }

        return false;
    }
}
