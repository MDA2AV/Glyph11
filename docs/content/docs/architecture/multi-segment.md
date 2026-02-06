---
title: Multi-Segment Handling
weight: 2
---

When input arrives as multiple `ReadOnlySequence<byte>` segments (common with `PipeReader`), the entry point automatically linearizes the buffer before parsing.

## Linearization Strategy

The multi-segment path follows three steps:

1. **Completeness check** — Uses `SequenceReader` to scan for `\r\n\r\n`. Returns `false` with zero allocation if the header is incomplete.
2. **Linearize** — Calls `ToArray()` to copy the segments into a single contiguous byte array.
3. **Parse** — Passes the contiguous array to the ROM parser.

```
Segment 1          Segment 2          Segment 3
┌──────────┐       ┌──────────┐       ┌──────────┐
│ GET / HT │  ──►  │ TP/1.1\r │  ──►  │ \n\r\n   │
└──────────┘       └──────────┘       └──────────┘
      │                  │                  │
      └──────────────────┴──────────────────┘
                         │
                    ToArray()
                         │
                         ▼
              ┌──────────────────────┐
              │ GET / HTTP/1.1\r\n\r\n│
              └──────────────────────┘
                         │
                    ROM Parser
```

## Why Linearize?

An alternative approach would be to traverse segments individually during parsing. Benchmarks show that the linearize-then-parse approach is consistently faster because:

- The ROM parser operates on contiguous memory with simple pointer arithmetic
- Segment traversal requires branch-heavy logic at every byte boundary
- One `ToArray()` call is cheaper than many small allocations from segment-aware parsing

## Allocation Cost

The only allocation is the `ToArray()` call, which produces a single byte array sized to the total header length:

| Header Size | Allocation |
|------------|-----------|
| ~80 B | 112 B |
| ~1 KB | 1,056 B |
| ~4 KB | 4,128 B |
| ~16 KB | 16,416 B |

## Building Test Buffers

Use `BufferSegment` to construct multi-segment sequences for testing:

```csharp
using Glyph11.Utils;

var first = new BufferSegment("GET / HTTP/1.1\r\n"u8.ToArray());
var last = first
    .Append("Host: localhost\r\n"u8.ToArray())
    .Append("\r\n"u8.ToArray());

var seq = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
```
