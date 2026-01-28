using Jordiware.BencodeDotNet.Attributes;
using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Utils;
using System.IO.Pipelines;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Reflection-based Bencode serializer for complex types.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReflectionBencodeSerializer{TType}"/> provides a generic serializer that maps a public,
/// parameterless-constructible CLR type into a <see cref="BDictionary"/> by inspecting its public
/// instance fields and properties using reflection.
/// </para>
/// <para>
/// Only <b>public instance members</b> are considered. Private, protected, internal, static,
/// compiler-generated, ignored, or init-only members are excluded from serialization and
/// deserialization.
/// </para>
/// <para>
/// Each eligible member is serialized as a dictionary entry whose key is a UTF-8 encoded
/// <see cref="BString"/> derived from the member name or overridden via
/// <see cref="BencodeNameAttribute"/>.
/// </para>
/// <para>
/// Member metadata is computed once per closed generic type and cached using lazy initialization.
/// Serializer resolution failures or invalid type configurations will surface on first use.
/// </para>
/// </remarks>
/// <typeparam name="TType">
/// The CLR type to serialize and deserialize. Must be non-nullable and expose a public
/// parameterless constructor.
/// </typeparam>
public sealed class ReflectionBencodeSerializer<TType> : BencodeSerializer<TType, BDictionary>
    where TType : notnull, new()
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Lazy<TypeMetadata> metadata = new(GetTypeMetadata, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Serializes a CLR object into a <see cref="BDictionary"/> by reflecting over its public members.
    /// </summary>
    /// <param name="input">The object instance to serialize.</param>
    /// <param name="output">
    /// When this method returns <c>true</c>, contains the resulting <see cref="BDictionary"/>.
    /// </param>
    /// <returns>
    /// Always returns <c>true</c>. Failures are reported via exceptions.
    /// </returns>
    /// <exception cref="BencodeSerializerException">
    /// Thrown if a member value cannot be serialized using its resolved Bencode serializer.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Only public instance fields and properties discovered at metadata initialization time
    /// are considered.
    /// </para>
    /// <para>
    /// Members whose value is <c>null</c> are skipped and not emitted into the resulting dictionary.
    /// </para>
    /// <para>
    /// Dictionary keys are emitted in deterministic, lexicographically sorted order based on
    /// their <see cref="BString"/> representation.
    /// </para>
    /// </remarks>
    public override bool TrySerialize(TType input, out BDictionary? output)
    {
        var dict = new Dictionary<BString, IBObject>();

        foreach (var member in metadata.Value.Members)
        {
            var value = member.Getter(input);

            if (value is null)
                continue;

            if (!member.Serializer.TrySerialize(value, out var bvalue) || bvalue is null)
                throw new BencodeSerializerException($"Failed to serialize member '{member.Name}' of type '{member.MemberType}'.");

            dict.Add(member.Key, bvalue);
        }

        output = new BDictionary(dict);
        return true;
    }

    /// <summary>
    /// Deserializes a <see cref="BDictionary"/> into a new instance of <typeparamref name="TType"/>.
    /// </summary>
    /// <param name="input">The source <see cref="BDictionary"/>.</param>
    /// <param name="output">
    /// When this method returns <c>true</c>, contains the deserialized object instance.
    /// </param>
    /// <returns>
    /// Always returns <c>true</c>. Failures are reported via exceptions.
    /// </returns>
    /// <exception cref="BencodeSerializerException">
    /// Thrown if a dictionary value cannot be deserialized into the corresponding member type.
    /// </exception>
    /// <remarks>
    /// <para>
    /// A new instance of <typeparamref name="TType"/> is created using its public parameterless
    /// constructor.
    /// </para>
    /// <para>
    /// Dictionary entries whose keys do not match any known public member are ignored.
    /// </para>
    /// <para>
    /// Members that are not present in the dictionary are left unchanged on the target instance.
    /// This allows nullable and optional members to be omitted safely.
    /// </para>
    /// </remarks>
    public override bool TryDeserialize(BDictionary input, out TType? output)
    {
        var instance = new TType();

        foreach (var (key, bvalue) in input)
        {
            if (!metadata.Value.TryGetMetadataForKey(key, out var member))
                continue;

            if (!member.Serializer.TryDeserialize(bvalue, out var value))
                throw new BencodeSerializerException($"Failed to deserialize member '{member.Name}' (key '{key}') of type '{member.MemberType}'.");

            member.Setter(instance, value);
        }

        output = instance;
        return true;
    }

    /// <summary>
    /// Asynchronously serializes a POCO or complex object to the provided
    /// <see cref="PipeWriter"/> in Bencode dictionary format.
    /// </summary>
    /// <param name="input">
    /// The object to serialize. Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="writer">
    /// The <see cref="PipeWriter"/> to which the serialized dictionary will be written.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous write operation.
    /// </returns>
    /// <remarks>
    /// Each public or attributed member of the object is serialized as a key-value
    /// pair, using precomputed metadata for efficient access.  
    /// Member keys are written as Bencode strings, and null values are skipped.  
    /// This implementation mirrors <c>TrySerialize</c> semantics and may allocate
    /// small temporary byte arrays for dictionary delimiters and keys.
    /// </remarks>
    public override async Task WriteToPipeAsync(TType input, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        Span<byte> span = writer.GetSpan(1);
        span[0] = Bencode.DictionaryBeginCharacter;
        writer.Advance(1);

        foreach (var member in metadata.Value.Members)
        {
            var value = member.Getter(input);

            if (value is null)
                continue;

            await member.Key.WriteToPipeAsync(writer, cancellationToken);
            await member.Serializer.WriteToPipeAsync(value, writer, cancellationToken);
        }

        span = writer.GetSpan(1);
        span[0] = Bencode.TerminationCharacter;
        writer.Advance(1);
    }

    private static TypeMetadata GetTypeMetadata()
    {
        var fields = typeof(TType).GetFields(Flags)
                                    .Where(f => !f.IsStatic &&
                                                !f.IsInitOnly &&
                                                !f.IsDefined(typeof(CompilerGeneratedAttribute), false) &&
                                                !f.IsDefined(typeof(BencodeIgnoreAttribute), inherit: false));

        var properties = typeof(TType).GetProperties(Flags)
                                        .Where(p => p.GetMethod != null &&
                                                    p.SetMethod != null &&
                                                    !p.SetMethod.IsStatic &&
                                                    p.GetIndexParameters().Length == 0 &&
                                                    !IsInitOnly(p) &&
                                                    !p.IsDefined(typeof(BencodeIgnoreAttribute), inherit: false));

        var members = new List<MemberMetadata>();
        var keyset = new HashSet<BString>();
        foreach (var info in fields)
        {
            var effectiveType = Nullable.GetUnderlyingType(info.FieldType) ?? info.FieldType;

            if (!BencodeSerializer.TryGetSerializerForType(effectiveType, new(), out var serializer) || serializer is null)
                throw new BencodeSerializerNotFoundException($"No Bencode serializer found for member '{info.Name}' of type '{info.FieldType}'.");

            var nameAttribute = info.GetCustomAttribute<BencodeNameAttribute>(false);

            var metadata = new MemberMetadata
            {
                Name = info.Name,
                Key = nameAttribute?.Name ?? new BString(info.Name, Encoding.UTF8),
                MemberType = info.FieldType,
                Getter = info.GetValue,
                Setter = info.SetValue,
                Serializer = serializer
            };

            if (keyset.Contains(metadata.Key))
                throw new BencodeFormatException($"Duplicate Bencode key '{metadata.Key}' in type '{typeof(TType)}'.");

            keyset.Add(metadata.Key);
            members.Add(metadata);
        }
        foreach (var info in properties)
        {
            var effectiveType = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;

            if (!BencodeSerializer.TryGetSerializerForType(effectiveType, new(), out var serializer) || serializer is null)
                throw new BencodeSerializerNotFoundException($"No Bencode serializer found for member '{info.Name}' of type '{info.PropertyType}'.");

            var nameAttribute = info.GetCustomAttribute<BencodeNameAttribute>(false);

            var metadata = new MemberMetadata
            {
                Name = info.Name,
                Key = nameAttribute?.Name ?? new BString(info.Name, Encoding.UTF8),
                MemberType = info.PropertyType,
                Getter = info.GetValue,
                Setter = info.SetValue,
                Serializer = serializer
            };

            if (keyset.Contains(metadata.Key))
                throw new BencodeFormatException($"Duplicate Bencode key '{metadata.Key}' in type '{typeof(TType)}'.");

            keyset.Add(metadata.Key);
            members.Add(metadata);
        }

        members.Sort((a, b) => a.Key.CompareTo(b.Key));

        var membersMetadata = members.ToArray();
        var memberIndexes = new Dictionary<BString, int>(membersMetadata.Length);
        for (int i = 0; i < membersMetadata.Length; i++)
        {
            memberIndexes[membersMetadata[i].Key] = i;
        }

        return new TypeMetadata
        {
            Members = membersMetadata,
            IndexByKey = memberIndexes
        };
    }

    private static bool IsInitOnly(PropertyInfo p) => p.SetMethod!
                                                       .ReturnParameter
                                                       .GetRequiredCustomModifiers()
                                                       .Contains(typeof(IsExternalInit));

    private sealed class TypeMetadata
    {
        public required MemberMetadata[] Members;
        public required IDictionary<BString, int> IndexByKey;

        public bool TryGetMetadataForKey(BString key, out MemberMetadata metadata)
        {
            if (!IndexByKey.TryGetValue(key, out var index))
            {
                metadata = null!;
                return false;
            }

            metadata = Members[index];
            return true;
        }
    }

    private sealed class MemberMetadata
    {
        public required string Name;
        public required BString Key;

        public required Type MemberType;

        public required Func<object, object?> Getter;
        public required Action<object, object?> Setter;

        public required IBencodeSerializer Serializer;
    }
}
