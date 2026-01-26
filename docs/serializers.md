# Serializers

This document describes the serializer model used by **BencodeDotNet**, including how built-in serializers are resolved, how primitive and framework types are handled, and how consumers can extend the system by implementing custom serializers.

The serializer system is intentionally explicit, non-reflective, and non-throwing. Its goal is to provide predictable, safe, and easily auditable serialization behavior for Bencode data.

---

## Overview

A _serializer_ in BencodeDotNet is responsible for converting between:

- A CLR type (the **origin** type)
- A concrete Bencode object type implementing `IBobject` (the **target** type)

Serialization and deserialization are symmetrical whenever possible and always follow a **non-throwing** contract: failures are reported via a `bool` return value, never via exceptions.

At runtime, serializers are resolved dynamically based on a CLR `Type` using a centralized resolver.

---

## Core Contracts

### `IBencodeSerializer`

`IBencodeSerializer` defines the non-generic boundary used by the serializer registry and dynamic dispatch mechanisms.

Key characteristics:

- Operates on `object` and `IBobject`
- Erases generic type information
- Used exclusively for runtime resolution and invocation
- **Must never throw exceptions**

Consumers are expected to build serializers by deriving from the strongly typed base classes described below, rather than implementing this interface directly.

---

### `BencodeSerializer<TOrigin, TTarget>`

This is the canonical base class for all serializers.

- `TOrigin` is the CLR type being serialized or deserialized
- `TTarget` is the concrete Bencode object type

Responsibilities:

- Provide strongly typed `TrySerialize` / `TryDeserialize` methods
- Adapt those methods to the non-generic `IBencodeSerializer` interface
- Enforce type safety at runtime

All concrete serializers in BencodeDotNet derive (directly or indirectly) from this class.

---

### Specialized Base Classes

#### Reference Types

`ReferenceTypeBencodeSerializer<TOrigin, TTarget>` is intended for reference types:

- `TOrigin` must be a class
- Suitable for complex objects, collections, and composite structures

Typical responsibilities include:

- Validating nullability
- Creating new instances during deserialization
- Delegating serialization of nested values

#### Unmanaged Value Types

`UnmanagedTypeBencodeSerializer<TOrigin, TTarget>` targets unmanaged value types:

- `TOrigin` must be `unmanaged`
- Intended for primitives and blittable framework types

Deserialization guarantees that when the method returns `true`, the output value is fully defined.

---

## Built-in Serializer Registry

The built-in serializer registry is **immutable** and reserved exclusively for primitive and framework types shipped with BencodeDotNet.

Primitive and framework serializers are registered explicitly in a centralized registry.

```csharp
BencodeSerializer.TypeSerializers
```

Characteristics of the registry:

- Immutable and thread-safe
- Maps CLR types to **serializer types**, not instances
- Reserved exclusively for built-in primitive and framework serializers
- **Cannot be extended or modified by consumers**
- No reflection-based scanning or auto-registration

Serializer instances are created on demand using `Activator.CreateInstance`.

Any construction or validation failure results in a graceful `false` return.

---

## Serializer Resolution Order

When a serializer is requested for a CLR type, resolution proceeds in the following order:

1. **Attribute-declared serializer**
2. **Enumerable and dictionary serializers**
3. **Built-in registry lookup**
4. **Reflection-based serializer** (if the type is eligible)

Resolution stops at the first successful match.

No exceptions are propagated during resolution.

---

## Attribute-Based Serialization

Custom types can explicitly declare their serializer using `BencodeSerializerAttribute`.

```csharp
[BencodeSerializer(typeof(MyTypeBencodeSerializer))]
public sealed class MyType
{
    // ...
}
```

### Rules and Constraints

- The attribute participates in the **first tier** of resolution
- The declared serializer type:
  - Must implement `IBencodeSerializer`
  - **Must derive from** `BencodeSerializer<TOrigin, TTarget>`
  - Must declare `TOrigin` **exactly equal** to the annotated type
- Open generic serializer types are rejected

The attribute is **metadata-only**:

- It does not instantiate serializers
- It does not influence global registration

All instantiation is performed by the central resolver.

Optional constructor arguments may be supplied via the attribute and are forwarded during instantiation.

---

## Enumerable and Dictionary Serializers

BencodeDotNet provides built-in support for common collection abstractions.

### Dictionaries

Types implementing `IDictionary<TKey, TValue>` are handled by:

- `DictionaryBencodeSerializer<TKey, TValue>`

Dictionary serializers:

- Require both key and value serializers to be resolvable
- Enforce key validity and ordering rules
- Reject unsupported or invalid key types

### Enumerables

Types implementing `IEnumerable<T>` are handled by:

- `ArrayBencodeSerializer<T>` for arrays
- `EnumerableBencodeSerializer<T>` for other enumerable types

`string` is explicitly excluded from enumerable handling.

Enumerable serializers:

- Serialize elements in sequence order
- Delegate element serialization to the resolved element serializer
- Fail if any element cannot be serialized

---

## Reflection-based serializer

BencodeDotNet provides an optional **reflection-based serializer** that enables automatic serialization and deserialization of simple object (POCO) types without requiring a custom serializer implementation.

This serializer acts as a **fallback mechanism** in the serializer resolution process and is only used when no explicit serializer is declared via attributes and no registered serializer exists for the target type.

### Purpose and scope

The reflection-based serializer is designed to cover common, straightforward scenarios:

- Plain CLR objects with a public parameterless constructor
- Public instance members (properties or fields)
- Member types that already have resolvable Bencode serializers

It intentionally avoids attempting to support complex or ambiguous cases (inheritance hierarchies, polymorphism, private members, etc.) in order to keep behavior predictable and safe.

### Supported types

A type is eligible for reflection-based serialization only if all of the following conditions are met:

- The type is **not** abstract
- The type is **not** an interface
- The type is **not** `object`
- The type is **not** a primitive, enum, or pointer type
- The type declares a **public parameterless constructor**

If any of these conditions are not satisfied, the reflection-based serializer will not be instantiated.

### Member discovery

During serializer initialization, the type is inspected once using reflection and a metadata cache is built. Only the following members are considered:

- Public instance properties or fields
- Members with both a readable getter and a writable setter
- Members whose types have a resolvable Bencode serializer

Private members, static members, and members without setters are intentionally ignored.

Each eligible member is mapped to a Bencode dictionary entry using its resolved key and serializer.

### Serialization behavior

When serializing an object:

- A new `Bdictionary` is created
- Each eligible member is read using its compiled getter
- Members whose values are `null` are skipped
- Each non-null value is serialized using the resolved member serializer
- The resulting key/value pairs are added to the dictionary

If a member serializer fails unexpectedly, serialization throws an `InvalidOperationException`.

### Deserialization behavior

When deserializing a `Bdictionary`:

- A new instance of the target type is created using its parameterless constructor
- Each key/value pair in the dictionary is processed
- If a key does not correspond to a known member, it is ignored
- If a matching member is found:
  - The value is deserialized using the member’s serializer
  - The resulting value is assigned using the compiled setter

Unknown dictionary keys are silently ignored, allowing forward-compatible payloads.

If a member fails to deserialize, an `InvalidOperationException` is thrown.

---

## Implementing a Custom Serializer

### Step 1: Derive from the Correct Base Class

- Use `UnmanagedTypeBencodeSerializer<TOrigin, TTarget>` for primitives or blittable structs
- Use `ReferenceTypeBencodeSerializer<TOrigin, TTarget>` for classes and composite types

### Step 2: Implement the Typed Methods

Implement:

```csharp
bool TrySerialize(TOrigin input, out TTarget? output);
bool TryDeserialize(TTarget input, out TOrigin? output);
```

Guidelines:

- Always derive from `BencodeSerializer<TOrigin, TTarget>` (directly or indirectly)
- Choose the appropriate specialization for your CLR type
- Never throw exceptions
- Perform full validation before producing output
- Return `false` on any failure
- Do not partially initialize output values

### Step 3: Declare or Register

Choose **one** of the following extension mechanisms:

- Apply `BencodeSerializerAttribute` to the target CLR type (the standard and recommended approach for custom or extensible types)
- Pass an explicit serializer instance to encoding or decoding APIs that accept custom serializers

Custom serializers **must not** attempt to register themselves in the built-in registry, which is immutable and reserved for framework and primitive types.

---

## Common Pitfalls

- Not deriving from `BencodeSerializer<TOrigin, TTarget>` when implementing a custom serializer
- Using an incorrect `TOrigin` type in the serializer base class
- Throwing exceptions instead of returning `false`
- Relying on implicit registration or reflection-based discovery
- Attempting to serialize unsupported nested types without resolvable serializers

---

## Design Intent

The serializer system is designed to be:

- Explicit and auditable
- Predictable in behavior
- Safe under malformed or adversarial input
- Easy to extend without global side effects

All serializer resolution logic is centralized, and all failure modes are explicit.

This design favors correctness and clarity over convenience or magic.
