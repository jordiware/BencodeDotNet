# v0.3.0

## Added
- Reflection-based serializer for complex/POCO types
- Asynchronous Stream-based reader and writer APIs
- Custom `BencodeException` types

## Changed
- `Bencoder` type name to `BencodeEncoder`
- `Bdecoder` type name to `BencodeDecoder`
- Changed thrown exceptions to custom `BencodeException` types

# v0.2.0

## Added
- Synchronous Bencode encoding
- Synchronous and asynchronous Bencode decoding APIs
- Built-in serializers for primitive and collection types
- Extendable serializer model for user-defined types
- Fully enforced and extended `BencodeOptions`

## Changed
- Decoding and validation behavior now consistently honors `BencodeOptions`

# v0.1.1

## Fixed
- Minor edge case handling issues in decoding logic

# v0.1.0

## Added
- Core Bencode primitive types: integer, string, list, and dictionary
- Initial `BencodeOptions` type defining structural and size constraints
- Streamed asynchronous decoder for Bencode data

## Notes
- `BencodeOptions` were partially enforced and not consistently applied across all decoding paths
