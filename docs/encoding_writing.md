# Encoding and Writing

This document explains how to **encode** CLR objects into Bencode and **write** Bencoded data using BencodeDotNet.

It focuses on _when_ to use each API, _what guarantees they provide_, and _typical usage patterns_ — not on serializer internals or low-level I/O mechanics.

---

## Two Ways to Produce Bencode Data

BencodeDotNet provides **two complementary entry points** for producing Bencode output:

| API              | Purpose                                                    | Typical Use Case                                   |
| ---------------- | ---------------------------------------------------------- | -------------------------------------------------- |
| `BencodeEncoder` | Encode a single CLR value into an in-memory Bencode object | Validation, inspection, further processing         |
| `BencodeWriter`  | Encode and write values directly to a stream or file       | Files, network output, multiple consecutive values |

A good rule of thumb:

> **If you want a Bencode object in memory, use `BencodeEncoder`.**
> **If you want to write Bencode data somewhere, use `BencodeWriter`.**

---

## BencodeEncoder — Encoding into Bencode Objects

`BencodeEncoder` is responsible for converting CLR objects into their **validated `IBobject` representation**.

It performs three steps:

1. Resolves an appropriate serializer for the runtime type
2. Invokes the serializer to produce a Bencode object
3. Validates the result using the configured `BencodeOptions`

### When to Use

Use `BencodeEncoder` when:

- You need a Bencode object in memory
- You want to inspect or manipulate the Bencode tree
- You plan to reuse the encoded value multiple times
- You want validation to happen immediately

Typical scenarios include:

- Preparing data for hashing or signing
- Inspecting encoded structures
- Passing Bencode objects between components

---

## Basic Encoding

To encode a CLR object into an `IBobject`:

```csharp
var encoder = new BencodeEncoder();

IBobject obj = encoder.Encode(myValue);
```

Behavior:

- Serializer resolution uses the runtime type of `myValue`
- If no serializer is found, encoding fails immediately
- The returned object is guaranteed to be valid Bencode

### Encoding with an Explicit Serializer

You may explicitly provide a serializer:

```csharp
var obj = encoder.Encode<MyType, Bdictionary>(value, mySerializer);
```

This is useful when:

- Multiple serializers exist for a type
- You want explicit control over the encoding strategy
- You are bypassing automatic discovery

---

## Validation Guarantees

`BencodeEncoder` enforces **all validation rules** defined by `BencodeOptions`, including:

- Maximum nesting depth
- Container size limits
- Payload size constraints
- Structural correctness

If validation fails, encoding throws a `BencodeSerializerException`.

If `Encode` completes successfully, the returned `IBobject`:

- Is non-null
- Is structurally valid
- Satisfies all configured limits

---

## BencodeWriter — Streaming and Incremental Output

`BencodeWriter` is designed for **forward-only, streaming output**.

It encodes values and writes their Bencode representation directly to a `Stream` or file, without requiring the entire payload to be buffered in memory.

### When to Use

Use `BencodeWriter` when:

- Writing Bencode data to files or network streams
- Producing large outputs incrementally
- Writing multiple consecutive Bencode values
- You do not need to retain the encoded object in memory

Typical scenarios include:

- Writing `.torrent`-style files
- Streaming protocol output
- Logging or exporting Bencode data

---

## Writing Existing Bencode Objects

If you already have an `IBobject`, you can write it directly:

```csharp
var writer = new BencodeWriter();

await writer.WriteBencodeAsync(bencodeObject, outputStream);
```

Characteristics:

- No serialization is performed
- The object is assumed to already represent valid Bencode
- The stream is not disposed by the writer

This is ideal when working with objects produced by `BencodeEncoder` or `BencodeReader`.

---

## Writing CLR Values

`BencodeWriter` can also encode and write CLR values directly.

```csharp
await writer.WriteAsync(myValue, outputStream);
```

Behavior:

- If `myValue` implements `IBobject`, it is written directly
- Otherwise, a serializer is resolved automatically
- The value is encoded and written in a single operation

### Using an Explicit Serializer

```csharp
await writer.WriteAsync(myValue, outputStream, mySerializer);
```

This mirrors the explicit serializer usage in `BencodeEncoder` and is useful for the same reasons.

---

## Writing Multiple Values

`BencodeWriter` supports writing **multiple consecutive Bencode values** to the same stream.

```csharp
await writer.WriteAsync(value1, stream);
await writer.WriteAsync(value2, stream);
await writer.WriteAsync(value3, stream);
```

Each call appends a complete Bencode value to the stream.

No framing or separators are added — the output is a raw concatenation of Bencode objects.

---

## Writing to Files

Convenience APIs are provided for file output.

### Writing an `IBobject` to a File

```csharp
await writer.WriteBencodeToFileAsync(obj, "data.bencode", overwrite: true);
```

### Writing a CLR Value to a File

```csharp
await writer.WriteToFileAsync(myValue, "data.bencode", overwrite: true);
```

Behavior:

- Files are created or overwritten based on the `overwrite` flag
- File streams are owned and disposed by the method
- Encoding and validation rules are still enforced

---

## Validation and Error Handling

Both `BencodeEncoder` and `BencodeWriter`:

- Enforce `BencodeOptions` constraints
- Fail fast on serialization or validation errors
- Never emit partially encoded values

Common exception types include:

- `BencodeSerializerNotFoundException` — no serializer available
- `BencodeSerializerException` — serialization or validation failure
- `BencodeIOException` — invalid or non-writable output stream

---

## Choosing the Right API

| Scenario                             | Recommended API  |
| ------------------------------------ | ---------------- |
| Encode a value into a Bencode object | `BencodeEncoder` |
| Inspect or reuse encoded data        | `BencodeEncoder` |
| Write Bencode data to a stream       | `BencodeWriter`  |
| Write multiple consecutive values    | `BencodeWriter`  |
| Write directly to a file             | `BencodeWriter`  |

---

## Thread Safety Notes

Neither `BencodeEncoder` nor `BencodeWriter` instances are thread-safe.

- Do not use a single instance concurrently
- Create separate instances per operation or per thread

---

This separation between **encoding** and **writing** allows BencodeDotNet to support both in-memory construction and efficient streaming output while preserving strict validation guarantees.
