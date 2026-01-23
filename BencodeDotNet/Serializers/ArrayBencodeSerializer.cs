using Jordiware.BencodeDotNet.Objects;

namespace Jordiware.BencodeDotNet.Serializers;

/// <summary>
/// Provides Bencode serialization and deserialization support for CLR arrays
/// of type <typeparamref name="TType"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ArrayBencodeSerializer{TType}"/> encodes arrays as Bencode lists
/// (<see cref="Blist"/>), where each array element is serialized using the
/// resolved Bencode serializer for <typeparamref name="TType"/>.
/// </para>
/// <para>
/// Serializer resolution for the array element type is performed once per
/// operation via <see cref="BencodeSerializer.TryGetSerializerForType(Type, out BencodeSerializer?)"/>.
/// If no compatible serializer is available for <typeparamref name="TType"/>,
/// both serialization and deserialization fail.
/// </para>
/// <para>
/// This serializer is reference-type based and therefore rejects
/// <see langword="null"/> input values. A <see langword="null"/> array or
/// <see langword="null"/> Bencode input results in a failed operation rather
/// than an exception.
/// </para>
/// <para>
/// All elements are processed sequentially, and the operation fails immediately
/// if any individual element cannot be serialized or deserialized.
/// </para>
/// </remarks>
public sealed class ArrayBencodeSerializer<TType> : ReferenceTypeBencodeSerializer<TType[], Blist>
{
    private readonly BencodeOptions _options;

    /// <summary>
    /// Initializes a new <see cref="ArrayBencodeSerializer{TType}"/> instance
    /// using the default <see cref="BencodeOptions"/> configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This constructor creates a serializer configured with a new
    /// <see cref="BencodeOptions"/> instance using default constraint values
    /// and text encoding.
    /// </para>
    /// <para>
    /// The resulting serializer applies the standard validation and encoding
    /// policies defined by <see cref="BencodeOptions"/> when serializing or
    /// deserializing array values.
    /// </para>
    /// </remarks>
    public ArrayBencodeSerializer()
    {
        _options = new();
    }

    /// <summary>
    /// Initializes a new <see cref="ArrayBencodeSerializer{TType}"/> instance
    /// using the specified <see cref="BencodeOptions"/> configuration.
    /// </summary>
    /// <param name="options">
    /// The <see cref="BencodeOptions"/> instance that defines validation limits
    /// and encoding behavior for this serializer.
    /// </param>
    /// <remarks>
    /// <para>
    /// The provided <paramref name="options"/> instance is retained and used
    /// for all serialization and deserialization operations performed by this
    /// serializer.
    /// </para>
    /// </remarks>
    public ArrayBencodeSerializer(BencodeOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Attempts to serialize a CLR array into a Bencode list.
    /// </summary>
    /// <param name="input">
    /// The array to serialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains a
    /// <see cref="Blist"/> representing the serialized form of
    /// <paramref name="input"/>. When this method returns
    /// <see langword="false"/>, this parameter is set to <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the array was successfully serialized;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Serialization fails if <paramref name="input"/> is <see langword="null"/>,
    /// if no Bencode serializer is available for <typeparamref name="TType"/>,
    /// or if serialization of any array element fails.
    /// </para>
    /// <para>
    /// Each element is serialized using the resolved serializer for
    /// <typeparamref name="TType"/> and added to the resulting
    /// <see cref="Blist"/> in the same order as in the source array.
    /// </para>
    /// </remarks>
    public override bool TrySerialize(TType[] input, out Blist? output)
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
    /// Attempts to deserialize a Bencode list into a CLR array.
    /// </summary>
    /// <param name="input">
    /// The <see cref="Blist"/> to deserialize.
    /// </param>
    /// <param name="output">
    /// When this method returns <see langword="true"/>, contains an array of
    /// <typeparamref name="TType"/> representing the deserialized form of
    /// <paramref name="input"/>. When this method returns
    /// <see langword="false"/>, this parameter is set to <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the list was successfully deserialized;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Deserialization fails if <paramref name="input"/> is <see langword="null"/>,
    /// if no Bencode serializer is available for <typeparamref name="TType"/>,
    /// or if deserialization of any list element fails.
    /// </para>
    /// <para>
    /// Each element of the <see cref="Blist"/> is deserialized using the resolved
    /// serializer for <typeparamref name="TType"/> and placed into the resulting
    /// array in the same order.
    /// </para>
    /// </remarks>
    public override bool TryDeserialize(Blist input, out TType[]? output)
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

        output = objects!.ToArray();
        return true;
    }
}
