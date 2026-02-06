---
title: BufferSegment
weight: 7
---

**Namespace:** `Glyph11.Utils`

```csharp
public sealed class BufferSegment : ReadOnlySequenceSegment<byte>
```

A helper for constructing multi-segment `ReadOnlySequence<byte>` buffers, primarily useful for testing.

## Constructors

| Constructor | Description |
|------------|-------------|
| `BufferSegment(ReadOnlyMemory<byte> memory)` | Creates the first segment with the given memory |

## Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Append(ReadOnlyMemory<byte> memory)` | `BufferSegment` | Appends a new segment and returns it |

## Declaration

```csharp
namespace Glyph11.Utils;

public sealed class BufferSegment : ReadOnlySequenceSegment<byte>
{
    public BufferSegment(ReadOnlyMemory<byte> memory);
    public BufferSegment Append(ReadOnlyMemory<byte> memory);
}
```

## Usage

```csharp
using Glyph11.Utils;

var first = new BufferSegment("GET / HTTP/1.1\r\n"u8.ToArray());
var last = first
    .Append("Host: localhost\r\n"u8.ToArray())
    .Append("\r\n"u8.ToArray());

var seq = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
```

This creates a `ReadOnlySequence<byte>` with three segments, simulating fragmented network input for testing the multi-segment parsing path.
