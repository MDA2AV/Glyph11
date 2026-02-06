---
title: HardenedParser
weight: 1
---

**Namespace:** `Glyph11.Parser.Hardened`

A security-hardened HTTP/1.1 header parser with RFC 9110/9112 validation and configurable resource limits.

## Usage

```csharp
using Glyph11.Parser.Hardened;

var limits = ParserLimits.Default;

// Entry point — auto-dispatches based on segment layout
bool ok = HardenedParser.TryExtractFullHeader(
    ref buffer,       // ReadOnlySequence<byte>
    request,          // BinaryRequest
    in limits,        // ParserLimits
    out int bytesRead
);

// Direct ROM access (single contiguous buffer)
ReadOnlyMemory<byte> mem = ...;
bool ok = HardenedParser.TryExtractFullHeaderROM(
    ref mem, request, in limits, out int bytesRead
);
```

## Return Values

- Returns `false` if the header is incomplete (no `\r\n\r\n` terminator found). This is not an error — the caller should wait for more data.
- Returns `true` when a complete header has been parsed. `bytesReadCount` indicates how many bytes were consumed.
- Throws `HttpParseException` with a descriptive message for any protocol violation.

## Validation Rules

The HardenedParser enforces the following on every request:

### Request Line

- Method must contain only valid RFC 9110 Section 5.6.2 token characters (`A-Z`, `a-z`, `0-9`, `` !#$%&'*+-.^_`|~ ``)
- Method length must not exceed `MaxMethodLength`
- Multiple spaces between request-line components are rejected — RFC 9112 Section 3
- Request-target must not contain control characters (0x00-0x1F, 0x7F) — RFC 9112 Section 3.2
- URL length must not exceed `MaxUrlLength`
- HTTP version must match the format `HTTP/X.Y` (exactly 8 bytes, digits at positions 5 and 7)
- Query parameter count must not exceed `MaxQueryParameterCount`

### Line Endings

- Bare LF (0x0A without preceding 0x0D) is rejected — RFC 9112 Section 2.2
- Obsolete line folding (header lines starting with SP or HTAB) is rejected — RFC 9112 Section 5.2
- Whitespace between header name and colon is rejected — RFC 9112 Section 5.1

### Headers

- Header name must contain only valid token characters
- Header name must not be empty and must not exceed `MaxHeaderNameLength`
- Header value must contain only valid field-value characters (RFC 9110 Section 5.5: HTAB, SP, VCHAR, obs-text)
- Header value length must not exceed `MaxHeaderValueLength`
- Total header count must not exceed `MaxHeaderCount`
- Total header bytes (request line + all headers + terminators) must not exceed `MaxTotalHeaderBytes`
- Lines without a colon separator are rejected (throws, not silently skipped)

### HTTP Version Caching

Common versions (`HTTP/1.1`, `HTTP/1.0`) are cached as static byte arrays to avoid per-request allocation.

## Performance

Validation uses SIMD-accelerated `SearchValues<byte>` with `IndexOfAnyExcept` for token and field-value character checks, and vectorized `IndexOf` for bare-LF detection. This keeps the overhead to ~1.4-1.6x over the unvalidated `FlexibleParser`.

See the [Benchmarks](/Glyph11/benchmarks/) page for detailed numbers and trend charts.

## Multi-Segment Handling

When input arrives as multiple `ReadOnlySequence<byte>` segments (common with `PipeReader`), the entry point automatically linearizes the buffer before parsing:

1. Checks for `\r\n\r\n` presence using `SequenceReader` — returns `false` with zero allocation if incomplete
2. Calls `ToArray()` to produce a single contiguous byte array
3. Parses using the ROM path for maximum speed

See [Multi-Segment Handling](../architecture/multi-segment) for details.
