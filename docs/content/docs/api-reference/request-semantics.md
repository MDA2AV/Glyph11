---
title: RequestSemantics
weight: 6
---

**Namespace:** `Glyph11.Validation`

```csharp
public static class RequestSemantics
```

Static methods for post-parse semantic validation. All checks use case-insensitive ASCII comparison for header names.

## Methods

### HasConflictingContentLength

```csharp
public static bool HasConflictingContentLength(BinaryRequest request)
```

Returns `true` if the request contains multiple `Content-Length` headers with different values. This is a request smuggling vector.

---

### HasTransferEncodingWithContentLength

```csharp
public static bool HasTransferEncodingWithContentLength(BinaryRequest request)
```

Returns `true` if the request contains both `Transfer-Encoding` and `Content-Length` headers. Per RFC 9112 Section 6.1, this combination is a smuggling vector.

---

### HasDotSegments

```csharp
public static bool HasDotSegments(BinaryRequest request)
```

Returns `true` if the request path contains `/../` or `/./` segments, indicating a path traversal attempt.

## Declaration

```csharp
namespace Glyph11.Validation;

public static class RequestSemantics
{
    public static bool HasConflictingContentLength(BinaryRequest request);
    public static bool HasTransferEncodingWithContentLength(BinaryRequest request);
    public static bool HasDotSegments(BinaryRequest request);
}
```
