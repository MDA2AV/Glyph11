---
title: Performance
weight: 6
---

For detailed benchmark results, allocation tracking, and trend charts, see the [Benchmarks](/Glyph11/benchmarks/) page.

## Key Characteristics

- **ROM path is always zero-allocation** — no GC pressure regardless of request size
- **Multi-segment linearization** provides ROM-speed parsing with a single upfront allocation
- **Incomplete input** (no `\r\n\r\n`) returns `false` with zero allocation
- **SIMD-accelerated validation** (`SearchValues<byte>`, `IndexOfAnyExcept`) keeps the HardenedParser within ~1.4-1.6x of the unvalidated FlexibleParser
- All `RequestSemantics` checks are **zero-allocation**

## Running Benchmarks

```bash
cd Benchmarks
dotnet run -c Release
```
