# Documentation Index

This directory contains the reference documentation for BencodeDotNet.
Each document focuses on a specific aspect of the library.

## Core Usage

- [`reading_decoding.md`](reading_decoding.md)
  How to read and decode Bencode data into application values, covering strict
  decoding, streaming readers, single vs multiple object semantics, and error handling.

- [`encoding_writing.md`](encoding_writing.md)
  How to encode CLR values into Bencode and write Bencode data to streams or files,
  including validation guarantees, serializer resolution, and streaming output APIs.

## Serialization

- [`serializers.md`](serializers.md)
  Detailed explanation of the serializer architecture, built-in registry,
  resolution rules, attribute-based discovery, reflection-based fallback
  serialization, and how to implement and extend custom serializers.

## Validation and Safety Guarantees

- [`validation_and_limits.md`](validation_and_limits.md)
  Formal description of grammar validation, structural rules, depth limits,
  failure semantics, and performance and safety guarantees enforced during
  decoding, streaming, and deserialization.
