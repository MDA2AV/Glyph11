---
title: HardenedParser
weight: 1
---

**Namespace:** `Glyph11.Parser.Hardened`

```csharp
public static partial class HardenedParser
```

A security-hardened HTTP/1.1 header parser with RFC 9110/9112 validation.

## Methods

### TryExtractFullHeader

```csharp
public static bool TryExtractFullHeader(
    ref ReadOnlySequence<byte> input,
    BinaryRequest request,
    in ParserLimits limits,
    out int bytesReadCount)
```

Entry point that auto-dispatches based on segment layout. If the input is a single segment, delegates to `TryExtractFullHeaderROM`. If multi-segment, checks for header completeness, linearizes via `ToArray()`, then parses.

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `input` | `ref ReadOnlySequence<byte>` | The input buffer to parse |
| `request` | `BinaryRequest` | The request object to populate |
| `limits` | `in ParserLimits` | Resource limits to enforce |
| `bytesReadCount` | `out int` | Number of bytes consumed on success |

**Returns:** `true` if a complete header was parsed; `false` if the header is incomplete (waiting for more data).

**Throws:** `HttpParseException` for protocol violations.

---

### TryExtractFullHeaderROM

```csharp
public static bool TryExtractFullHeaderROM(
    ref ReadOnlyMemory<byte> input,
    BinaryRequest request,
    in ParserLimits limits,
    out int bytesReadCount)
```

Single-segment parser operating on contiguous memory. Zero-allocation — all parsed fields are `ReadOnlyMemory<byte>` slices into the input.

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `input` | `ref ReadOnlyMemory<byte>` | Contiguous input buffer |
| `request` | `BinaryRequest` | The request object to populate |
| `limits` | `in ParserLimits` | Resource limits to enforce |
| `bytesReadCount` | `out int` | Number of bytes consumed on success |

**Returns:** `true` if a complete header was parsed; `false` if the header is incomplete.

**Throws:** `HttpParseException` for protocol violations.
