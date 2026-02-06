---
title: Request Semantics
weight: 2
---

**Namespace:** `Glyph11.Validation`

After parsing, run semantic checks to detect protocol-level attacks that are syntactically valid but dangerous.

## Usage

```csharp
using Glyph11;
using Glyph11.Validation;

if (RequestSemantics.HasTransferEncodingWithContentLength(request))
    // reject: RFC 9112 Section 6.1 violation

if (RequestSemantics.HasConflictingContentLength(request))
    // reject: multiple Content-Length headers with different values

if (RequestSemantics.HasDotSegments(request))
    // reject: /../ or /./ segments in path
```

All checks use case-insensitive ASCII comparison for header names.

## Request Smuggling

### HasTransferEncodingWithContentLength

Detects requests that include both `Transfer-Encoding` and `Content-Length` headers. Per RFC 9112 Section 6.1, if both are present, the `Transfer-Encoding` takes precedence — but the combination is a common CL.TE / TE.CL smuggling vector.

### HasConflictingContentLength

Detects multiple `Content-Length` headers with different values. This is a classic HTTP request smuggling vector where a frontend and backend disagree on the message body length.

{{< callout type="info" >}}
Per RFC 9110 Section 8.6, a server MUST reject requests with conflicting Content-Length values.
{{< /callout >}}

### HasConflictingCommaSeparatedContentLength

Detects a single `Content-Length` header with comma-separated values that differ (e.g. `Content-Length: 42, 0`). RFC 9112 Section 6.2 allows comma-separated Content-Length, but all values must be identical.

### HasInvalidContentLengthFormat

Detects non-digit characters in `Content-Length` values (e.g. `Content-Length: abc`, `Content-Length: 1e5`). Different parsers interpret these differently, creating smuggling opportunities.

**RFC:** 9110 Section 8.6 — Content-Length is `1*DIGIT`.

### HasContentLengthWithLeadingZeros

Detects `Content-Length` values with leading zeros (e.g. `Content-Length: 0200`). Some parsers interpret leading zeros as octal, creating body length disagreements.

### HasInvalidTransferEncoding

Detects `Transfer-Encoding` values that are not exactly `chunked` (after trimming OWS). Obfuscated values like `xchunked`, `"chunked"`, or `chunked-thing` are TE.TE smuggling vectors.

**RFC:** 9112 Section 6.1.

## Host Header Attacks

### HasInvalidHostHeaderCount

Detects requests without exactly one `Host` header. Missing Host headers cause routing confusion; multiple Host headers cause disagreements between frontend and backend (SSRF).

**RFC:** 9112 Section 3.2 — exactly one Host header required for HTTP/1.1.

## Path Traversal

### HasDotSegments

Detects `/../` or `/./` segments in the request path, including trailing `/..` and `/.`. These allow directory traversal to access files outside the intended root.

**RFC:** 3986 Section 5.2.4.

### HasBackslashInPath

Detects backslash characters (`\`) in the path. Backslashes are treated as path separators on Windows, enabling traversal via `\..\`.

### HasDoubleEncoding

Detects `%25` sequences in the path. `%252e%252e` decodes to `%2e%2e` after one pass, then `..` after a second. This bypasses single-decode security filters.

### HasEncodedNullByte

Detects `%00` in the path. Null bytes cause C-based file systems to truncate the path at the null byte. `file.txt%00.jpg` passes extension checks but opens `file.txt`.

### HasOverlongUtf8

Detects overlong UTF-8 sequences in the path. Overlong encodings (e.g. `0xC0 0xAF` for `/`) bypass ASCII-only path checks.

**RFC:** 3629 Section 3 — overlong sequences are forbidden.

## Fragment Rejection

### HasFragmentInRequestTarget

Detects fragment identifiers (`#`) in the request path. Fragments must not appear in HTTP request-targets. Their presence indicates injection or malformed input.

**RFC:** 9112 Section 3.2.

## Performance

All checks are **zero-allocation** and run in nanoseconds. Path-scanning checks (DotSegments, Fragment, Backslash, DoubleEncoding, EncodedNull, OverlongUtf8) are constant-time since they only inspect the request path. Header-scanning checks scale linearly with header count.

| Category | Small (~80B) | 32KB (~151 headers) |
|----------|------------:|--------------------:|
| Header-scanning (worst) | ~9 ns | ~208 ns |
| Path-scanning (worst) | ~2.5 ns | ~2.6 ns |
| All 13 checks combined | ~60 ns | ~1.4 us |

See [Performance](/docs/performance/) for full benchmark tables.

## Integration Pattern

Run all semantic checks after successful parsing:

```csharp
if (HardenedParser.TryExtractFullHeader(ref buffer, request, in limits, out var bytesRead))
{
    // Request smuggling
    if (RequestSemantics.HasTransferEncodingWithContentLength(request))
        throw new HttpParseException("Request smuggling: TE + CL.");

    if (RequestSemantics.HasConflictingContentLength(request))
        throw new HttpParseException("Conflicting Content-Length values.");

    if (RequestSemantics.HasConflictingCommaSeparatedContentLength(request))
        throw new HttpParseException("Conflicting comma-separated Content-Length.");

    if (RequestSemantics.HasInvalidContentLengthFormat(request))
        throw new HttpParseException("Invalid Content-Length format.");

    if (RequestSemantics.HasContentLengthWithLeadingZeros(request))
        throw new HttpParseException("Content-Length has leading zeros.");

    if (RequestSemantics.HasInvalidTransferEncoding(request))
        throw new HttpParseException("Invalid Transfer-Encoding value.");

    // Host header
    if (RequestSemantics.HasInvalidHostHeaderCount(request))
        throw new HttpParseException("Invalid Host header count.");

    // Path traversal
    if (RequestSemantics.HasDotSegments(request))
        throw new HttpParseException("Path traversal detected.");

    if (RequestSemantics.HasBackslashInPath(request))
        throw new HttpParseException("Backslash in path.");

    if (RequestSemantics.HasDoubleEncoding(request))
        throw new HttpParseException("Double encoding detected.");

    if (RequestSemantics.HasEncodedNullByte(request))
        throw new HttpParseException("Encoded null byte in path.");

    if (RequestSemantics.HasOverlongUtf8(request))
        throw new HttpParseException("Overlong UTF-8 in path.");

    // Fragment
    if (RequestSemantics.HasFragmentInRequestTarget(request))
        throw new HttpParseException("Fragment in request-target.");

    // Safe to process request
}
```
