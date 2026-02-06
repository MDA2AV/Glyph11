---
title: RequestSemantics
weight: 6
---

**Namespace:** `Glyph11.Validation`

```csharp
public static class RequestSemantics
```

Static methods for post-parse semantic validation. All checks use case-insensitive ASCII comparison for header names. Each method returns `true` if the attack pattern is detected.

## Methods

### HasConflictingContentLength

```csharp
public static bool HasConflictingContentLength(BinaryRequest request)
```

Returns `true` if the request contains multiple `Content-Length` headers with different values. Request smuggling vector (RFC 9110 Section 8.6).

---

### HasTransferEncodingWithContentLength

```csharp
public static bool HasTransferEncodingWithContentLength(BinaryRequest request)
```

Returns `true` if the request contains both `Transfer-Encoding` and `Content-Length` headers. CL.TE / TE.CL smuggling vector (RFC 9112 Section 6.1).

---

### HasConflictingCommaSeparatedContentLength

```csharp
public static bool HasConflictingCommaSeparatedContentLength(BinaryRequest request)
```

Returns `true` if a single `Content-Length` header contains comma-separated values that are not all identical (e.g. `Content-Length: 42, 0`). Smuggling vector (RFC 9112 Section 6.2).

---

### HasInvalidContentLengthFormat

```csharp
public static bool HasInvalidContentLengthFormat(BinaryRequest request)
```

Returns `true` if any `Content-Length` value contains non-digit characters. Allows comma-separated form per RFC 9112 Section 6.2.

---

### HasContentLengthWithLeadingZeros

```csharp
public static bool HasContentLengthWithLeadingZeros(BinaryRequest request)
```

Returns `true` if any `Content-Length` value has leading zeros (e.g. `0200`). Prevents octal interpretation confusion.

---

### HasInvalidTransferEncoding

```csharp
public static bool HasInvalidTransferEncoding(BinaryRequest request)
```

Returns `true` if a `Transfer-Encoding` header value is not exactly `chunked` (case-insensitive, after OWS trimming). Detects TE.TE smuggling via obfuscated values.

---

### HasInvalidHostHeaderCount

```csharp
public static bool HasInvalidHostHeaderCount(BinaryRequest request)
```

Returns `true` if the request does not have exactly one `Host` header. RFC 9112 Section 3.2 requires exactly one.

---

### HasDotSegments

```csharp
public static bool HasDotSegments(BinaryRequest request)
```

Returns `true` if the request path contains `/../`, `/./`, trailing `/..`, or `/.` segments. Path traversal indicator.

---

### HasBackslashInPath

```csharp
public static bool HasBackslashInPath(BinaryRequest request)
```

Returns `true` if the request path contains backslash characters. Windows path traversal vector.

---

### HasDoubleEncoding

```csharp
public static bool HasDoubleEncoding(BinaryRequest request)
```

Returns `true` if the request path contains `%25` (double-encoded percent). Bypasses single-decode security filters.

---

### HasEncodedNullByte

```csharp
public static bool HasEncodedNullByte(BinaryRequest request)
```

Returns `true` if the request path contains `%00`. Null bytes cause path truncation in C-based file systems.

---

### HasOverlongUtf8

```csharp
public static bool HasOverlongUtf8(BinaryRequest request)
```

Returns `true` if the request path contains overlong UTF-8 sequences (`0xC0`, `0xC1` lead bytes, or `0xE0`/`0xF0` with insufficient continuation bytes). Bypasses ASCII path checks (RFC 3629 Section 3).

---

### HasFragmentInRequestTarget

```csharp
public static bool HasFragmentInRequestTarget(BinaryRequest request)
```

Returns `true` if the request path contains `#`. Fragments must not appear in HTTP request-targets (RFC 9112 Section 3.2).

## Declaration

```csharp
namespace Glyph11.Validation;

public static class RequestSemantics
{
    public static bool HasConflictingContentLength(BinaryRequest request);
    public static bool HasTransferEncodingWithContentLength(BinaryRequest request);
    public static bool HasConflictingCommaSeparatedContentLength(BinaryRequest request);
    public static bool HasInvalidContentLengthFormat(BinaryRequest request);
    public static bool HasContentLengthWithLeadingZeros(BinaryRequest request);
    public static bool HasInvalidTransferEncoding(BinaryRequest request);
    public static bool HasInvalidHostHeaderCount(BinaryRequest request);
    public static bool HasDotSegments(BinaryRequest request);
    public static bool HasBackslashInPath(BinaryRequest request);
    public static bool HasDoubleEncoding(BinaryRequest request);
    public static bool HasEncodedNullByte(BinaryRequest request);
    public static bool HasOverlongUtf8(BinaryRequest request);
    public static bool HasFragmentInRequestTarget(BinaryRequest request);
}
```
