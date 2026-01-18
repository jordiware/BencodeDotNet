# BencodeDotNet

BencodeDotNet is a modern, high-performance **Bencode** decoding library for .NET.

It provides a correct, strict, and memory-efficient implementation of the Bencode specification, designed for production use with large or streaming inputs such as BitTorrent metadata and network payloads.

---

## Features

* ✅ **Fully compliant Bencode decoder**
* ⚡ **Asynchronous, streaming parsing** from `Stream`
* 📈 **Linear‑time parsing** with predictable memory usage
* 🧱 **Strongly typed Bencode object model**
* 🔒 **Strict validation** of malformed or non‑conformant input
* 🛡️ **Configurable maximum nesting depth** for safety
* ❌ **No full buffering** – suitable for arbitrarily large inputs
* 🧪 **Extensively tested**, including deep‑nesting stress cases

---

## Supported Bencode Types

BencodeDotNet implements the four canonical Bencode data types:

| Bencode Type | .NET Type     |
| ------------ | ------------- |
| Integer      | `Binteger`    |
| Byte String  | `Bstring`     |
| List         | `Blist`       |
| Dictionary   | `Bdictionary` |

All values implement the common interface:

```csharp
public interface IBobject { }
```

This allows uniform handling of decoded values while preserving strong typing.

---

## Target Framework and Requirements

BencodeDotNet targets **.NET 10**.

This is a **hard requirement**. The library relies on runtime and base class library features that are not available in earlier versions of .NET.

Before using BencodeDotNet, ensure your project targets:

```xml
<TargetFramework>net10.0</TargetFramework>
```

---

## Installation

BencodeDotNet is not yet published as a NuGet package.

To use it, clone the repository and reference the project directly from your solution:

```bash
git clone https://github.com/jordiware/BencodeDotNet.git
```

Then add a project reference:

```bash
dotnet add reference path/to/BencodeDotNet.csproj
```

Alternatively, you may include the project in your solution file and reference it from there.

---

## Basic Usage

BencodeDotNet provides multiple ways to create a decoder, depending on how you want to integrate it into your application.

### Using Decoder Factory Helpers

BencodeDotNet exposes several factory helpers for common input sources. These are the recommended entry points for most scenarios.

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

Each factory method accepts optional decoding options and returns a ready-to-use decoder instance.

### Using the `Bdecoder<TStream>` Constructor

If you need full control over decoder configuration or lifetime, you can instantiate the decoder directly:

```csharp
using var stream = File.OpenRead("data.bencode");

var options = new BdecodingOptions
{
    MaxDepth = 1024
};

var decoder = new Bdecoder<Stream>(ref stream, options);
IBobject value = await decoder.DecodeAsync();
```

In both cases, the decoder reads from the stream incrementally and blocks only until enough data is available to complete a valid Bencode value.

Multiple values may be decoded sequentially from the same stream.

---

## Intended Usage

BencodeDotNet is designed for:

* Parsing Bencode data from files, sockets, or other streams
* Handling very large or unbounded inputs safely
* Enforcing strict compliance with the Bencode specification

It is **not** a permissive or best‑effort parser: invalid input is rejected deterministically and early.

---

## Validation and Safety

BencodeDotNet enforces strict validation and applies explicit safety limits during decoding. Invalid or non-conformant input is rejected deterministically.

### BencodeOptions

Decoding limits are configured via `BencodeOptions`, which specifies upper bounds to protect against malformed or malicious input. All limits are enforced during decoding; violations result in runtime exceptions.

**Available limits**

| Option              | Description                                              | Default |
| ------------------- | -------------------------------------------------------- | ------- |
| `MaxDepth`          | Maximum combined nesting depth of lists and dictionaries | `1024`  |
| `MaxStringLength`   | Maximum declared byte length of a Bencode string         | `64 MB` |
| `MaxContainerItems` | Maximum number of items in a list or dictionary          | `1024`  |

These limits apply to the *declared* structure and sizes in the input (for example, string byte length, not decoded character count).

### Using BencodeOptions

```csharp
var options = new BencodeOptions(
    maxDepth: 1024,
    maxStringLength: 64 * 1024 * 1024,
    maxContainerItems: 1024
);

var decoder = Bdecoder.FromFile("data.bencode", options);
IBobject value = await decoder.DecodeAsync();
```

### Enforced Rules

In addition to the configured limits, the decoder enforces the Bencode specification:

* Invalid tokens or malformed structure are rejected
* Integers must be well-formed
* Dictionaries must contain **sorted keys**, as required by the specification
* Unexpected end-of-stream conditions are detected and reported

---

## Performance Characteristics

BencodeDotNet is designed with performance and scalability in mind:

* Streaming parsing without loading full inputs into memory
* Linear‑time decoding
* Minimal allocations
* Safe handling of deeply nested structures within configured limits

---

## Contributing

Contributions and reviews are welcome.

Please ensure that any changes:

* Preserve strict validation semantics
* Maintain predictable memory behavior
* Are covered by appropriate tests

---

## License

MIT License

---

**BencodeDotNet** — a strict, efficient Bencode decoder for modern .NET.
