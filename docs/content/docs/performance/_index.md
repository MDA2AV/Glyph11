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

## Key Takeaways

- **ROM path is always zero-allocation** — no GC pressure regardless of request size
- **Multi-segment linearization** provides ROM-speed parsing with a single upfront allocation
- **Incomplete input** (no `\r\n\r\n`) returns `false` with zero allocation
- **SIMD-accelerated validation** (`SearchValues<byte>`, `IndexOfAnyExcept`) keeps the hardened parser within 1.4-1.6x of the unvalidated parser

## Running Benchmarks

```bash
cd src/Benchmarks
dotnet run -c Release
```
