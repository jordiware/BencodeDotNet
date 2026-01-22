# Encoding and Decoding

This document describes how to encode and decode Bencode data using **BencodeDotNet**.

It focuses on *usage semantics*, *validation guarantees*, and *design intent*, rather than exhaustive API listings. All examples assume familiarity with the Bencode data model and the `IBobject` hierarchy.

---

## Overview

BencodeDotNet exposes two primary entry points:

- `Bdecoder` — decodes Bencode-encoded data into `IBobject` instances or CLR values
- `Bencoder` — encodes CLR values into validated `IBobject` representations

Both components:

- Are instantiated explicitly via constructors
- Accept optional `BencodeOptions`
- Apply **strict validation** by default
- Do **not** perform permissive or best-effort parsing

There are no static factory helpers. All data to be processed is supplied directly to encoding or decoding methods.

---

## Decoder Usage

### Creating a Decoder

```csharp
var decoder = new Bdecoder();
```

Custom decoding limits may be supplied via `BencodeOptions`:

```csharp
var options = new BencodeOptions(maxDepth: 512);
var decoder = new Bdecoder(options);
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
IBobject value = decoder.Decode(text, Encoding.UTF8);
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

BencodeDotNet supports decoding directly into CLR values via registered or attributed serializers.

```csharp
var result = decoder.Decode<MyType>(bytes);
```

Serializer resolution follows this order:

1. A serializer declared via `BencodeSerializerAttribute` on the target type
2. A serializer registered in the global serializer registry

If no compatible serializer is found, decoding fails with `NotSupportedException`.

---

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

### Asynchronous Decoding

Asynchronous decoding is supported for files and streams:

```csharp
IBobject value = await decoder.DecodeAsync("data.bencode");
```

```csharp
var result = await decoder.DecodeAsync<MyType>(stream, cancellationToken);
```

Async decoding:

- Uses `System.IO.Pipelines.PipeReader`
- Preserves parsing state across partial reads
- Supports cancellation
- Does **not** buffer the entire input

---

## Encoder Usage

### Creating an Encoder

```csharp
var encoder = new Bencoder();
```

Custom encoding limits may be supplied via `BencodeOptions`:

```csharp
var encoder = new Bencoder(new BencodeOptions(maxDepth: 256));
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

You may encode using a specific serializer:

```csharp
var serializer = new MyTypeBencodeSerializer();
IBobject encoded = encoder.Encode(value, serializer);
```

Explicit serializers must:

- Successfully serialize the input value
- Produce a non-null `IBobject`
- Produce output that satisfies all validation constraints

---

## Validation and Safety

BencodeDotNet is designed with a strong emphasis on correctness, predictability, and defensive behavior when handling untrusted input. Both encoding and decoding paths apply strict validation rules to ensure malformed or hostile data is rejected early and safely.

### Structural Validation

During decoding, the parser enforces the Bencode grammar rigorously. Integers, byte strings, lists, and dictionaries must all follow the exact specification; any deviation (such as invalid delimiters, malformed lengths, or unexpected end-of-input) results in a decoding failure rather than partial or best-effort recovery.

Nested structures are validated incrementally as they are read, ensuring that lists and dictionaries are properly opened and closed and that their internal elements are well-formed.

### Depth and Size Limits

To protect against resource-exhaustion attacks (for example, deeply nested lists or dictionaries), decoding enforces a configurable maximum depth. Once this limit is exceeded, decoding stops immediately and fails in a controlled manner.

Similarly, string lengths and container sizes are validated as they are parsed. This prevents attempts to allocate excessive memory based on maliciously large length prefixes.

### Dictionary Key Rules

Dictionary keys are required to be valid byte strings and are processed in strict order, as mandated by the Bencode specification. Invalid key types, duplicated keys, or out-of-order keys are rejected to avoid ambiguous or non-canonical representations.

### Serializer Safety

On the encoding side, serializers follow a non-throwing `TrySerialize` / `TryDeserialize` pattern. Invalid values, unsupported types, or incompatible serializers result in a clean `false` return value rather than exceptions, making failure modes explicit and easy to handle.

Custom serializers resolved via attributes or the central registry are validated for compatibility before use, ensuring that only correctly declared serializers participate in the encoding or decoding process.

### Cancellation and Predictable Failure

Async decoding operations honor `CancellationToken` parameters, allowing long-running or stalled operations to be cancelled deterministically. In all failure cases—whether due to invalid data, validation limits, or cancellation—the library guarantees a well-defined and predictable outcome without leaving partially constructed objects behind.

Together, these measures make BencodeDotNet suitable for processing both trusted and untrusted Bencode data while maintaining safety, clarity, and performance.
## Error Handling

BencodeDotNet uses exceptions to report invalid input or misuse:

- `FormatException` — malformed or non-conformant Bencode data
- `NotSupportedException` — missing serializers
- `InvalidOperationException` — serializer failures or invalid results
- `OperationCanceledException` — cancellation during async decoding

No partial or undefined states are exposed to the caller.

---

## Design Notes

### No Factory Methods

`Bdecoder` and `Bencoder` are instantiated explicitly to:

- Make option ownership explicit
- Avoid hidden global state
- Enable reuse with consistent validation semantics

### Data Passed to Methods

All data is provided directly to encoding and decoding methods to:

- Keep object lifetimes simple
- Avoid implicit buffering
- Make usage explicit and predictable

### Strictness by Design

BencodeDotNet is intentionally strict.

It does not attempt to recover from malformed input, tolerate specification violations, or guess intent. This behavior is fundamental to the library and will not change in future versions.

