using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet.Attributes;

/// <summary>
/// Specifies a custom Bencode serializer to be used for a CLR type.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BencodeSerializerAttribute"/> enables a type to explicitly declare
/// the serializer implementation that should be used to serialize and deserialize
/// instances of that type.
/// </para>
/// <para>
/// When applied, this attribute participates in the first tier of the serializer
/// resolution process. The <see cref="BencodeSerializer"/> resolver will inspect
/// the annotated CLR type for this attribute and, if present, attempt to resolve
/// and instantiate the declared serializer before consulting the built-in
/// serializer registry.
/// </para>
/// <para>
/// The specified serializer type <b>must derive from</b>
/// <see cref="BencodeSerializer{TOrigin, TTarget}"/> where <c>TOrigin</c> is the
/// annotated CLR type. Serializers that only implement
/// <see cref="IBencodeSerializer"/> without inheriting from
/// <see cref="BencodeSerializer{TOrigin, TTarget}"/> are not supported and will
/// be rejected by the serializer resolver.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class BencodeSerializerAttribute : Attribute
{
    /// <summary>
    /// Gets the concrete serializer type associated with the annotated CLR type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value returned by this property represents the concrete
    /// <see cref="IBencodeSerializer"/> implementation that the serializer
    /// resolver should attempt to use when serializing or deserializing instances
    /// of the annotated type.
    /// </para>
    /// <para>
    /// The specified type is required to implement <see cref="IBencodeSerializer"/>
    /// and is expected to derive from
    /// <see cref="BencodeSerializer{TOrigin, TTarget}"/> for the annotated CLR type.
    /// </para>
    /// </remarks>
    public Type SerializerType { get; }

    /// <summary>
    /// Gets the optional constructor arguments supplied to the serializer type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The values contained in this array are forwarded to the constructor of the
    /// serializer type when an instance is created by the serializer resolver.
    /// </para>
    /// <para>
    /// If no arguments are provided, the resolver will attempt to invoke the
    /// parameterless constructor of the serializer type.
    /// </para>
    /// <para>
    /// This property may be <see langword="null"/> if no constructor arguments
    /// were specified.
    /// </para>
    /// </remarks>
    public object?[]? Arguments { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializerAttribute"/>
    /// class with the specified serializer type and optional constructor arguments.
    /// </summary>
    /// <param name="serializerType">
    /// The concrete <see cref="IBencodeSerializer"/> implementation to associate
    /// with the annotated CLR type.
    /// </param>
    /// <param name="arguments">
    /// Optional arguments forwarded to the serializer type's constructor during
    /// instantiation.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serializerType"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="serializerType"/> does not implement
    /// <see cref="IBencodeSerializer"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This constructor performs immediate validation to ensure that the supplied
    /// <paramref name="serializerType"/> implements
    /// <see cref="IBencodeSerializer"/>. This validation occurs at attribute
    /// construction time and prevents invalid serializer declarations from being
    /// silently accepted.
    /// </para>
    /// </remarks>
    public BencodeSerializerAttribute(Type serializerType, params object?[]? arguments)
    {
        SerializerType = serializerType ?? throw new ArgumentNullException(nameof(serializerType));

        if (!serializerType.IsAssignableTo(typeof(IBencodeSerializer)))
        {
            throw new InvalidOperationException(
                $"Serializer '{serializerType}' does not implement IBencodeSerializer.");
        }

        Arguments = arguments;
    }
}
