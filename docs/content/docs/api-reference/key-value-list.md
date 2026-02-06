---
title: KeyValueList
weight: 5
---

**Namespace:** `Glyph11.Protocol`

```csharp
public sealed class KeyValueList : IDisposable
```

A pooled list of `KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>` used for headers and query parameters.

## Constructors

| Constructor | Description |
|------------|-------------|
| `KeyValueList(int initialCapacity = 16)` | Creates a new list with the specified initial capacity, backed by `ArrayPool<T>` |

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Count` | `int` | Number of key-value pairs in the list |
| `this[int index]` | `KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>` | Gets the pair at the specified index |

## Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `AsSpan()` | `ReadOnlySpan<KeyValuePair<...>>` | Returns a span over the populated entries |
| `Dispose()` | `void` | Returns pooled arrays to `ArrayPool<T>` |

## Declaration

```csharp
namespace Glyph11.Protocol;

public sealed class KeyValueList : IDisposable
{
    public KeyValueList(int initialCapacity = 16);

    public int Count { get; }
    public KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>> this[int index] { get; }

    public ReadOnlySpan<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>> AsSpan();
    public void Dispose();
}
```

## Usage

```csharp
var headers = request.Headers;

// Iterate by index
for (int i = 0; i < headers.Count; i++)
{
    var kv = headers[i];
    ReadOnlyMemory<byte> name = kv.Key;
    ReadOnlyMemory<byte> value = kv.Value;
}

// Or use AsSpan() for span-based iteration
foreach (var kv in headers.AsSpan())
{
    // ...
}
```

## Pooling

`KeyValueList` uses `ArrayPool<T>` internally with an initial capacity of 16. Call `BinaryRequest.Dispose()` when done to return pooled arrays. For request-per-connection reuse, call `KeyValueList.Clear()` between requests.
