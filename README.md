# Glyph11

Glyph11 is a dependency free, low allocation HTTP/1.1 parser for C#. It does not rely on any specific network technology but can be used with any (such as `Socket`, `NetworkStream`, `PipeReader` or anything else).

[![NuGet](https://img.shields.io/nuget/v/Glyph11.svg)](https://www.nuget.org/packages/Glyph11/)
[![Docs](https://img.shields.io/badge/docs-online-blue)](https://MDA2AV.github.io/Glyph11/)

## Usage

Glyph11 works with any source that produces a `ReadOnlySequence<byte>` or `ReadOnlyMemory<byte>` — `PipeReader`, `Socket`, `NetworkStream`, or raw byte arrays.

```csharp
using System.Buffers;
using Glyph11.Protocol;
using Glyph11.Parser.Hardened;
using Glyph11.Validation;

var request = new BinaryRequest();
var limits = ParserLimits.Default;

ReadOnlySequence<byte> buffer = ...; // from any network source

if (HardenedParser.TryExtractFullHeader(ref buffer, request, in limits, out int bytesRead))
{
    // All parsed fields are zero-copy slices into the original buffer:
    // request.Method.Span  → e.g. "GET"
    // request.Path.Span    → e.g. "/api/users"
    // request.Version.Span → e.g. "HTTP/1.1"
    // request.Headers      → KeyValueList of name/value pairs
    // request.QueryParameters → KeyValueList of query params

    // Run post-parse semantic checks on untrusted input:
    if (RequestSemantics.HasTransferEncodingWithContentLength(request))
        throw new InvalidOperationException("Request smuggling: TE + CL.");

    if (RequestSemantics.HasDotSegments(request))
        throw new InvalidOperationException("Path traversal detected.");

    // Process request, then advance your reader by bytesRead.

    // Reuse between requests — clear instead of reallocating:
    request.Headers.Clear();
    request.QueryParameters.Clear();
}
```

For a complete `PipeReader` integration loop, see the [integration guide](https://MDA2AV.github.io/Glyph11/docs/getting-started/integration/).

## Parsers

Glyph11 ships two parsers:

- **`HardenedParser`** — RFC 9110/9112 compliant with full validation and configurable resource limits. Recommended for internet-facing applications.
- **`FlexibleParser`** — Minimal validation for maximum throughput. Suitable for trusted environments where input is pre-validated.

## Performance

### HardenedParser

| Method                | Mean        | Gen0   | Allocated |
|---------------------- |------------:|-------:|----------:|
| Small_ROM (~80B)      |    93.5 ns  |      - |       0 B |
| Small_MultiSegment    |   197.7 ns  | 0.0057 |     112 B |
| Header1K_ROM          |   213.0 ns  |      - |       0 B |
| Header1K_MultiSegment |   409.9 ns  | 0.0558 |   1,056 B |
| Header4K_ROM          |   675.8 ns  |      - |       0 B |
| Header4K_MultiSegment | 1,455.0 ns  | 0.2193 |   4,128 B |

### FlexibleParser

| Method                | Mean        | Gen0   | Allocated |
|---------------------- |------------:|-------:|----------:|
| Small_ROM (~80B)      |    68.8 ns  |      - |       0 B |
| Small_MultiSegment    |   171.2 ns  | 0.0057 |     112 B |
| Header1K_ROM          |   142.3 ns  |      - |       0 B |
| Header1K_MultiSegment |   311.1 ns  | 0.0558 |   1,056 B |
| Header4K_ROM          |   415.2 ns  |      - |       0 B |
| Header4K_MultiSegment |   930.9 ns  | 0.2193 |   4,128 B |

The HardenedParser adds ~1.4-1.6x overhead over FlexibleParser for full RFC compliance, SIMD-accelerated validation, and resource limit enforcement.