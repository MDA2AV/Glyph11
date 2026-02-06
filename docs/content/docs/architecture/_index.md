---
title: Architecture
weight: 2
---

Glyph11's parser has two execution paths, automatically dispatched based on the memory layout of the input buffer.

## Parsing Paths

```
                      ┌─────────────────────────┐
                      │   ReadOnlySequence<byte> │
                      │      (from network)      │
                      └────────────┬────────────┘
                                   │
                      ┌────────────▼────────────┐
                      │   TryExtractFullHeader   │
                      │     (entry point)        │
                      └────────────┬────────────┘
                                   │
                 ┌─────────────────┴─────────────────┐
                 │ IsSingleSegment?                   │
                 ▼                                    ▼
          ┌─────────────┐                    ┌──────────────────┐
          │  ROM Path   │                    │  Linearize Path  │
          │ (zero-copy) │                    │ ToArray() → ROM  │
          └─────────────┘                    └──────────────────┘
```

### ROM Path (ReadOnlyMemory)

The zero-allocation hot path for single-segment buffers. All parsed fields are `ReadOnlyMemory<byte>` slices into the original buffer — no copies are made.

This path is automatically selected when the input `ReadOnlySequence<byte>` consists of a single contiguous memory segment (`IsSingleSegment == true`), which is the common case when the full request arrives in one read.

### Linearize Path

For multi-segment input (common with `PipeReader` when data arrives across multiple reads), the buffer is:

1. Checked for completeness (`\r\n\r\n` presence) using `SequenceReader` — returns `false` with zero allocation if incomplete
2. Copied into a single contiguous byte array via `ToArray()`
3. Parsed using the ROM path for maximum speed

This approach trades one upfront allocation for significantly faster parsing and fewer total allocations compared to traversing segments individually.

## Why Two Paths?

The ROM path achieves zero allocation by slicing directly into the caller's buffer. But `ReadOnlySequence<byte>` can span multiple non-contiguous memory segments, which makes direct slicing impossible.

Rather than implementing a slower segment-traversing parser, Glyph11 linearizes multi-segment input into a contiguous array first, then uses the same fast ROM parser. Benchmarks show this is consistently faster and produces fewer total allocations.
