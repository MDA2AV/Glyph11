# Glyph11

Glyph11 is a dependency free, low allocation HTTP/1.1 parser for C#. It does not rely on any specific network technology but can be used with any (such as `Socket`, `NetworkStream`, `PipeReader` or anything else).

![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512bd4)
[![NuGet](https://img.shields.io/nuget/v/Glyph11.svg)](https://www.nuget.org/packages/Glyph11/)
[![Docs](https://img.shields.io/badge/docs-online-blue)](https://MDA2AV.github.io/Glyph11/)
[![Coverage](https://img.shields.io/sonar/coverage/MDA2AV_Glyph11?server=https%3A%2F%2Fsonarcloud.io)](https://sonarcloud.io/summary/new_code?id=MDA2AV_Glyph11)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=MDA2AV_Glyph11&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=MDA2AV_Glyph11)

## Usage

Glyph11 works with any source that produces a `ReadOnlySequence<byte>` or `ReadOnlyMemory<byte>` — `PipeReader`, `Socket`, `NetworkStream`, or raw byte arrays.

```csharp
using System.Buffers;
using Glyph11;
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
        throw new HttpParseException("Request smuggling: TE + CL.");

    if (RequestSemantics.HasDotSegments(request))
        throw new HttpParseException("Path traversal detected.");

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

- **ROM path is zero-allocation** — no GC pressure regardless of request size
- **SIMD-accelerated validation** keeps the HardenedParser within ~1.4-1.6x of the unvalidated FlexibleParser
- **Multi-segment linearization** provides ROM-speed parsing with a single upfront allocation

See the [live benchmarks](https://MDA2AV.github.io/Glyph11/benchmarks/) for latest numbers and trend charts.
