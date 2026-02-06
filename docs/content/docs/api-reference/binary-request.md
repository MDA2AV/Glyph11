---
title: BinaryRequest
weight: 4
---

**Namespace:** `Glyph11.Protocol`

```csharp
public class BinaryRequest : IDisposable
```

The core data structure populated by the parser. All fields are `ReadOnlyMemory<byte>` slices referencing the original input buffer (zero-copy on the ROM path).

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Version` | `ReadOnlyMemory<byte>` | HTTP version (e.g. `HTTP/1.1`) |
| `Method` | `ReadOnlyMemory<byte>` | HTTP method (e.g. `GET`, `POST`) |
| `Path` | `ReadOnlyMemory<byte>` | Request path (e.g. `/api/users`) |
| `Body` | `ReadOnlyMemory<byte>` | Request body (if present) |
| `QueryParameters` | `KeyValueList` | Parsed query string parameters |
| `Headers` | `KeyValueList` | Parsed HTTP headers |

## Methods

| Method | Description |
|--------|-------------|
| `Dispose()` | Returns pooled arrays to `ArrayPool<T>` |

## Declaration

```csharp
namespace Glyph11.Protocol;

public class BinaryRequest : IDisposable
{
    public ReadOnlyMemory<byte> Version { get; }
    public ReadOnlyMemory<byte> Method { get; }
    public ReadOnlyMemory<byte> Path { get; }
    public ReadOnlyMemory<byte> Body { get; }
    public KeyValueList QueryParameters { get; }
    public KeyValueList Headers { get; }

    public void Dispose();
}
```

{{< callout type="warning" >}}
Since parsed fields reference the input buffer, the buffer must remain valid for as long as you access the request data. If you need the data to outlive the buffer, copy it (e.g. `request.Method.ToArray()`).
{{< /callout >}}

## Reuse Pattern

For request-per-connection reuse, call `Clear()` on `Headers` and `QueryParameters` between requests instead of creating a new instance:

```csharp
private readonly BinaryRequest _request = new();

private void ResetForNextRequest()
{
    _request.Headers.Clear();
    _request.QueryParameters.Clear();
}
```
