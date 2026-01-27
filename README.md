# BencodeDotNet

BencodeDotNet is a modern, high‑performance **Bencode** library for .NET.

It provides a **correct, strict, and memory‑efficient** implementation of the Bencode specification, designed for production‑grade parsing and encoding of large or streaming inputs such as BitTorrent metadata and network payloads.

> **Project status**: BencodeDotNet is now in **v0.3**, fully locked and pre‑v1.0. The core architecture, reading, and writing APIs are stable, validated, and production-ready.

---

## Features

* ✅ **Fully compliant Bencode decoder**
* ✍️ **Synchronous and asynchronous encoding/writing**
* 📈 **Linear‑time parsing** with predictable memory usage
* 🧱 **Strongly typed Bencode object model**
* 🔒 **Strict validation** of malformed or non‑conformant input
* 🛡️ **Configurable safety limits** (nesting depth, sizes) fully enforced
* ❌ **No full buffering** — suitable for arbitrarily large inputs
* 🧪 **Extensively tested**, including deep‑nesting stress cases
* 🏗️ **Built-in primitive and collection serializers** with extensible model

---

## Supported Bencode Types

BencodeDotNet implements the four canonical Bencode data types:

| Bencode Type | .NET Type     |
| ------------ | ------------- |
| Integer      | `Binteger`    |
| Byte String  | `Bstring`     |
| List         | `Blist`       |
| Dictionary   | `Bdictionary` |

This allows uniform handling of decoded values while preserving strong typing and explicit type semantics.

---

## Target Framework and Requirements

BencodeDotNet targets **.NET 10**.

This is a **hard requirement**. The library relies on runtime and base class library features that are not available in earlier versions of .NET.

```xml
<TargetFramework>net10.0</TargetFramework>
```

---

## Installation

BencodeDotNet is not yet published as a NuGet package.

Clone the repository and reference the project directly from your solution:

```bash
git clone https://github.com/jordiware/BencodeDotNet.git
dotnet add reference path/to/BencodeDotNet.csproj
```

---

## Basic Usage

BencodeDotNet exposes explicit **encoder/writer** and **decoder/reader** types, both configurable via constructors and optional `BencodeOptions`. If no options are provided, sensible defaults are used.

Detailed examples for encoding, writing, decoding, and reading are documented separately.

> 👉 See **`docs/encoding_and_writing.md`** and **`docs/reading_and_decoding.md`** for in‑depth usage, advanced scenarios, and API details.

---

## Intended Usage

BencodeDotNet is designed for:

* Parsing Bencode data from files, sockets, or other streams
* Encoding or writing Bencode data efficiently
* Handling very large or unbounded inputs safely
* Enforcing strict compliance with the Bencode specification

It is **not** a permissive or best‑effort parser: invalid input is rejected deterministically and early.

---

## Performance Characteristics

BencodeDotNet is designed with performance and scalability in mind:

* Streaming parsing and writing without full buffering
* Linear‑time decoding
* Minimal allocations
* Safe handling of deeply nested structures within configured limits

---

## Contributing

Contributions and reviews are welcome. Any changes should:

* Preserve strict validation semantics
* Maintain predictable memory behavior
* Do not weaken safety guarantees
* Be covered by appropriate tests

---

## License

MIT License

---

**BencodeDotNet** — a strict, efficient Bencode library for modern .NET.
