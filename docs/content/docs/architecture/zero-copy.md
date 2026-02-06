---
title: Zero-Copy Parsing
weight: 1
---

The ROM (ReadOnlyMemory) path is the core of Glyph11's performance. It parses HTTP headers without allocating any heap memory by slicing directly into the caller's buffer.

## How It Works

When input arrives as a single contiguous segment, every parsed field — method, path, version, header names, header values, query parameters — is represented as a `ReadOnlyMemory<byte>` slice pointing into the original input buffer.

```
Input buffer (owned by caller):
┌──────────────────────────────────────────────────────┐
│ G E T   / a p i   H T T P / 1 . 1 \r\n H o s t ... │
└──────────────────────────────────────────────────────┘
  ▲         ▲         ▲
  │         │         │
  Method    Path      Version
  [0..3]    [4..8]    [9..17]
```

No `string` objects are created. No byte arrays are copied. The parser simply calculates offsets and lengths, then constructs `ReadOnlyMemory<byte>` slices.

## Buffer Lifetime

{{< callout type="warning" >}}
Because parsed fields are slices into the original buffer, the buffer must remain valid for the entire time you access request data. If you need the data to outlive the buffer, copy it explicitly.
{{< /callout >}}

```csharp
// Safe: buffer is still valid
var method = request.Method; // slice into buffer

// Copy if you need to keep it after buffer is released
byte[] methodCopy = request.Method.ToArray();
```

## Performance Impact

The zero-copy approach means the ROM path produces **0 bytes** of heap allocation regardless of request size. This eliminates GC pressure entirely for the common single-segment case.

| Scenario | Allocations |
|----------|------------|
| ROM path (any size) | 0 B |
| Multi-segment (linearized) | 1 array allocation |
