# BencodeDotNet

BencodeDotNet is a modern, high‑performance **Bencode** decoding library for .NET.

It provides a **correct, strict, and memory‑efficient** implementation of the Bencode specification, designed for production‑grade parsing of large or streaming inputs such as BitTorrent metadata and network payloads.

> **Project status**: BencodeDotNet is currently in **v0.2** development. The core architecture and decoding model are stable, but the public API is still considered provisional and may evolve before a final release.

---

## Features

* ✅ **Fully compliant Bencode decoder**
* ⚡ **Asynchronous, streaming parsing** from `Stream`
* 📈 **Linear‑time parsing** with predictable memory usage
* 🧱 **Strongly typed Bencode object model**
* 🔒 **Strict validation** of malformed or non‑conformant input
* 🛡️ **Configurable safety limits** (nesting depth, sizes)
* ❌ **No full buffering** — suitable for arbitrarily large inputs
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

This allows uniform handling of decoded values while preserving strong typing and explicit type semantics.

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

BencodeDotNet exposes explicit **encoder** and **decoder** types, both configured via constructors and optional `BencodeOptions`. If no options are provided, sensible defaults are used.

To keep this README concise, detailed examples for encoding and decoding are documented separately.

👉 See **`docs/encoding_and_decoding.md`** for in‑depth usage examples, advanced scenarios, and API details.

---

## Intended Usage

BencodeDotNet is designed for:

* Parsing Bencode data from files, sockets, or other streams
* Handling very large or unbounded inputs safely
* Enforcing strict compliance with the Bencode specification

It is **not** a permissive or best‑effort parser: invalid input is rejected deterministically and early.

---

## Performance Characteristics

BencodeDotNet is designed with performance and scalability in mind:

* Streaming parsing without loading full inputs into memory
* Linear‑time decoding
* Minimal allocations
* Safe handling of deeply nested structures within configured limits

---

## Project Status and Roadmap

BencodeDotNet is currently released as **v0.2**.

At this stage:

* The core decoding architecture is considered stable
* Strict validation semantics are intentional and non‑negotiable
* Public APIs may still change before v1.0

---

## Contributing

Contributions and reviews are welcome.

Please ensure that any changes:

* Preserve strict validation semantics
* Maintain predictable memory behavior
* Do not weaken safety guarantees
* Are covered by appropriate tests

---

## License

MIT License

---

**BencodeDotNet** — a strict, efficient Bencode decoder for modern .NET.
