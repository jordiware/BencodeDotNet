# Encoding and Decoding

This document describes how to encode and decode Bencode data using **BencodeDotNet**.

It focuses on _usage semantics_, _validation guarantees_, and _design intent_, rather than exhaustive API listings. All examples assume familiarity with the Bencode data model and the `IBobject` hierarchy.

---

## Overview

BencodeDotNet exposes two primary entry points:

- `BencodeDecoder` — decodes Bencode-encoded data into `IBobject` instances or CLR values
- `BencodeEncoder` — encodes CLR values into validated `IBobject` representations

Both components:

- Are instantiated explicitly via constructors
- Accept optional `BencodeOptions`
- Apply **strict validation** by default
- Do **not** perform permissive or best-effort parsing

There are no static factory helpers. All data to be processed is supplied directly to encoding or decoding methods.

---

## Text Encoding

BencodeDotNet does not assume a fixed text encoding for Bencode string values.

All conversion between CLR `string` values and their underlying byte
representations is governed by the `TextEncoding` property of
`BencodeOptions`.

By default, `BencodeOptions` uses UTF-8 encoding:

- `BencodeOptions.DefaultTextEncoding` is set to `Encoding.UTF8`
- This default is applied whenever no explicit encoding is provided

The selected text encoding affects:

- How CLR `string` values are encoded into Bencode byte strings
- How byte strings are decoded back into CLR `string` values
- How string byte lengths are calculated for validation and size limits

Custom encodings may be supplied by constructing `BencodeOptions` explicitly:

```csharp
var options = new BencodeOptions(
    textEncoding: Encoding.Unicode);
```

The specified encoding is applied consistently across all encoding and decoding operations using the associated `BencodeOptions` instance.

No validation of encoded text is performed at construction time; invalid or non-decodable byte sequences are handled according to the behavior of the configured Encoding during decoding.

---

## Decoder Usage

### Creating a Decoder

```csharp
var decoder = new BencodeDecoder();
```

Custom decoding limits may be supplied via `BencodeOptions`:

```csharp
var options = new BencodeOptions(maxDepth: 512);
var decoder = new BencodeDecoder(options);
```

If no options are provided, default limits are used.

---

### Decoding into `IBobject`

The simplest decoding APIs return a strongly-typed `IBobject` tree.

#### From a byte array

```csharp
IBobject value = decoder.Decode(bytes);
```

#### From a string

```csharp
IBobject value = decoder.Decode(text);
```

#### From a stream or file (asynchronous)

```csharp
using var stream = File.OpenRead("data.bencode");
IBobject value = await decoder.DecodeAsync(stream);

IBobject fileValue = await decoder.DecodeAsync("data.bencode");
```

All decoding operations:

- Expect **exactly one** top-level Bencode value
- Reject trailing data
- Enforce nesting depth and size limits
- Throw on malformed input

---

### Decoding into CLR Types

BencodeDotNet supports decoding directly into CLR values via registered or attributed serializers.

```csharp
var result = decoder.Decode<MyType>(bytes);
```

Serializer resolution follows this order:

1. A serializer declared via `BencodeSerializerAttribute` on the target type
2. A serializer registered in the global serializer registry

If no compatible serializer is found, decoding fails with `NotSupportedException`.

### Using Explicit Serializers

You may bypass serializer discovery by providing a serializer explicitly:

```csharp
var serializer = new MyTypeBencodeSerializer();
var result = decoder.Decode<MyType, Bdictionary>(bytes, serializer);
```

Explicit serializers are required to:

- Match the expected `IBobject` type produced by decoding
- Produce a non-null CLR value

Violations result in `InvalidOperationException`.

---

### Asynchronous Streaming

BencodeDotNet supports incremental reading of multiple consecutive Bencoded objects using `BencodeReader`:

```csharp
var reader = new BencodeReader(options);

await foreach (var bobj in reader.ReadAsync(stream))
{
    // Process IBobject instance
}

await foreach (var value in reader.ReadAsync<MyType>(stream, serializer))
{
    // Process deserialized CLR value
}
```

- Objects are yielded as soon as they are fully parsed and validated.
- Validation limits (`BencodeOptions`) apply to each object.
- Cancellation is supported via `CancellationToken`.
- Partial or malformed objects at the end of the stream trigger `BencodeFormatException`.

File-based helpers are also available:

```csharp
await foreach (var bobj in reader.ReadFromFileAsync("data.bencode"))
{
    // Process each top-level object
}

await foreach (var value in reader.ReadFromFileAsync<MyType>("data.bencode", serializer))
{
    // Process deserialized CLR value
}
```

---

## Encoder Usage

### Creating an Encoder

```csharp
var encoder = new BencodeEncoder();
```

Custom encoding limits may be supplied via `BencodeOptions`:

```csharp
var encoder = new BencodeEncoder(new BencodeOptions(maxDepth: 256));
```

### Encoding CLR Values

```csharp
IBobject encoded = encoder.Encode(value);
```

The encoder:

1. Resolves a serializer for the runtime type of `value`
2. Invokes the serializer to produce an `IBobject`
3. Validates the result using `BencodeOptions`

Encoding fails if any of these steps cannot be completed successfully.

### Using Explicit Serializers

```csharp
var serializer = new MyTypeBencodeSerializer();
IBobject encoded = encoder.Encode(value, serializer);
```

Explicit serializers must:

- Successfully serialize the input value
- Produce a non-null `IBobject`
- Produce output that satisfies all validation constraints

### Streaming Writes with BencodeWriter

`BencodeWriter` enables writing multiple consecutive Bencode objects or typed values incrementally:

```csharp
var writer = new BencodeWriter(options);

await writer.WriteAsync(value, stream, serializer);
await writer.WriteToFileAsync(value, "output.bencode", overwrite: true, serializer);
```

- Objects are written incrementally to reduce memory footprint.
- Optional serializers can be provided; otherwise, the registry is used.
- File-based helpers manage the file stream automatically.
- Cancellation is supported.

---

## Validation and Safety

BencodeDotNet is designed with a strong emphasis on correctness, predictability, and defensive behavior when handling untrusted input. Both encoding and decoding paths apply strict validation rules to ensure malformed or hostile data is rejected early and safely.

### Structural Validation

During decoding, the parser enforces the Bencode grammar rigorously. Integers, byte strings, lists, and dictionaries must all follow the exact specification; any deviation (such as invalid delimiters, malformed lengths, or unexpected end-of-input) results in a decoding failure rather than partial or best-effort recovery.

Nested structures are validated incrementally as they are read, ensuring that lists and dictionaries are properly opened and closed and that their internal elements are well-formed.

### Depth and Size Limits

Decoding enforces a configurable maximum depth. Once this limit is exceeded, decoding stops immediately and fails in a controlled manner.

Similarly, string lengths and container sizes are validated as they are parsed to prevent excessive memory allocation.

### Dictionary Key Rules

Dictionary keys are required to be valid byte strings and are processed in strict order. Invalid key types, duplicated keys, or out-of-order keys are rejected.

### Serializer Safety

Serializers follow a non-throwing `TrySerialize` / `TryDeserialize` pattern. Custom serializers are validated for compatibility before use.

### Cancellation and Predictable Failure

Async operations honor `CancellationToken`. All failure cases produce a well-defined outcome without leaving partially constructed objects.

---

## Error Handling

- `FormatException` — malformed or non-conformant Bencode data
- `NotSupportedException` — missing serializers
- `InvalidOperationException` — serializer failures or invalid results
- `OperationCanceledException` — cancellation during async decoding

No partial or undefined states are exposed.

---

## Design Notes

### No Factory Methods

`BencodeDecoder` and `BencodeEncoder` are instantiated explicitly to:

- Make option ownership explicit
- Avoid hidden global state
- Enable reuse with consistent validation semantics

### Data Passed to Methods

All data is provided directly to encoding and decoding methods to:

- Keep object lifetimes simple
- Avoid implicit buffering
- Make usage explicit and predictable

### Strictness by Design

BencodeDotNet is intentionally strict and does not recover from malformed input or tolerate specification violations.
