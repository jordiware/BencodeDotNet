# Validation and Limits

This document defines the validation rules, limits, and behavioral guarantees enforced by **BencodeDotNet** during encoding and decoding operations.

It is intended as a _contractual reference_: if an operation succeeds, all guarantees described here hold. If any rule or limit is violated, the operation fails deterministically.

This document applies equally to synchronous and asynchronous decoding APIs.

---

## Scope and Intent

BencodeDotNet implements **strict Bencode validation**. It does not attempt to recover from malformed input, apply best-effort parsing, or silently ignore invalid data.

The goals of the validation model are:

- Correctness with respect to the Bencode specification
- Predictable and auditable behavior
- Resistance to malformed or hostile inputs
- Clear and atomic failure semantics

---

## Grammar Enforcement

All input data is validated against the Bencode grammar exactly.

The decoder enforces, among others, the following rules:

- Integers must follow the form `i<digits>e`
- Leading zeros are rejected (except for zero itself)
- String length prefixes must be valid decimal integers
- String payloads must match the declared length
- Lists (`l...e`) and dictionaries (`d...e`) must be properly terminated
- String payloads are interpreted according to the configured `BencodeOptions.TextEncoding`
  and must match the declared length in encoded bytes.

Any deviation from the grammar results in immediate failure.

---

## Dictionary Validation Rules

Dictionary objects are subject to additional structural constraints.

BencodeDotNet enforces that:

- Dictionary keys are Bencode strings
- Keys are lexicographically ordered
- Duplicate keys are rejected
- Dictionary boundaries are strictly respected

These rules follow the BitTorrent specification and prevent ambiguous or non-canonical representations.

---

## Structural Validation and Depth Limits

Nested Bencode structures are validated incrementally during decoding.

### Maximum Depth

- A configurable maximum nesting depth is enforced
- Exceeding the maximum depth causes immediate failure
- The default depth limit is chosen to balance safety and typical usage

Depth limits protect against:

- Stack exhaustion
- Maliciously crafted deeply nested inputs
- Unbounded resource consumption

### Non-Recursive Parsing

Decoding is implemented without recursive descent.

- Explicit state tracking is used
- Call stack growth is bounded
- Deep but valid inputs are handled safely up to the configured limit

---

## Type-Level Validation During Deserialization

Decoding into CLR types introduces an additional validation layer.

For a deserialization operation to succeed:

- The Bencode data must be structurally valid
- A compatible serializer must be resolvable for the target CLR type, either:
  - Automatically via type-declared attributes or the central serializer registry
  - Explicitly provided by the caller
- The resolved serializer must accept the Bencode object
- All nested serializers must also succeed

If any serializer in the object graph fails, the entire operation fails.

No partially initialized CLR objects are ever exposed.

---

## Failure Semantics

Failure behavior is explicit and consistent.

- Parsing and decoding failures result in controlled exceptions
- Serializer failures are reported via `false` return values
- No partial results are returned
- No internal state is leaked or reused

There is no silent truncation, fallback behavior, or partial decoding.

---

## Performance and Safety Guarantees

BencodeDotNet provides the following guarantees:

- Linear-time decoding with respect to input size
- No quadratic parsing behavior
- Streaming-safe decoding using `PipeReader`
- `CancellationToken` is respected during asynchronous decoding from streams or files.
- If cancellation occurs, decoding aborts immediately and no partial result is returned.
- Memory usage grows proportionally to input size and structure

These guarantees apply to both synchronous and asynchronous APIs.

### Encoding Guarantees

- `BencodeEncoder` enforces all configured limits during encoding:
  - Maximum payload length
  - Maximum nesting depth
  - Container size limits
- Serializers are required to produce non-null `IBobject` results
- Failures in serialization result in deterministic exceptions

---

## What Is Not Validated

The following concerns are explicitly out of scope for BencodeDotNet validation:

- Semantic validation of domain-specific data
- Cross-field or cross-object invariants
- Application-level constraints
- Business rules beyond the Bencode specification

Such validation is the responsibility of the consuming application.

---

## Summary of Guarantees

If a decoding or deserialization operation succeeds:

- The input data was valid Bencode
- All structural and ordering rules were enforced
- All configured limits were respected
- The resulting object graph is complete and consistent

If an operation fails:

- The failure is explicit
- No partial results are exposed
- No undefined behavior occurs

This validation model prioritizes correctness, safety, and predictability over permissive parsing.
