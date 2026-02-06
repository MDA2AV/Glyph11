---
title: Request Semantics
weight: 2
---

**Namespace:** `Glyph11.Validation`

After parsing, run semantic checks to detect protocol-level attacks that are syntactically valid but dangerous.

## Usage

```csharp
using Glyph11.Validation;

// Detect conflicting Content-Length values (request smuggling vector)
if (RequestSemantics.HasConflictingContentLength(request))
    // reject: multiple Content-Length headers with different values

// Detect Transfer-Encoding + Content-Length (request smuggling vector)
if (RequestSemantics.HasTransferEncodingWithContentLength(request))
    // reject: RFC 9112 Section 6.1 violation

// Detect path traversal attempts
if (RequestSemantics.HasDotSegments(request))
    // reject: /../ or /./ segments in path
```

All checks use case-insensitive ASCII comparison for header names.

## Checks

### HasConflictingContentLength

Detects multiple `Content-Length` headers with different values. This is a classic HTTP request smuggling vector where a frontend and backend disagree on the message body length.

{{< callout type="info" >}}
Per RFC 9110 Section 8.6, a server MUST reject requests with conflicting Content-Length values.
{{< /callout >}}

### HasTransferEncodingWithContentLength

Detects requests that include both `Transfer-Encoding` and `Content-Length` headers. Per RFC 9112 Section 6.1, if both are present, the `Transfer-Encoding` takes precedence — but the combination is a common smuggling vector.

### HasDotSegments

Detects `/../` or `/./` sequences in the request path. These are path traversal indicators that could allow access to files outside the intended directory.

## Integration Pattern

Run all semantic checks after successful parsing:

```csharp
if (HardenedParser.TryExtractFullHeader(ref buffer, request, in limits, out var bytesRead))
{
    if (RequestSemantics.HasTransferEncodingWithContentLength(request))
        throw new InvalidOperationException("Request smuggling attempt.");

    if (RequestSemantics.HasConflictingContentLength(request))
        throw new InvalidOperationException("Conflicting Content-Length.");

    if (RequestSemantics.HasDotSegments(request))
        throw new InvalidOperationException("Path traversal attempt.");

    // Safe to process request
}
```
