# Glyph11 Security Model

Glyph11 provides two layers of defense against HTTP/1.1 protocol attacks:

1. **Parse-time validation** (`HardenedParser`) — rejects malformed input during parsing
2. **Post-parse validation** (`RequestSemantics`) — detects semantically dangerous patterns after parsing

---

## Parse-Time Protections (HardenedParser)

These checks are enforced automatically during `TryExtractFullHeader` / `TryExtractFullHeaderROM`. Violations throw `InvalidOperationException`.

### Bare LF Rejection

**Attack:** HTTP request smuggling via bare line feed (0x0A without preceding 0x0D). Some parsers accept bare LF as a line terminator while others require CRLF, creating parsing discrepancies.

**RFC:** 9112 Section 2.2 — "A recipient of such a bare CR MUST consider that element to be invalid."

**Protection:** The parser scans the entire header section for any bare LF (0x0A not preceded by 0x0D) and rejects the request.

**CVEs prevented:** CVE-2023-30589 (Node.js), CVE-2025-58056 (Netty), CVE-2019-16785 (Waitress).

### Obsolete Line Folding (obs-fold) Rejection

**Attack:** Header value continuation lines (starting with SP or HTAB) can obfuscate Transfer-Encoding headers to create smuggling vectors.

```
Transfer-Encoding: chunked\r\n
 Garbage\r\n
```

**RFC:** 9112 Section 5.2 — "A server that receives an obs-fold in a request message MUST either reject the message... or replace each received obs-fold."

**Protection:** Any header line starting with SP (0x20) or HTAB (0x09) is rejected.

### Whitespace Before Colon Rejection

**Attack:** `Content-Length : 0` (space before colon) may be accepted by some parsers but rejected by others, enabling TE.TE smuggling.

**RFC:** 9112 Section 5.1 — "No whitespace is allowed between the field name and colon."

**Protection:** The parser checks the byte immediately before the colon in each header line and rejects if it is SP or HTAB.

### Multiple Spaces in Request Line

**Attack:** Multiple spaces between request-line components (`GET  /path HTTP/1.1`) create parsing ambiguity that can lead to request smuggling.

**RFC:** 9112 Section 3 — `request-line = method SP request-target SP HTTP-version`

**Protection:** After finding the space delimiters, the parser verifies no additional spaces follow immediately.

### Request-Target Control Character Rejection

**Attack:** Control characters (0x00-0x1F, 0x7F) in the URL enable null-byte path truncation and injection.

**RFC:** 9112 Section 3.2 — request-target must contain only valid URI characters.

**Protection:** Every byte of the request-target is checked; any control character causes rejection.

### Method Token Validation

**Attack:** Invalid characters in HTTP methods can confuse downstream processing.

**RFC:** 9110 Section 5.6.2 — method is a `token` (`!#$%&'*+-.^_`|~ DIGIT ALPHA`).

**Protection:** Every byte of the method is validated against the RFC 9110 token character set.

### Header Name Token Validation

**Attack:** Control characters or invalid bytes in header names create parsing discrepancies between systems.

**RFC:** 9110 Section 5.1 — field-name is a `token`.

**Protection:** Every byte of each header name is validated against the token character set.

### Header Value Character Validation

**Attack:** CRLF injection, null byte injection, and control character injection in header values.

**RFC:** 9110 Section 5.5 — field-value allows only HTAB (0x09), SP (0x20), VCHAR (0x21-0x7E), and obs-text (0x80-0xFF).

**Protection:** Every byte of each header value is validated against the allowed character set. CR, LF, NUL, and all other control characters are rejected.

**CVEs prevented:** CVE-2024-52875, CVE-2024-20337.

### Empty Header Name Rejection

**Attack:** A header line starting with `:` (empty name) causes undefined behavior in different parsers.

**RFC:** 9110 Section 5.1 — field-name requires at least one token character.

**Protection:** Header lines where the colon is at position 0 are rejected.

### Missing Colon Rejection

**Attack:** Header lines without a colon separator may be silently dropped by lenient parsers, creating discrepancies.

**Protection:** Any header line that does not contain a colon is rejected with an exception (not silently skipped).

### HTTP Version Validation

**Attack:** Invalid version strings enable protocol downgrade attacks and undefined behavior.

**RFC:** 9112 Section 2.6 — `HTTP-version = "HTTP/" DIGIT "." DIGIT`

**Protection:** The version string must be exactly 8 bytes matching `HTTP/X.Y` where X and Y are ASCII digits.

### Resource Limits (DoS Prevention)

All limits are configurable via `ParserLimits`:

| Limit | Default | Attack Prevented |
|-------|---------|------------------|
| `MaxHeaderCount` | 100 | Header flooding |
| `MaxHeaderNameLength` | 256 | Oversized header names |
| `MaxHeaderValueLength` | 8192 | Oversized header values |
| `MaxUrlLength` | 8192 | URL buffer overflow |
| `MaxQueryParameterCount` | 128 | Query parameter flooding |
| `MaxMethodLength` | 16 | Oversized method strings |
| `MaxTotalHeaderBytes` | 32768 | Total header section DoS |

---

## Post-Parse Protections (RequestSemantics)

These checks are called explicitly after successful parsing. Each returns `true` if the attack pattern is detected.

### Request Smuggling

#### `HasTransferEncodingWithContentLength`

**Attack:** CL.TE / TE.CL request smuggling. When both `Transfer-Encoding` and `Content-Length` are present, front-end and back-end may disagree on message body boundaries.

**RFC:** 9112 Section 6.1

#### `HasConflictingContentLength`

**Attack:** Multiple `Content-Length` headers with different values. One system uses the first value, another uses the last.

**RFC:** 9110 Section 8.6

#### `HasConflictingCommaSeparatedContentLength`

**Attack:** A single `Content-Length` header with comma-separated values that differ (e.g. `Content-Length: 42, 0`).

**RFC:** 9112 Section 6.2

#### `HasInvalidContentLengthFormat`

**Attack:** Non-digit characters in Content-Length values (`Content-Length: abc`, `Content-Length: 1 2`, `Content-Length: 1e5`). Different parsers interpret these differently.

**RFC:** 9110 Section 8.6 — Content-Length is `1*DIGIT`.

**CVEs prevented:** CVE-2018-7159 (Node.js).

#### `HasContentLengthWithLeadingZeros`

**Attack:** `Content-Length: 0200` may be interpreted as decimal 200 by one parser but octal 128 by another.

**RFC:** 9110 Section 8.6

#### `HasInvalidTransferEncoding`

**Attack:** TE.TE smuggling via obfuscated Transfer-Encoding values (`xchunked`, `"chunked"`, `chunked-thing`). One system recognizes it as chunked, another does not.

**RFC:** 9112 Section 6.1

### Host Header Attacks

#### `HasInvalidHostHeaderCount`

**Attack:** Missing Host header (routing confusion) or multiple Host headers (routing disagreement between front-end and back-end, SSRF).

**RFC:** 9112 Section 3.2 — exactly one Host header required for HTTP/1.1.

### Path Traversal

#### `HasDotSegments`

**Attack:** `/../` and `/./` sequences in the path allow directory traversal to access files outside the intended root.

**RFC:** 3986 Section 5.2.4

#### `HasBackslashInPath`

**Attack:** Backslash characters (`\`) are treated as path separators on Windows, enabling traversal via `\..\`.

#### `HasDoubleEncoding`

**Attack:** `%252e%252e` decodes to `%2e%2e` after one pass, then `..` after a second. Bypasses single-decode security filters.

#### `HasEncodedNullByte`

**Attack:** `%00` in the path causes C-based file systems to truncate the path at the null byte. `file.txt%00.jpg` passes extension checks but opens `file.txt`.

#### `HasOverlongUtf8`

**Attack:** Overlong UTF-8 sequences encode ASCII characters (like `/` as `0xC0 0xAF`) to bypass ASCII-only path checks.

**RFC:** 3629 Section 3 — overlong sequences are forbidden.

### Fragment Rejection

#### `HasFragmentInRequestTarget`

**Attack:** Fragment identifiers (`#`) must not appear in HTTP request-targets. Their presence indicates injection or malformed input.

**RFC:** 9112 Section 3.2

---

## Usage

```csharp
using Glyph11.Parser.Hardened;
using Glyph11.Validation;

var limits = ParserLimits.Default;

if (HardenedParser.TryExtractFullHeader(ref buffer, request, in limits, out var bytesRead))
{
    // Parse-time checks already passed (method, headers, version, limits, etc.)
    // Now run post-parse semantic checks:

    if (RequestSemantics.HasInvalidHostHeaderCount(request))
        throw new InvalidOperationException("Invalid Host header count.");

    if (RequestSemantics.HasTransferEncodingWithContentLength(request))
        throw new InvalidOperationException("Request smuggling: TE + CL.");

    if (RequestSemantics.HasConflictingContentLength(request))
        throw new InvalidOperationException("Conflicting Content-Length values.");

    if (RequestSemantics.HasConflictingCommaSeparatedContentLength(request))
        throw new InvalidOperationException("Conflicting comma-separated Content-Length.");

    if (RequestSemantics.HasInvalidContentLengthFormat(request))
        throw new InvalidOperationException("Invalid Content-Length format.");

    if (RequestSemantics.HasContentLengthWithLeadingZeros(request))
        throw new InvalidOperationException("Content-Length has leading zeros.");

    if (RequestSemantics.HasInvalidTransferEncoding(request))
        throw new InvalidOperationException("Invalid Transfer-Encoding value.");

    if (RequestSemantics.HasDotSegments(request))
        throw new InvalidOperationException("Path traversal detected.");

    if (RequestSemantics.HasBackslashInPath(request))
        throw new InvalidOperationException("Backslash in path.");

    if (RequestSemantics.HasDoubleEncoding(request))
        throw new InvalidOperationException("Double encoding detected.");

    if (RequestSemantics.HasEncodedNullByte(request))
        throw new InvalidOperationException("Encoded null byte in path.");

    if (RequestSemantics.HasOverlongUtf8(request))
        throw new InvalidOperationException("Overlong UTF-8 in path.");

    if (RequestSemantics.HasFragmentInRequestTarget(request))
        throw new InvalidOperationException("Fragment in request-target.");

    // Safe to process request
}
```

---

## Attack Categories Covered

| Category | Parse-Time | Post-Parse |
|----------|:----------:|:----------:|
| HTTP Request Smuggling (CL.TE, TE.CL, TE.TE) | | X |
| CRLF / Header Injection | X | |
| Bare LF Smuggling | X | |
| Obs-fold Smuggling | X | |
| Header Name Injection | X | |
| Header Value Injection | X | |
| Content-Length Manipulation | | X |
| Transfer-Encoding Obfuscation | | X |
| Host Header Attacks | | X |
| Path Traversal | | X |
| Null Byte Injection | X | X |
| Double Encoding Bypass | | X |
| Overlong UTF-8 Bypass | | X |
| Backslash Traversal | | X |
| Fragment Injection | | X |
| Resource Exhaustion (DoS) | X | |
| HTTP Version Manipulation | X | |
| Request Line Injection | X | |

---

## References

- [RFC 9110 — HTTP Semantics](https://httpwg.org/specs/rfc9110.html)
- [RFC 9112 — HTTP/1.1](https://httpwg.org/specs/rfc9112.html)
- [RFC 3986 — URI Syntax](https://www.rfc-editor.org/rfc/rfc3986)
- [RFC 3629 — UTF-8](https://www.rfc-editor.org/rfc/rfc3629)
- [PortSwigger — HTTP Request Smuggling](https://portswigger.net/web-security/request-smuggling)
- [CWE-113 — HTTP Request/Response Splitting](https://cwe.mitre.org/data/definitions/113.html)
- [CWE-444 — Inconsistent Interpretation of HTTP Requests](https://cwe.mitre.org/data/definitions/444.html)
