# Reading and Decoding

This document explains how to **read** and **decode** Bencode data using BencodeDotNet.

It focuses on _when_ to use each API, _what problems they solve_, and _typical usage patterns_ — not on internal parsing mechanics.

---

## Two Ways to Consume Bencode Data

BencodeDotNet provides **two complementary entry points** for consuming Bencode data:

| API              | Purpose                                      | Typical Use Case                                      |
| ---------------- | -------------------------------------------- | ----------------------------------------------------- |
| `BencodeDecoder` | Decode a single, complete Bencode value      | In-memory data, buffers, protocol messages            |
| `BencodeReader`  | Stream and incrementally read Bencode values | Files, network streams, multiple concatenated objects |

A good rule of thumb:

> **If you already have the full data in memory, use `BencodeDecoder`.**
> **If the data comes from a stream or may contain multiple objects, use `BencodeReader`.**

---

## BencodeDecoder — Decoding a Single Value

`BencodeDecoder` is designed for **strict, single-value decoding**.

It expects _exactly one_ top-level Bencode object and **rejects any trailing data**.

### When to Use

Use `BencodeDecoder` when:

- You have a full Bencode value in memory
- You expect exactly one object
- Trailing bytes should be treated as an error
- You want immediate validation

Typical scenarios include:

- Parsing a protocol message
- Decoding a stored metadata value
- Converting a Bencode string or byte buffer into a CLR object

### Basic Decoding

You can decode directly into an `IBobject`:

```csharp
var decoder = new BencodeDecoder();

IBobject obj = decoder.Decode("d3:foo3:bare");
```

Supported input forms:

- `byte[]`
- `ReadOnlySpan<byte>`
- `string` (using the configured text encoding)

### Decoding into CLR Types

If a compatible serializer is registered or declared, you can decode directly into a CLR type:

```csharp
var decoder = new BencodeDecoder();

MyType value = decoder.Decode<MyType>(bytes);
```

Serializer resolution follows the configured discovery rules (attributes first, registry fallback).

### Using an Explicit Serializer

You may also pass a serializer explicitly:

```csharp
var value = decoder.Decode<MyType, Bdictionary>(bytes, mySerializer);
```

This is useful when:

- Multiple serializers exist for a type
- You want explicit control over deserialization
- You are bypassing automatic discovery

### Validation Behavior

All decoded objects are validated using the configured `BencodeOptions`:

- Structural correctness
- Depth limits
- Size constraints
- Format rules

If validation fails, decoding throws a `BencodeFormatException` or `BencodeValidationException`.

---

## BencodeReader — Streaming and Incremental Reading

`BencodeReader` is designed for **stream-based consumption**.

It reads from a `Stream` and emits **fully parsed top-level objects** as soon as they are complete.

### When to Use

Use `BencodeReader` when:

- Data comes from a file or network stream
- You cannot (or do not want to) buffer everything in memory
- The input may contain **multiple consecutive Bencode objects**
- You want incremental processing

Typical scenarios include:

- Reading `.torrent`-style data files
- Consuming a continuous Bencode stream
- Processing large datasets

---

## Reading Multiple Objects

The most common streaming pattern is reading _multiple_ top-level objects.

### Reading as `IBobject`

```csharp
var reader = new BencodeReader();

await foreach (var obj in reader.ReadMultipleAsync(stream))
{
    // Each obj is a fully parsed and validated IBobject
}
```

Key characteristics:

- Objects are yielded **as soon as they are complete**
- Parsing continues until the end of the stream
- Validation is applied to every object
- Partial objects at EOF cause an exception

### Reading and Deserializing

You can deserialize each object as it is read:

```csharp
await foreach (var item in reader.ReadMultipleAsync<MyType>(stream))
{
    // item is a deserialized MyType instance
}
```

Serializer resolution happens once, before reading begins.

If deserialization fails for any object, enumeration stops with an exception.

---

## Reading a Single Object from a Stream

If you only care about the **first** top-level object in a stream, use the single-object APIs.

```csharp
var reader = new BencodeReader();

IBobject obj = await reader.ReadSingleAsync(stream);
```

Behavior:

- Parsing stops immediately after the first complete object
- Remaining stream data is ignored
- Validation is still enforced

### Single Object + Deserialization

```csharp
MyType value = await reader.ReadSingleAsync<MyType>(stream);
```

This is useful when:

- The stream contains framing or extra data
- You only care about the first message
- You want streaming but single-value semantics

---

## Reading from Files

`BencodeReader` provides file-based convenience APIs.

### Multiple Objects from a File

```csharp
await foreach (var obj in reader.ReadMultipleFromFileAsync("data.bencode"))
{
    // Process each object
}
```

### Single Object from a File

```csharp
var obj = await reader.ReadSingleFromFileAsync("data.bencode");
```

Deserializer-based overloads are also available for both cases.

---

## Validation and Error Handling

Both `BencodeDecoder` and `BencodeReader`:

- Enforce validation through `BencodeOptions`
- Fail fast on malformed or invalid data
- Never yield partially parsed objects

Common exception types include:

- `BencodeFormatException` — malformed input or unexpected EOF
- `BencodeValidationException` — option-based validation failures
- `BencodeSerializerException` — deserialization errors
- `BencodeSerializerNotFoundException` — no compatible serializer found

---

## Choosing the Right API

| Scenario                         | Recommended API                   |
| -------------------------------- | --------------------------------- |
| Decode a string or byte buffer   | `BencodeDecoder`                  |
| Expect exactly one object        | `BencodeDecoder`                  |
| Read from a stream incrementally | `BencodeReader`                   |
| Multiple concatenated objects    | `BencodeReader.ReadMultipleAsync` |
| First object only from a stream  | `BencodeReader.ReadSingleAsync`   |

---

## Thread Safety Notes

Neither `BencodeDecoder` nor `BencodeReader` instances are thread-safe.

- Do not use a single instance concurrently
- Create separate instances per operation or per thread

---

This separation between **decoding** and **reading** allows BencodeDotNet to support both strict, in-memory parsing and efficient, streaming-based consumption without compromising validation guarantees.
