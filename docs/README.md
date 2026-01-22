# Documentation Index

This directory contains the reference documentation for BencodeDotNet.
Each document focuses on a specific aspect of the library.

## Core Usage

- [`encoding_and_decoding.md`](encoding_and_decoding.md)  
  How to encode and decode Bencode data, including validation guarantees,
  error handling, and sync/async APIs.

## Serialization

- [`serializers.md`](serializers.md)  
  Detailed explanation of the serializer architecture, built-in registry,
  resolution rules, and how to implement and extend custom serializers.

## Validation and Safety Guarantees

- [`validation_and_limits.md`](validation_and_limits.md)  
  Formal description of grammar validation, structural rules, depth limits,
  failure semantics, and performance and safety guarantees enforced during
  decoding and deserialization.
