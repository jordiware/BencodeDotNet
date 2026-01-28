# v1.0.0-preview1

## Changed
- Improved performance in async writing
- Bencode primitive type names' capitalization: 
  - `IBobject` → `IBObject`
  - `Binteger` → `BInteger`
  - `Bstring` → `BString`
  - `Blist` → `BList`
  - `Bdictionary` → `BDictionary`

---

# v0.3.0

## Added
- Asynchronous Stream-based reader and writer APIs
- Reflection-based serializer for complex/POCO types
- Dedicated `byte[]` serializer: `ByteArraySerializer`
- Custom `BencodeException` types

## Changed
- `Bencoder` type renamed as `BencodeEncoder`
- `Bdecoder` type renamed as `BencodeDecoder`
- `BencodeDecoder` file/stream async reads moved to `BencodeReader`
- Changed thrown exceptions to custom `BencodeException` types

---

# v0.2.0

## Added
- Synchronous Bencode encoding
- Synchronous and asynchronous Bencode decoding APIs
- Built-in serializers for primitive and collection types
- Extendable serializer model for user-defined types
- Fully enforced and extended `BencodeOptions`

## Changed
- Decoding and validation behavior now consistently honors `BencodeOptions`

---

# v0.1.1

## Fixed
- Minor edge case handling issues in decoding logic

---

# v0.1.0

## Added
- Core Bencode primitive types: integer, string, list, and dictionary
- Initial `BencodeOptions` type defining structural and size constraints
- Streamed asynchronous decoder for Bencode data

## Notes
- `BencodeOptions` partially enforced and not consistently applied
