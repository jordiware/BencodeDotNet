# Streamed Read/Write

BencodeDotNet supports forward-only, streaming read and write operations for Bencode data.  
These APIs are intended for scenarios where multiple consecutive Bencoded objects are read from or written to a stream, avoiding full in-memory materialization.

---

## Overview

- **Reader:** `BencodeReader`  
  Incrementally reads Bencoded objects from a `Stream` and yields fully parsed top-level objects as they are completed. Supports deserialization using registered serializers.

- **Writer:** `BencodeWriter`  
  Encodes Bencode objects or arbitrary values using serializers and writes them incrementally to a `Stream` or file.

**Key benefits of streaming APIs:**

- Low memory footprint, suitable for large or continuous data.
- Immediate processing of each top-level object as soon as it is parsed.
- Supports asynchronous enumeration and cancellation.

**Important notes:**

- Neither `BencodeReader` nor `BencodeWriter` owns the underlying `Stream`.
- Instances are **not thread-safe**; do not share across multiple consumers concurrently.
- Validation and serialization respect the configured `BencodeOptions`.

---

## BencodeReader

`BencodeReader` provides incremental reading of one or more Bencoded objects from a stream.

### Usage

#### Reading raw Bencode objects

```csharp
var reader = new BencodeReader(options);

await foreach (var bobj in reader.ReadAsync(stream, cancellationToken))
{
    // Process IBobject instance
}
```

#### Reading and deserializing into typed values

```csharp
await foreach (var item in reader.ReadAsync<MyType>(stream, serializer, cancellationToken))
{
    // Process deserialized value
}
```

#### Reading from a file

```csharp
await foreach (var bobj in reader.ReadFromFileAsync("data.bencode"))
{
    // Process top-level object
}

await foreach (var value in reader.ReadFromFileAsync<MyType>("data.bencode", serializer))
{
    // Process deserialized value
}
```

### Behavior and Guarantees

- Incremental parsing using a `PipeReader` for efficiency.
- Each top-level object is validated against `BencodeOptions` before being yielded.
- If a partial object is encountered at the end of the stream, a `BencodeFormatException` is thrown.
- Supports cancellation via `CancellationToken`.

---

## BencodeWriter

`BencodeWriter` provides incremental writing of Bencode data to a stream or file.

### Writing raw Bencode objects

```csharp
var writer = new BencodeWriter(options);

await writer.WriteBencodeAsync(bobj, stream, cancellationToken);
await writer.WriteBencodeToFileAsync(bobj, "output.bencode", overwrite: true, cancellationToken);
```

### Writing typed values using serializers

```csharp
await writer.WriteAsync(myValue, stream, serializer, cancellationToken);
await writer.WriteToFileAsync(myValue, "output.bencode", overwrite: true, serializer, cancellationToken);
```

### Behavior and Guarantees

- Incrementally writes to a `PipeWriter` for efficiency.
- Supports optional serializers; falls back to the registry if none is provided.
- File-based methods automatically manage the file stream lifetime.
- Validates output via `BencodeOptions` if applicable.
- Supports cancellation via `CancellationToken`.

---

## Comparison: Materialized vs Streaming

| Feature              | Materialized (`Bdecoder`)    | Streaming (`BencodeReader` / `BencodeWriter`)   |
|----------------------|------------------------------|-------------------------------------------------|
| Memory usage         | High for large objects       | Low, object-by-object                           |
| Immediate processing | No                           | Yes, objects yielded as soon as parsed          |
| Cancellation support | Limited                      | Full support via `CancellationToken`            |
| Multiple objects     | Only if wrapped              | Supports consecutive objects in the same stream |
| File I/O             | Requires separate read/write | Direct support with async file helpers          |

---

## Best Practices

- Use streaming APIs for large datasets or network streams to reduce memory pressure.
- Always provide a `CancellationToken` when processing unknown or slow streams.
- When reading typed values, ensure that the appropriate serializer is available in the registry or provided explicitly.
- Avoid sharing a reader or writer instance across multiple threads.

---

## Exceptions

### BencodeReader

- `ArgumentNullException` – stream is `null`.
- `BencodeIOException` – stream is not readable.
- `BencodeFormatException` – malformed or truncated Bencode data.
- `BencodeValidationException` – object fails validation.
- `BencodeSerializerException` – deserialization fails.
- `BencodeSerializerNotFoundException` – no compatible serializer found.

### BencodeWriter

- `ArgumentNullException` – value or stream is `null`.
- `BencodeIOException` – stream is not writable.
- `BencodeSerializerNotFoundException` – no serializer found for type.
- `IOException` – file exists and `overwrite` is false.

---

This document provides a complete reference for using the streamed read/write features in BencodeDotNet, 
ensuring safe and efficient processing of Bencode data in asynchronous and low-memory scenarios.
