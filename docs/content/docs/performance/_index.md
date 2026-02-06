---
title: Performance
weight: 6
---

All benchmarks use 3 segments for multi-segment cases. ROM uses a single contiguous buffer. Multi-segment input is linearized (copied to a contiguous array) before parsing.

## HardenedParser

Includes full RFC 9110/9112 validation using SIMD-accelerated `SearchValues<byte>` and fused bare-LF checking.

| Method | Mean | Allocated |
|--------|-----:|----------:|
| Small_ROM (~80B) | 93.5 ns | 0 B |
| Small_MultiSegment | 197.7 ns | 112 B |
| Header1K_ROM | 213.0 ns | 0 B |
| Header1K_MultiSegment | 409.9 ns | 1,056 B |
| Header4K_ROM | 675.8 ns | 0 B |
| Header4K_MultiSegment | 1,455.0 ns | 4,128 B |

## FlexibleParser

No validation — parsing only.

| Method | Mean | Allocated |
|--------|-----:|----------:|
| Small_ROM (~80B) | 68.8 ns | 0 B |
| Small_MultiSegment | 171.2 ns | 112 B |
| Header1K_ROM | 142.3 ns | 0 B |
| Header1K_MultiSegment | 311.1 ns | 1,056 B |
| Header4K_ROM | 415.2 ns | 0 B |
| Header4K_MultiSegment | 930.9 ns | 4,128 B |

## Validation Overhead

| Payload | HardenedParser (ROM) | FlexibleParser (ROM) | Overhead |
|---------|--------------------:|--------------------:|:--------:|
| ~80B    | 93.5 ns             | 68.8 ns             | 1.36x    |
| 1KB     | 213.0 ns            | 142.3 ns            | 1.50x    |
| 4KB     | 675.8 ns            | 415.2 ns            | 1.63x    |

## RequestSemantics

Post-parse semantic validation checks run against an already-parsed `BinaryRequest`. Header-scanning checks scale linearly with header count; path-scanning checks are constant-time since the path length is fixed across payload sizes.

### Header-scanning checks

| Method | Small (~80B) | 4KB | 32KB |
|--------|------------:|-----------:|-----------:|
| HasConflictingContentLength | 6.9 ns | 26.5 ns | 208.4 ns |
| HasTransferEncodingWithContentLength | 6.4 ns | 30.8 ns | 199.5 ns |
| HasInvalidHostHeaderCount | 2.0 ns | 20.2 ns | 154.4 ns |
| HasInvalidContentLengthFormat | 7.6 ns | 18.7 ns | 152.6 ns |
| HasContentLengthWithLeadingZeros | 7.6 ns | 18.9 ns | 154.7 ns |
| HasConflictingCommaSeparatedContentLength | 9.2 ns | 25.6 ns | 156.2 ns |
| HasInvalidTransferEncoding | 2.4 ns | 25.7 ns | 157.8 ns |

### Path-scanning checks

| Method | Small (~80B) | 4KB | 32KB |
|--------|------------:|-----------:|-----------:|
| HasDotSegments | 2.4 ns | 2.5 ns | 2.6 ns |
| HasFragmentInRequestTarget | 1.2 ns | 1.4 ns | 1.3 ns |
| HasBackslashInPath | 1.4 ns | 1.3 ns | 1.3 ns |
| HasDoubleEncoding | 1.2 ns | 1.2 ns | 1.2 ns |
| HasEncodedNullByte | 1.2 ns | 1.2 ns | 1.2 ns |
| HasOverlongUtf8 | 2.5 ns | 2.5 ns | 2.5 ns |

All checks are **zero-allocation**. Running all 13 checks on a typical small request adds ~60 ns total overhead.

## Key Takeaways

- **ROM path is always zero-allocation** — no GC pressure regardless of request size
- **Multi-segment linearization** provides ROM-speed parsing with a single upfront allocation
- **Incomplete input** (no `\r\n\r\n`) returns `false` with zero allocation
- **SIMD-accelerated validation** (`SearchValues<byte>`, `IndexOfAnyExcept`) keeps the hardened parser within 1.4-1.6x of the unvalidated parser

## Benchmark Trends

Interactive chart tracking performance across commits. Updated on each manual benchmark run on `main`.

<iframe src="/Glyph11/benchmarks/index.html" width="100%" height="600" frameborder="0" style="border: 1px solid #e5e7eb; border-radius: 8px;"></iframe>

## Running Benchmarks

```bash
cd src/Benchmarks
dotnet run -c Release
```
