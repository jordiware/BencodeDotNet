# Overview

BencodeDotNet is a strict, streaming **Bencode decoder** for modern .NET applications.

It is designed to safely and efficiently parse Bencode‑encoded data from streams of arbitrary size, while enforcing full compliance with the Bencode specification and explicit safety limits.

This document provides a high‑level overview of the library’s purpose, guarantees, and intended usage.

---

## What BencodeDotNet Is

BencodeDotNet is a **decoder-only** library that converts Bencode data into a strongly typed object model.

It focuses on:

- Correctness over permissiveness
- Streaming and scalability
- Predictable resource usage
- Clear failure modes

The library is suitable for production environments where input data may be large, untrusted, or received incrementally.

---

## What BencodeDotNet Is Not

BencodeDotNet intentionally does **not** attempt to be:

- A permissive or best-effort parser
- A serializer for loosely structured data
- A legacy or cross-framework compatibility library

Malformed input, specification violations, and safety limit breaches are treated as **errors**, not recoverable conditions.

---

## Supported Data Model

BencodeDotNet implements the four canonical Bencode types:

- **Integer** → `Binteger`
- **Byte string** → `Bstring`
- **List** → `Blist`
- **Dictionary** → `Bdictionary`

All decoded values implement the `IBobject` interface, allowing uniform handling while preserving strong typing.

The object model is immutable and represents the decoded data exactly as defined by the input.

---

## Streaming First

Decoding is performed directly from a `Stream`.

This allows:

- Parsing of very large inputs without buffering them entirely
- Incremental decoding from files, sockets, or other data sources
- Sequential decoding of multiple Bencode values from the same stream

The decoder only reads as much data as required to complete the next valid value.

---

## Validation and Safety Guarantees

BencodeDotNet enforces both **specification correctness** and **explicit safety limits**.

During decoding, the following guarantees apply:

- Invalid tokens or malformed structures are rejected
- Integers must be well‑formed
- Dictionary keys must be sorted, as required by the specification
- Unexpected end‑of‑stream conditions are detected

In addition, decoding is constrained by configurable limits defined via `BencodeOptions`, including:

- Maximum nesting depth
- Maximum string byte length
- Maximum number of items per container

Violations of these limits result in runtime exceptions and immediate termination of decoding.

---

## Intended Use Cases

BencodeDotNet is well suited for:

- Decoding Bencode payloads received over the network
- Processing large or unbounded Bencode streams
- Enforcing strict validation of untrusted input

---

## Target Framework

BencodeDotNet targets **.NET 10**.

This is a hard requirement. The library relies on modern runtime and base class library features that are not available in earlier versions of .NET.

---

## Next Steps

For practical usage examples and detailed guidance, refer to the other documents in the `docs/` folder:

- Decoding basics and factory methods
- Working with decoded objects
- Safety limits and tuning
- End‑to‑end examples

---

BencodeDotNet aims to provide a clear, safe, and efficient foundation for working with Bencode data in modern .NET applications.

