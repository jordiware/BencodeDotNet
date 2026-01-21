using Jordiware.BencodeDotNet.Objects;
using Jordiware.BencodeDotNet.Serializers;

namespace Jordiware.BencodeDotNet;

public sealed class Bencoder
{
    private readonly BencodeOptions _options;

    public Bencoder(BencodeOptions? options = default)
    {
        _options = options ?? new();
    }

    public IBobject Encode(object? origin)
    {
        if (origin is null)
            throw new ArgumentNullException(nameof(origin));

        var type = origin.GetType();

        if (!BencodeSerializer.TryGetSerializerForType(type, out var serializer) || serializer is null)
            throw new NotSupportedException($"No Bencode serializer is registered or declared for type '{type}'.");

        if (!serializer.TrySerialize(origin, out var result))
            throw new InvalidOperationException($"The serializer '{serializer.GetType()}' failed to serialize an instance of '{type}'.");

        if (result is null)
            throw new InvalidOperationException($"The serializer '{serializer.GetType()}' produced a null Bencode object.");

        _options.Validate(result);

        return result;
    }
}
