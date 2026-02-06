---
title: Performance
weight: 6
---

All benchmarks use 3 segments for multi-segment cases. ROM uses a single contiguous buffer. Multi-segment input is linearized (copied to a contiguous array) before parsing.

## Benchmark Results

| Method | Mean | Allocated |
|--------|-----:|----------:|
| Small_ROM (~80B) | 105 ns | 0 B |
| Small_MultiSegment | 345 ns | 112 B |
| Header1K_ROM | 497 ns | 0 B |
| Header1K_MultiSegment | 1,178 ns | 1,056 B |
| Header4K_ROM | 1,909 ns | 0 B |
| Header4K_MultiSegment | 3,900 ns | 4,128 B |
| Header16K_ROM | 6,972 ns | 0 B |
| Header16K_MultiSegment | 13,746 ns | 16,416 B |
| Header32K_ROM | 3,813 ns | 0 B |
| Header32K_MultiSegment | 17,705 ns | 32,808 B |

## Key Takeaways

- **ROM path is always zero-allocation** — no GC pressure regardless of request size
- **Multi-segment linearization** provides ROM-speed parsing with a single upfront allocation
- **Incomplete input** (no `\r\n\r\n`) returns `false` with zero allocation

## Running Benchmarks

```bash
cd src/Benchmarks
dotnet run -c Release
```
