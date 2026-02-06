---
title: Parsers
weight: 3
---

Glyph11 provides two HTTP/1.1 header parsers with different security/performance tradeoffs.

## Parser Comparison

| Feature | HardenedParser | FlexibleParser |
|---------|---------------|----------------|
| **Namespace** | `Glyph11.Parser.Hardened` | `Glyph11.Parser.FlexibleParser` |
| **Validation** | RFC 9110/9112 compliant | Minimal |
| **Resource limits** | Configurable via `ParserLimits` | None |
| **Method validation** | Token characters only | None |
| **Header name validation** | Token characters only | None |
| **Header value validation** | Field-value characters only | None |
| **Bare LF rejection** | Yes | No |
| **Obs-fold rejection** | Yes | No |
| **HTTP version** | Format validated (`HTTP/X.Y`) | Not validated |
| **Malformed lines** | Throws `HttpParseException` | Silently skipped |
| **Multi-segment** | Auto-linearizes to ROM path | Auto-linearizes to ROM path |
| **SIMD-accelerated** | Yes (`SearchValues<byte>`) | N/A |

## Choosing a Parser

**Use `HardenedParser`** (recommended) when:

- Parsing untrusted input from the network
- Building internet-facing HTTP servers
- Security compliance is required

**Use `FlexibleParser`** when:

- Input is pre-validated or from a trusted source
- Maximum throughput is the priority
- Operating behind a hardened reverse proxy

## Performance

The HardenedParser adds ~1.4-1.6x overhead over FlexibleParser for full RFC compliance. Validation uses SIMD-accelerated `SearchValues<byte>` and `IndexOfAnyExcept` to minimize the cost.

| Payload | HardenedParser (ROM) | FlexibleParser (ROM) | Overhead |
|---------|--------------------:|--------------------:|:--------:|
| ~80B    | 93.5 ns             | 68.8 ns             | 1.36x    |
| 1KB     | 213.0 ns            | 142.3 ns            | 1.50x    |
| 4KB     | 675.8 ns            | 415.2 ns            | 1.63x    |

{{< callout type="info" >}}
Both parsers validate HTTP syntax. For semantic checks (request smuggling, path traversal), use [`RequestSemantics`](../security/request-semantics) after parsing.
{{< /callout >}}
