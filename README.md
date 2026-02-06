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

## CI Workflows

### Benchmarks

The **Benchmark** workflow (`.github/workflows/benchmark.yml`) measures parser throughput and allocation using BenchmarkDotNet.

| Trigger | Job | What it does |
|---------|-----|--------------|
| `pull_request` | **Parser Benchmarks** | Runs `FlexibleParserBenchmark` and `HardenedParserBenchmark`, compares against the baseline on `gh-pages`, and posts a comment on the PR. Fails if any metric regresses by more than 15%. |
| `workflow_dispatch` | **Full Benchmarks** | Runs all benchmarks (parsers + `AllSemanticChecksBenchmark`), updates the baseline on `gh-pages`, and triggers a docs site rebuild. |

**Data flow:** benchmark results are stored as `benchmarks/data.js` on the `gh-pages` branch. The docs site loads this file to render trend charts at [/benchmarks/](https://MDA2AV.github.io/Glyph11/benchmarks/).

To publish updated benchmark data:

1. Merge your changes to `main`.
2. Go to **Actions > Benchmark > Run workflow** on `main`.

### Compliance Probe

The **Probe** workflow (`.github/workflows/probe.yml`) tests HTTP/1.1 compliance across multiple server frameworks using [Glyph11.Probe](src/Glyph11.Probe), a tool that sends malformed and ambiguous HTTP requests and checks the server's response against strict RFC 9110/9112 expectations.

Servers tested: **Glyph11** (raw TCP + HardenedParser), **Kestrel** (ASP.NET Core), **Flask** (Python), **Express** (Node.js), **Spring Boot** (Java), **Quarkus** (Java), **Nancy** (.NET), **Jetty** (Java), **Nginx** (native), **Apache** (native), **Caddy** (native).

| Trigger | What it does |
|---------|--------------|
| `pull_request` | Starts all three servers, probes each one, evaluates results with strict status-code matching (e.g. a parser error must return `400`, not `404`), and posts a comparison table as a PR comment. Never fails the build — this is informational. |
| `workflow_dispatch` | Same as above, plus pushes `probe/data.js` to `gh-pages` and triggers a docs site rebuild. |

**Data flow:** probe results are stored as `probe/data.js` on the `gh-pages` branch. The docs site loads this file to render the comparison matrix at [/probe-results/](https://MDA2AV.github.io/Glyph11/probe-results/).

To publish updated probe data:

1. Merge your changes to `main`.
2. Go to **Actions > Probe > Run workflow** on `main`.
