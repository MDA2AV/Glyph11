---
title: Parsers
weight: 3
---

Glyph11 provides the **HardenedParser** — a security-hardened HTTP/1.1 header parser with RFC 9110/9112 validation and configurable resource limits.

## Parser Overview

The `HardenedParser` enforces strict protocol compliance and resource limits on every request. It validates method tokens, header names and values, URL length, and total header size against configurable `ParserLimits`.

| Feature | HardenedParser |
|---------|---------------|
| **Namespace** | `Glyph11.Parser.Hardened` |
| **Validation** | RFC 9110/9112 compliant |
| **Resource limits** | Configurable via `ParserLimits` |
| **Method validation** | Token characters only |
| **Header validation** | Name + value character checks |
| **HTTP version** | Format validated (`HTTP/X.Y`) |
| **Malformed lines** | Throws `InvalidOperationException` |
| **Multi-segment** | Auto-linearizes to ROM path |

## Choosing a Parser

The `HardenedParser` is recommended for all use cases, especially when:

- Parsing untrusted input from the network
- Building internet-facing HTTP servers
- Security compliance is required

{{< callout type="info" >}}
The parser validates HTTP syntax. For semantic checks (request smuggling, path traversal), use [`RequestSemantics`](../security/request-semantics) after parsing.
{{< /callout >}}
