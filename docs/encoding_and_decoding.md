# Encoding and Decoding

This document describes how to encode and decode Bencode data using **BencodeDotNet**.

It focuses on *usage semantics*, *validation guarantees*, and *design intent*, rather than exhaustive API listings. All examples assume familiarity with the Bencode data model and the `IBobject` hierarchy.

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
var options = new BencodeOptions(textEncoding: Encoding.Unicode);
```

The specified encoding is applied consistently across all encoding and decoding operations that use the associated `BencodeOptions` instance.

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

#### From a `ReadOnlySpan<byte>`

```csharp
IBobject value = decoder.Decode(span);
```

#### From a file (asynchronous)

```csharp
IBobject value = await decoder.DecodeAsync("data.bencode");
```

#### From a stream (asynchronous)

```csharp
using var stream = File.OpenRead("data.bencode");
IBobject value = await decoder.DecodeAsync(stream);
```

All decoding operations:

- Expect **exactly one** top-level Bencode value
- Reject trailing data
- Enforce nesting depth and size limits
- Throw on malformed input

---

### Decoding into CLR Types

BencodeDotNet supports decoding directly into CLR values via registered or attributed serializers:

```csharp
var result = decoder.Decode<MyType>(bytes);
```

Serializer resolution order:

1. `BencodeSerializerAttribute` on the target type
2. Global serializer registry

If no compatible serializer is found, decoding fails with `BencodeSerializerNotFoundException`.

---

### Using Explicit Serializers

You may bypass serializer discovery by providing a serializer explicitly:

```csharp
var serializer = new MyTypeBencodeSerializer();
var result = decoder.Decode<MyType, Bdictionary>(bytes, serializer);
```

Explicit serializers must:

- Match the expected `IBobject` type produced by decoding
- Produce a non-null CLR value

Violations result in `BencodeSerializerException`.

---

### Asynchronous Decoding

Async decoding is supported for files and streams:

```csharp
IBobject value = await decoder.DecodeAsync("data.bencode");
var result = await decoder.DecodeAsync<MyType>(stream, cancellationToken);
```

Async decoding:

- Uses `System.IO.Pipelines.PipeReader`
- Preserves parsing state across partial reads
- Supports cancellation
- Does **not** buffer the entire input
- Detects multiple top-level objects or trailing data

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

---

### Encoding CLR Values

To encode a CLR object using serializer discovery:

```csharp
IBobject encoded = encoder.Encode(value);
```

The encoder:

1. Resolves a serializer for the runtime type of `value`
2. Invokes the serializer to produce an `IBobject`
3. Validates the result using `BencodeOptions`

Encoding fails if any of these steps cannot be completed successfully.

---

### Using Explicit Serializers

```csharp
var serializer = new MyTypeBencodeSerializer();
IBobject encoded = encoder.Encode(value, serializer);
```

Explicit serializers must:

- Successfully serialize the input value
- Produce a non-null `IBobject`
- Produce output that satisfies all validation constraints

Violations result in `BencodeSerializerException`.

---

## Validation and Safety

BencodeDotNet enforces correctness, predictability, and defensive behavior:

### Structural Validation

- Parser enforces the Bencode grammar for integers, byte strings, lists, and dictionaries
- Any deviation triggers `BencodeFormatException`
- Incremental validation ensures proper container opening/closing

### Depth and Size Limits

- Configurable maximum depth prevents resource exhaustion
- String lengths and container sizes are validated on read

### Dictionary Key Rules

- Keys must be valid byte strings and sorted
- Duplicates or out-of-order keys are rejected

### Serializer Safety

- `TrySerialize` / `TryDeserialize` pattern ensures explicit failure handling
- Custom or registry-resolved serializers are validated before use

### Cancellation and Predictable Failure

- Async operations honor `CancellationToken`
- In all failure cases, partial objects are not exposed

---

## Error Handling

Exceptions are used to indicate invalid input or misuse:

- `BencodeFormatException` — malformed or non-conformant Bencode data
- `BencodeSerializerNotFoundException` — missing serializer
- `BencodeSerializerException` — serializer failure
- `BencodeIOException` — unreadable stream
- `OperationCanceledException` — cancellation

No partial or undefined states are exposed.

---

## Design Notes

### No Factory Methods

- Explicit instantiation makes option ownership clear
- Avoids hidden global state
- Enables reuse with consistent validation semantics

### Data Passed to Methods

- All data must be supplied to methods directly
- No implicit buffering or hidden state
- Usage is explicit and predictable

### Strictness by Design

- Strict validation is intentional
- No recovery from malformed input or spec violations

---

This version fully reflects:

- Sync and async decoding/encoding
- File, stream, span, and string support
- Serializer discovery and explicit serializer usage
- Cancellation support
- Full validation and error handling
