---
title: FlexibleParser
weight: 2
---

**Namespace:** `Glyph11.Parser.FlexibleParser`

```csharp
public static partial class FlexibleParser
```

A minimal-validation HTTP/1.1 header parser optimized for maximum throughput.

## Methods

### TryExtractFullHeader

```csharp
public static bool TryExtractFullHeader(
    ref ReadOnlySequence<byte> input,
    BinaryRequest request,
    out int bytesReadCount)
```

Entry point that auto-dispatches based on segment layout. If the input is a single segment, delegates to `TryExtractFullHeaderReadOnlyMemory`. If multi-segment, checks for header completeness, linearizes via `ToArray()`, then parses.

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `input` | `ref ReadOnlySequence<byte>` | The input buffer to parse |
| `request` | `BinaryRequest` | The request object to populate |
| `bytesReadCount` | `out int` | Number of bytes consumed on success |

**Returns:** `true` if a complete header was parsed; `false` if the header is incomplete (waiting for more data).

**Throws:** `HttpParseException` for structurally unparseable input.

---

### TryExtractFullHeaderReadOnlyMemory

```csharp
public static bool TryExtractFullHeaderReadOnlyMemory(
    ref ReadOnlyMemory<byte> input,
    BinaryRequest request,
    out int bytesReadCount)
```

Single-segment parser operating on contiguous memory. Zero-allocation — all parsed fields are `ReadOnlyMemory<byte>` slices into the input.

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `input` | `ref ReadOnlyMemory<byte>` | Contiguous input buffer |
| `request` | `BinaryRequest` | The request object to populate |
| `bytesReadCount` | `out int` | Number of bytes consumed on success |

**Returns:** `true` if a complete header was parsed; `false` if the header is incomplete.

**Throws:** `HttpParseException` for structurally unparseable input.
