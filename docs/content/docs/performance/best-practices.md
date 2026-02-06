---
title: Best Practices
weight: 1
---

Tips for getting the best performance out of Glyph11.

## Reuse BinaryRequest

Create one `BinaryRequest` per connection and reuse it across requests. Call `Clear()` on `Headers` and `QueryParameters` between requests instead of allocating a new instance.

```csharp
private readonly BinaryRequest _request = new();

private void ResetForNextRequest()
{
    _request.Headers.Clear();
    _request.QueryParameters.Clear();
}
```

This avoids allocating new `KeyValueList` instances and their pooled backing arrays on every request.

## Prefer Single-Segment Input

The ROM path is zero-allocation. If your network stack can provide data as a single contiguous buffer, you avoid the linearization allocation entirely.

With `PipeReader`, the first read often returns a single segment if the full header arrived in one network read. Glyph11 detects this automatically via `IsSingleSegment`.

## Buffer Lifetime Awareness

Parsed fields are `ReadOnlyMemory<byte>` slices into the original input buffer. This means:

- **Do not** access request fields after advancing the `PipeReader` past the consumed data
- **Do** copy any data you need to keep with `ToArray()` before advancing
- **Do** process and extract needed values before calling `reader.AdvanceTo()`

```csharp
// Process BEFORE advancing
var method = request.Method.ToArray(); // copy if needed later

// Now safe to advance
reader.AdvanceTo(buffer.Start, buffer.End);
```

## Minimize Post-Parse Copies

If you only need to compare parsed values (e.g. checking if the method is GET), use span comparison instead of converting to strings:

```csharp
// Efficient: span comparison, no allocation
if (request.Method.Span.SequenceEqual("GET"u8))
{
    // handle GET
}

// Avoid: string conversion allocates
// string method = Encoding.ASCII.GetString(request.Method.Span);
```

## Pool at the Application Level

For high-throughput servers, consider pooling `BinaryRequest` instances at the application level using `ObjectPool<T>` or a similar pattern:

```csharp
var pool = new DefaultObjectPool<BinaryRequest>(
    new DefaultPooledObjectPolicy<BinaryRequest>());

var request = pool.Get();
try
{
    // parse and handle
}
finally
{
    request.Headers.Clear();
    request.QueryParameters.Clear();
    pool.Return(request);
}
```

## Tune ParserLimits

Use tighter limits when you know your traffic patterns. Smaller limits mean faster rejection of oversized or malicious requests:

```csharp
// API that only accepts small requests
var limits = ParserLimits.Default with
{
    MaxHeaderCount = 20,
    MaxUrlLength = 2048,
    MaxTotalHeaderBytes = 8192
};
```
