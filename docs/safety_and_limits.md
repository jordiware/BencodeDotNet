# Safety and Limits

BencodeDotNet is designed to safely decode Bencode data from potentially untrusted or unbounded sources.

This document explains the safety mechanisms enforced during decoding, the role of `BencodeOptions`, and how to tune limits for different environments.

---

## Safety by Design

BencodeDotNet enforces safety at decode time rather than relying on caller discipline.

The decoder:

- Validates all input against the Bencode specification
- Applies explicit upper bounds to memory- and CPU-sensitive operations
- Fails fast when limits are exceeded or malformed input is detected

There is no permissive or best-effort mode. Invalid input is always treated as an error.

---

## BencodeOptions Overview

All configurable safety constraints are defined in the `BencodeOptions` struct.

```csharp
var options = new BencodeOptions(
    maxDepth: 1024,
    maxStringLength: 64 * 1024 * 1024,
    maxContainerItems: 1024
);
```

All limits are enforced during decoding. Violations result in runtime exceptions and immediate termination of the decoding operation.

---

## Maximum Nesting Depth (`MaxDepth`)

`MaxDepth` limits the combined nesting depth of lists and dictionaries.

### Why it exists

Deeply nested structures can:

- Cause excessive stack or heap usage
- Be used as a denial-of-service vector
- Indicate malformed or hostile input

### Behavior

- The depth counter is incremented when entering a list or dictionary
- Both list and dictionary nesting contribute to the same depth
- Exceeding the limit results in an `InvalidOperationException`

### Tuning guidance

- **Default (`1024`)**: Safe for most real-world data
- Lower values for tightly controlled inputs
- Higher values only if you fully trust the data source

---

## Maximum String Length (`MaxStringLength`)

`MaxStringLength` limits the declared byte length of a Bencode string.

### Important notes

- The limit applies to the **declared byte count**, not decoded characters
- No assumptions are made about encoding
- Strings exceeding the limit are rejected before allocation

### Why it exists

- Prevents excessive memory allocation
- Protects against integer overflow and malformed length prefixes

### Tuning guidance

- **Default (64 MB)**: Suitable for metadata and typical payloads
- Lower values for memory-constrained environments
- Increase cautiously for trusted, large binary payloads

---

## Maximum Container Items (`MaxContainerItems`)

`MaxContainerItems` limits the number of elements in a list or dictionary.

### Behavior

- Applies to list elements and dictionary key/value pairs
- Checked incrementally as items are decoded
- Exceeding the limit results in an `InvalidOperationException`

### Why it exists

- Prevents unbounded container growth
- Limits memory usage and processing time

### Tuning guidance

- **Default (`1024`)**: Suitable for most use cases
- Increase only when the expected structure size is well understood

---

## Dictionary Key Ordering

Bencode requires dictionary keys to be sorted lexicographically.

BencodeDotNet enforces this rule strictly:

- Keys must appear in sorted order
- Duplicate keys are rejected
- Violations result in a decoding error

This guarantees deterministic dictionary semantics and spec compliance.

---

## Failure Model

When a safety limit or validation rule is violated:

- Decoding stops immediately
- An exception is thrown
- No partial or undefined state is produced

The decoder does not attempt recovery or continuation after a failure.

---

## Recommended Profiles

### Untrusted Input (Default)

```csharp
new BencodeOptions(
    maxDepth: 1024,
    maxStringLength: 64 * 1024 * 1024,
    maxContainerItems: 1024
);
```

### Highly Constrained Environment

```csharp
new BencodeOptions(
    maxDepth: 128,
    maxStringLength: 1 * 1024 * 1024,
    maxContainerItems: 256
);
```

### Trusted, Large Payloads

```csharp
new BencodeOptions(
    maxDepth: 4096,
    maxStringLength: 256 * 1024 * 1024,
    maxContainerItems: 8192
);
```

Only increase limits when the input source is trusted and resource usage is acceptable.

---

## Summary

BencodeDotNet’s safety model is explicit, strict, and predictable.

By combining full specification validation with configurable limits, the library allows safe decoding of Bencode data across a wide range of environments without sacrificing correctness or performance.

