---
title: Security
weight: 4
---

Glyph11 provides two layers of security:

1. **Parse-time validation** — The `HardenedParser` enforces RFC 9110/9112 syntax rules and resource limits during parsing.
2. **Post-parse validation** — The `RequestSemantics` class detects protocol-level attacks after parsing.

## Defense in Depth

```
Input → [HardenedParser] → [RequestSemantics] → Application
         │                   │
         ├─ Token validation ├─ Smuggling detection
         ├─ Size limits      ├─ Path traversal
         └─ Format checks    └─ Header conflicts
```

The parser rejects malformed input (invalid characters, oversized fields, missing delimiters). Semantic checks catch valid-but-dangerous patterns like conflicting `Content-Length` headers or `Transfer-Encoding` combined with `Content-Length`.

{{< cards >}}
  {{< card link="parser-limits" title="Parser Limits" subtitle="Configure resource limits for DoS prevention." >}}
  {{< card link="request-semantics" title="Request Semantics" subtitle="Post-parse validation for protocol attacks." >}}
{{< /cards >}}
