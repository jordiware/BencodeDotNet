# Working with Decoded Objects

After decoding Bencode data, BencodeDotNet represents the result as a strongly typed, immutable object model.

This document explains how to inspect, traverse, and safely work with decoded Bencode values.

---

## The `IBobject` Interface

All decoded Bencode values implement the common interface:

```csharp
public interface IBobject { }
```

This allows you to handle decoded values generically while still preserving type safety through runtime checks or pattern matching.

---

## Concrete Bencode Types

A decoded value will always be one of the following concrete types:

| Bencode type | .NET type     |
|--------------|---------------|
| Integer      | `Binteger`    |
| Byte string  | `Bstring`     |
| List         | `Blist`       |
| Dictionary   | `Bdictionary` |

You should always explicitly handle these cases when processing decoded data.

---

## Type Inspection and Pattern Matching

The recommended way to work with decoded values is to use pattern matching.

```csharp
switch (value)
{
    case Binteger i:
        Console.WriteLine($"Integer: {i.Value}");
        break;

    case Bstring s:
        Console.WriteLine($"String length: {s.Length}");
        break;

    case Blist list:
        Console.WriteLine($"List with {list.Count} items");
        break;

    case Bdictionary dict:
        Console.WriteLine($"Dictionary with {dict.Count} entries");
        break;

    default:
        throw new InvalidOperationException("Unknown Bencode type");
}
```

This approach is explicit, readable, and robust against future changes.

---

## Working with Integers

`Binteger` represents a signed integer value.

```csharp
var number = (Binteger)value;
long rawValue = number.Value;
```

Integers are validated during decoding and are guaranteed to be well-formed.

---

## Working with Byte Strings

`Bstring` represents an opaque sequence of bytes.

```csharp
var str = (Bstring)value;
ReadOnlyMemory<byte> bytes = str.Bytes;
```

If the string represents text, you may decode it using an explicit encoding:

```csharp
string text = Encoding.UTF8.GetString(str.Bytes.Span);
```

BencodeDotNet does not assume any character encoding for byte strings.

---

## Working with Lists

`Blist` represents an ordered, immutable collection of `IBobject` values.

```csharp
var list = (Blist)value;

foreach (IBobject item in list)
{
    // Process each item
}
```

Lists may contain heterogeneous values, including nested lists and dictionaries.

---

## Working with Dictionaries

`Bdictionary` represents a mapping of `Bstring` keys to `IBobject` values.

```csharp
var dict = (Bdictionary)value;

foreach (var (key, val) in dict)
{
    string keyText = Encoding.UTF8.GetString(key.Bytes.Span);
    // Process value
}
```

### Accessing Values by Key

```csharp
var key = new Bstring(Encoding.UTF8.GetBytes("info"));

if (dict.TryGetValue(key, out IBobject info))
{
    // Use info
}
```

Dictionary keys are guaranteed to be sorted according to the Bencode specification.

---

## Nested Structures

Bencode data is often deeply nested.

When traversing nested structures:

- Prefer iterative traversal over recursion
- Validate expected types at each level
- Handle missing keys explicitly

Example:

```csharp
if (value is Bdictionary root &&
    root.TryGetValue(new Bstring(Encoding.UTF8.GetBytes("info")), out IBobject info) &&
    info is Bdictionary infoDict)
{
    // Safely work with infoDict
}
```

---

## Immutability Guarantees

All decoded objects are immutable.

This means:

- Decoded data cannot be modified in-place
- Objects are safe to share across threads
- Any transformation requires creating new objects

---

## Best Practices

- Use pattern matching to inspect types
- Treat `Bstring` as raw bytes unless you control the encoding
- Validate structure and types explicitly when handling untrusted input
- Avoid assumptions about optional fields

---

This document covers how to work with decoded values. For detailed information on safety limits and tuning, refer to **safety-and-limits.md**.

