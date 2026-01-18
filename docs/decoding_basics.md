# Decoding Basics

This document introduces the basic usage patterns for decoding Bencode data with **BencodeDotNet**.

It focuses on how to create a decoder, how decoding behaves, and how to integrate it safely into typical applications.

---

## Creating a Decoder

BencodeDotNet provides two primary ways to create a decoder:

- **Factory helpers** for common input sources (recommended)
- **Direct construction** for advanced scenarios

### Factory Helpers (Recommended)

Factory helpers create a ready-to-use decoder for common data sources and apply default options unless specified otherwise.

#### From a byte array

```csharp
byte[] data = File.ReadAllBytes("data.bencode");

var decoder = Bdecoder.FromBytes(data);
IBobject value = await decoder.DecodeAsync();
```

#### From a string

```csharp
string text = "d3:foo3:bare";

var decoder = Bdecoder.FromString(text, Encoding.UTF8);
IBobject value = await decoder.DecodeAsync();
```

#### From a file

```csharp
var decoder = Bdecoder.FromFile("data.bencode");
IBobject value = await decoder.DecodeAsync();
```

Each factory method optionally accepts a `BencodeOptions` instance to customize safety limits.

---

## Direct Decoder Construction

For scenarios requiring explicit control over decoder lifetime or configuration—such as decoding data received over the network—you may construct a decoder directly from a stream.

```csharp
using var client = new TcpClient("example.org", 6881);
using NetworkStream stream = client.GetStream();

var options = new BencodeOptions(
    maxDepth: 1024,
    maxStringLength: 64 * 1024 * 1024,
    maxContainerItems: 1024
);

var decoder = new Bdecoder<NetworkStream>(ref stream, options);
IBobject value = await decoder.DecodeAsync();
```

This approach is functionally equivalent to using the factory helpers, but is well suited for long-lived or externally managed streams such as network connections.

---

## Decoding Behavior

### Incremental Reading

Decoding is performed incrementally from the underlying stream.

The decoder reads only as much data as required to complete the next valid Bencode value. It does not buffer the entire input unless the input source itself is fully buffered (for example, a `MemoryStream`).

### Single-Value Semantics

Each call to `DecodeAsync()` decodes **exactly one** Bencode value.

If additional data remains in the stream, subsequent calls to `DecodeAsync()` will continue decoding from the current stream position.

```csharp
IBobject first = await decoder.DecodeAsync();
IBobject second = await decoder.DecodeAsync();
```

---

## Cancellation

Decoding supports cooperative cancellation.

```csharp
using var cts = new CancellationTokenSource();

IBobject value = await decoder.DecodeAsync(cts.Token);
```

If cancellation is requested, decoding stops promptly and an exception is raised.

---

## Error Handling

BencodeDotNet is a strict decoder. Errors are not recoverable.

Common error conditions include:

- Invalid Bencode syntax
- Unexpected end-of-stream
- Violation of `BencodeOptions` limits
- Dictionary keys not sorted

All such conditions result in runtime exceptions and immediate termination of decoding.

---

## Best Practices

- Prefer **factory helpers** unless you need direct control
- Configure `BencodeOptions` explicitly when handling untrusted input
- Decode values sequentially rather than buffering entire streams
- Treat decoding failures as fatal input errors

---

This document covers the fundamentals of decoding. For guidance on traversing and interpreting decoded objects, refer to **working-with-objects.md**.

