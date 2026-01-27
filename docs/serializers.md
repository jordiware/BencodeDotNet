# Serializers

This document explains how **BencodeDotNet** converts between CLR objects and Bencode values using its serializer system. It covers the core concepts, the serializer contracts, how serializers are resolved at runtime, what is built in, and how consumers can safely extend the system.

The serializer model is intentionally explicit, non-reflective by default, and largely non-throwing. Its goal is to provide predictable, auditable, and safe behavior when encoding and decoding Bencode data.

---

## At a Glance

- Serializers convert between a CLR type (**origin**) and a concrete `IBobject` (**target**)
- All serializers follow a **non-throwing** `Try*` contract
- Serializer resolution is centralized and deterministic
- Built-in serializers are immutable and reserved for framework types
- Custom serializers are opt-in and explicitly declared

---

## Core Concept

A **serializer** in BencodeDotNet is responsible for converting between:

- A CLR type (the *origin* type)
- A concrete Bencode object implementing `IBobject` (the *target* type)

Serialization and deserialization are symmetrical whenever possible and always follow a non-throwing contract: failures are reported via a `bool` return value rather than exceptions.

At runtime, serializers are resolved dynamically based on a CLR `Type` using a centralized resolver.

---

## Serializer Contracts

Understanding the serializer contracts is essential before implementing or extending the system.

### `IBencodeSerializer`

`IBencodeSerializer` defines the non-generic boundary used internally by the serializer registry and resolver.

Key characteristics:

- Operates on `object` and `IBobject`
- Erases generic type information
- Used exclusively for runtime resolution and invocation
- **Must never throw exceptions**

Consumers are not expected to implement this interface directly. Instead, serializers should derive from the strongly typed base classes described below.

#### Required Methods

##### `TrySerialize`

```csharp
bool TrySerialize(object input, out IBobject? output);
```

- Serializes a CLR value to a Bencode object.
- Returns `true` if successful, `false` otherwise.
- `output` must be either fully valid or `null` on failure.
- Must validate that the runtime type of `input` is supported.
- Must respect `BencodeOptions` constraints.
- **Do not partially initialize** output on failure.

##### `TryDeserialize`

```csharp
bool TryDeserialize(IBobject input, out object? output);
```

- Deserializes a Bencode object into a CLR value.
- Returns `true` if successful, `false` otherwise.
- `output` must be fully defined when `true`, or `null` on failure.
- Must validate compatibility between `input` and the target CLR type.

##### `WriteToPipeAsync`

```csharp
Task WriteToPipeAsync(object input, PipeWriter writer, CancellationToken cancellationToken = default);
```

- Serializes a CLR value **directly to a `PipeWriter`** in Bencode format.
- Avoids creating intermediate `IBobject` instances.
- Intended for high-performance streaming.
- The `PipeWriter` is **owned by the caller**; do **not flush, complete, or dispose** it.
- Must respect `CancellationToken`.
- Partial output may occur if canceled; only `OperationCanceledException` is allowed.

---

### `BencodeSerializer<TOrigin, TTarget>`

This is the canonical base class for all serializers.

- `TOrigin` is the CLR type being serialized or deserialized
- `TTarget` is the concrete Bencode object type

Responsibilities:

- Expose strongly typed `TrySerialize` / `TryDeserialize` methods
- Adapt those methods to the non-generic `IBencodeSerializer` interface
- Enforce runtime type safety

All concrete serializers in BencodeDotNet derive (directly or indirectly) from this class.

---

## Specialized Base Classes

### Reference Types

`ReferenceTypeBencodeSerializer<TOrigin, TTarget>` is intended for reference types:

- `TOrigin` must be a class
- Suitable for complex objects, collections, and composite structures

Typical responsibilities include:

- Validating nullability
- Creating new instances during deserialization
- Delegating serialization of nested values

---

### Unmanaged Value Types

`UnmanagedTypeBencodeSerializer<TOrigin, TTarget>` targets unmanaged value types:

- `TOrigin` must be `unmanaged`
- Intended for primitives and blittable framework types

Deserialization guarantees that when the method returns `true`, the output value is fully initialized.

---

## Implementing a Custom Serializer

Developers who need to extend BencodeDotNet with custom serialization logic should start here.

### 1. Choose the Right Base Class

- `UnmanagedTypeBencodeSerializer<TOrigin, TTarget>` for primitives and blittable structs
- `ReferenceTypeBencodeSerializer<TOrigin, TTarget>` for classes and composite types

---

### 2. Implement the Typed Methods

```csharp
bool TrySerialize(TOrigin input, out TTarget? output);
bool TryDeserialize(TTarget input, out TOrigin? output);
Task WriteToPipeAsync(TOrigin input, PipeWriter writer, CancellationToken cancellationToken = default);
```

Guidelines:

- Always derive from `BencodeSerializer<TOrigin, TTarget>`
- Never throw exceptions
- Fully validate before producing output
- Return `false` on any failure
- Do not partially initialize outputs

---

### 3. Declare or Provide the Serializer

Extend the system using **one** of the following:

- Apply `BencodeSerializerAttribute` to the CLR type (recommended)
- Pass an explicit serializer instance to APIs that accept it

Custom serializers must not attempt to modify the built-in registry.

---

## How Serializers Are Resolved

When a serializer is requested for a CLR type, resolution proceeds in the following order:

1. **Explicit serializer passed as a parameter**
2. **Serializer declared via attribute on the type**
3. **Dictionary, array, and enumerable serializers**
4. **Built-in serializer registry**
5. **Reflection-based serializer (fallback)**

Resolution stops at the first successful match.

No exceptions are propagated during resolution; failures are reported via return values.

---

## Built-in Serializer Registry

The built-in serializer registry contains serializers for primitive and framework types shipped with BencodeDotNet.

```csharp
BencodeSerializer.TypeSerializers
```

Registry characteristics:

- Immutable and thread-safe
- Maps CLR types to **serializer types**, not instances
- Reserved exclusively for built-in serializers
- **Cannot be modified or extended by consumers**
- No reflection-based scanning or auto-registration

Serializer instances are created on demand using `Activator.CreateInstance`. Any construction or validation failure results in a graceful `false` return.

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

- Participates in the **first tier** of resolution
- The declared serializer type:
  - Must implement `IBencodeSerializer`
  - **Must derive from** `BencodeSerializer<TOrigin, TTarget>`
  - Must declare `TOrigin` **exactly equal** to the annotated type
- Open generic serializer types are rejected

The attribute is metadata-only:

- It does not instantiate serializers
- It does not affect global registration

All instantiation is handled by the central resolver. Optional constructor arguments supplied via the attribute are forwarded during creation.

---

## Collection Serializers

BencodeDotNet includes built-in support for common collection abstractions.

### Dictionaries

Types implementing `IDictionary<TKey, TValue>` are handled by `DictionaryBencodeSerializer<TKey, TValue>`.

Dictionary serializers:

- Require resolvable serializers for both keys and values
- Enforce Bencode dictionary key rules and ordering
- Reject unsupported or invalid key types

---

### Enumerables

Types implementing `IEnumerable<T>` are handled by:

- `ArrayBencodeSerializer<T>` for arrays
- `EnumerableBencodeSerializer<T>` for other enumerable types

`string` and `byte[]` are explicitly excluded and have dedicated serializers.

Enumerable serializers:

- Serialize elements in sequence order
- Delegate element handling to the resolved element serializer
- Fail if any element cannot be serialized

---

## Reflection-Based Serializer (Fallback)

BencodeDotNet provides an optional **reflection-based serializer** as a last-resort fallback when no explicit or registered serializer is available.

This mechanism is intentionally conservative and designed to cover simple POCO scenarios without introducing ambiguity or hidden behavior.

### Eligibility

A type is eligible only if all of the following are true:

- The type is not abstract or an interface
- The type is not `object`
- The type is not a primitive, enum, or pointer
- The type declares a public parameterless constructor

If any condition fails, the reflection-based serializer is not used.

---

### Member Discovery

During initialization, the type is inspected once and a metadata cache is built.

Eligible members:

- Public instance properties or fields
- Readable and writable members only
- Member types with resolvable serializers

Private members, static members, and write-only or read-only members are ignored.

Each eligible member maps to a dictionary entry using its resolved key and serializer.

---

### Serialization Behavior

When serializing:

- A new `Bdictionary` is created
- Each eligible member value is read
- `null` values are skipped
- Non-null values are serialized via the member serializer
- Key/value pairs are added to the dictionary

If a member serializer fails unexpectedly, an `InvalidOperationException` is thrown.

---

### Deserialization Behavior

When deserializing:

- A new instance is created via the parameterless constructor
- Each dictionary entry is processed
- Unknown keys are ignored
- Known members are deserialized and assigned

If a member fails to deserialize, an `InvalidOperationException` is thrown. This behavior allows forward-compatible payloads while maintaining strict member correctness.

---

## Common Pitfalls

- Implementing `IBencodeSerializer` directly
- Using an incorrect `TOrigin` type
- Throwing exceptions instead of returning `false`
- Relying on implicit registration
- Serializing nested types without resolvable serializers

---

## Design Intent

The serializer system prioritizes:

- Explicit, auditable behavior
- Deterministic resolution
- Safety under malformed or adversarial input
- Extension without global side effects

Correctness and clarity are favored over convenience or implicit magic.
