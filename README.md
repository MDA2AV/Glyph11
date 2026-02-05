# Glyph11

Glyph11 is a dependency free, low allocation HTTP/1.1 parser for C#. It does not rely on any specific network technology but can be used with any (such as `Socket`, `NetworkStream`, `PipeReader` or anything else).

[![NuGet](https://img.shields.io/nuget/v/Glyph11.svg)](https://www.nuget.org/packages/Glyph11/)

## Usage

> [!IMPORTANT]
> This is an early stage development project which currently lacks conformity tests.

```csharp
ReadOnlySequence<byte> buffer = ...; // e.g. read from pipe reader

IBinaryRequest request = ...; // a re-usable/poolable request implementation to parse into

if (HardenedParser.TryExtractFullHeader(ref buffer, request, in limits, out var bytesRead))
{
    // handle the request and access request.Path, .Body etc.
    // advance the reader by bytesRead
}
```

## Performance

 Method                       | Mean       | Error    | StdDev   | Gen0   | Allocated |
----------------------------- |-----------:|---------:|---------:|-------:|----------:|
 Request in Single Segment |   125.4 ns | 38.41 ns | 35.93 ns |      - |         - |
 Request in Multiple Segments  | 1,043.6 ns | 19.78 ns | 17.53 ns | 0.0725 |     456 B |

 # Glyph11 Documentation

 Glyph11 is a dependency-free, low-allocation HTTP/1.1 header parser for C#. It operates on `ReadOnlyMemory<byte>` and `ReadOnlySequence<byte>`, making it compatible with any network stack (`Socket`, `NetworkStream`, `PipeReader`, etc.).

 ## Table of Contents

 - [Installation](#installation)
 - [Quick Start](#quick-start)
 - [Architecture](#architecture)
 - [HardenedParser](#hardenedparser)
   - [Usage](#usage)
   - [ParserLimits](#parserlimits)
   - [Validation](#validation)
   - [Multi-Segment Handling](#multi-segment-handling)
 - [Post-Parse Validation](#post-parse-validation)
 - [Protocol Types](#protocol-types)
   - [BinaryRequest](#binaryrequest)
   - [KeyValueList](#keyvaluelist)
 - [Utilities](#utilities)
 - [Integration Example](#integration-example)
 - [Benchmarks](#benchmarks)
 - [API Reference](#api-reference)

 ---

 ## Installation

 ```
 dotnet add package Glyph11
 ```

 Targets .NET 10.0. No external dependencies.

 ---

 ## Quick Start

 ```csharp
 using System.Buffers;
 using Glyph11.Protocol;
 using Glyph11.Parser.Hardened;

 ReadOnlySequence<byte> buffer = ...; // from PipeReader, Socket, etc.

 var request = new BinaryRequest();
 var limits = ParserLimits.Default;

 if (HardenedParser.TryExtractFullHeader(ref buffer, request, in limits, out int bytesRead))
 {
     // request.Method, request.Path, request.Headers, request.QueryParameters
     // are all populated as ReadOnlyMemory<byte> slices into the original buffer.
     // Advance your reader by bytesRead.
 }

 // When done, dispose to return pooled arrays:
 request.Dispose();
 ```

 ---

 ## Architecture

 The parser has two execution paths, automatically dispatched based on input layout:

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

 **ROM path** (ReadOnlyMemory): Zero-allocation hot path for single-segment buffers. All parsed fields are `ReadOnlyMemory<byte>` slices into the original buffer — no copies.

 **Linearize path**: For multi-segment input, the buffer is first checked for completeness (`\r\n\r\n` presence), then copied into a single contiguous array via `ToArray()`, and parsed using the ROM path. This is significantly faster and produces fewer allocations than traversing segments individually.

 ---

 ## HardenedParser

 **Namespace:** `Glyph11.Parser.Hardened`

 A security-hardened HTTP/1.1 header parser with RFC 9110/9112 validation and configurable resource limits.

 ### Usage

 ```csharp
 using Glyph11.Parser.Hardened;

 var limits = ParserLimits.Default;

 // Entry point — auto-dispatches based on segment layout
 bool ok = HardenedParser.TryExtractFullHeader(
     ref buffer,       // ReadOnlySequence<byte>
     request,          // BinaryRequest
     in limits,        // ParserLimits
     out int bytesRead
 );

 // Direct ROM access (single contiguous buffer)
 ReadOnlyMemory<byte> mem = ...;
 bool ok = HardenedParser.TryExtractFullHeaderROM(
     ref mem, request, in limits, out int bytesRead
 );
 ```

 **Return values:**
 - Returns `false` if the header is incomplete (no `\r\n\r\n` terminator found). This is not an error — the caller should wait for more data.
 - Returns `true` when a complete header has been parsed. `bytesReadCount` indicates how many bytes were consumed.
 - Throws `InvalidOperationException` with a descriptive message for any protocol violation.

 ### ParserLimits

 All limits are configurable via `with` expressions on the `record struct`:

 ```csharp
 public readonly record struct ParserLimits
 {
     public int MaxHeaderCount           { get; init; }  // default: 100
     public int MaxHeaderNameLength      { get; init; }  // default: 256
     public int MaxHeaderValueLength     { get; init; }  // default: 8192
     public int MaxUrlLength             { get; init; }  // default: 8192
     public int MaxQueryParameterCount   { get; init; }  // default: 128
     public int MaxMethodLength          { get; init; }  // default: 16
     public int MaxTotalHeaderBytes      { get; init; }  // default: 32768

     public static ParserLimits Default { get; }
 }
 ```

 Customize limits for your use case:

 ```csharp
 var strict = ParserLimits.Default with
 {
     MaxHeaderCount = 50,
     MaxTotalHeaderBytes = 16384
 };

 HardenedParser.TryExtractFullHeader(ref buffer, request, in strict, out bytesRead);
 ```

 ### Validation

 The HardenedParser enforces the following on every request:

 **Request line:**
 - Method must contain only valid RFC 9110 Section 5.6.2 token characters (`A-Z`, `a-z`, `0-9`, `!#$%&'*+-.^_`|~`)
 - Method length must not exceed `MaxMethodLength`
 - URL length must not exceed `MaxUrlLength`
 - HTTP version must match the format `HTTP/X.Y` (exactly 8 bytes, digits at positions 5 and 7)
 - Query parameter count must not exceed `MaxQueryParameterCount`

 **Headers:**
 - Header name must contain only valid token characters
 - Header name must not be empty and must not exceed `MaxHeaderNameLength`
 - Header value must contain only valid field-value characters (RFC 9110 Section 5.5: HTAB, SP, VCHAR, obs-text)
 - Header value length must not exceed `MaxHeaderValueLength`
 - Total header count must not exceed `MaxHeaderCount`
 - Total header bytes (request line + all headers + terminators) must not exceed `MaxTotalHeaderBytes`
 - Lines without a colon separator are rejected (throws, not silently skipped)

 **HTTP version caching:** Common versions (`HTTP/1.1`, `HTTP/1.0`) are cached as static byte arrays to avoid per-request allocation.

 ### Multi-Segment Handling

 When input arrives as multiple `ReadOnlySequence<byte>` segments (common with `PipeReader`), the entry point automatically linearizes the buffer before parsing:

 1. Checks for `\r\n\r\n` presence using `SequenceReader` — returns `false` with zero allocation if incomplete
 2. Calls `ToArray()` to produce a single contiguous byte array
 3. Parses using the ROM path for maximum speed

 This approach trades one upfront allocation for significantly faster parsing and fewer total allocations compared to traversing segments individually.

 ---

 ## Post-Parse Validation

 **Namespace:** `Glyph11.Validation`

 After parsing, you can run semantic checks to detect protocol-level attacks:

 ```csharp
 using Glyph11.Validation;

 // Detect conflicting Content-Length values (request smuggling vector)
 if (RequestSemantics.HasConflictingContentLength(request))
     // reject: multiple Content-Length headers with different values

 // Detect Transfer-Encoding + Content-Length (request smuggling vector)
 if (RequestSemantics.HasTransferEncodingWithContentLength(request))
     // reject: RFC 9112 Section 6.1 violation

 // Detect path traversal attempts
 if (RequestSemantics.HasDotSegments(request))
     // reject: /../ or /./ segments in path
 ```

 All checks use case-insensitive ASCII comparison for header names.

 ---

 ## Protocol Types

 ### BinaryRequest

 The core data structure populated by the parser. All fields are `ReadOnlyMemory<byte>` slices referencing the original input buffer (zero-copy on the ROM path).

 ```csharp
 public class BinaryRequest : IDisposable
 {
     public ReadOnlyMemory<byte> Version { get; }
     public ReadOnlyMemory<byte> Method { get; }
     public ReadOnlyMemory<byte> Path { get; }
     public ReadOnlyMemory<byte> Body { get; }
     public KeyValueList QueryParameters { get; }
     public KeyValueList Headers { get; }
 }
 ```

 **Important:** Since parsed fields reference the input buffer, the buffer must remain valid for as long as you access the request data. If you need the data to outlive the buffer, copy it (e.g. `request.Method.ToArray()`).

 ### KeyValueList

 A pooled list of `KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>` used for headers and query parameters.

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

 `KeyValueList` uses `ArrayPool<T>` internally with an initial capacity of 16. Call `BinaryRequest.Dispose()` when done to return pooled arrays. For request-per-connection reuse, call `KeyValueList.Clear()` between requests.

 ---

 ## Utilities

 ### BufferSegment

 A helper for constructing multi-segment `ReadOnlySequence<byte>` buffers, useful for testing:

 ```csharp
 using Glyph11.Utils;

 var first = new BufferSegment("GET / HTTP/1.1\r\n"u8.ToArray());
 var last = first
     .Append("Host: localhost\r\n"u8.ToArray())
     .Append("\r\n"u8.ToArray());

 var seq = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
 ```

 ---

 ## Integration Example

 Here's a complete integration pattern with `PipeReader`:

 ```csharp
 using System.Buffers;
 using System.IO.Pipelines;
 using Glyph11.Protocol;
 using Glyph11.Parser.Hardened;
 using Glyph11.Validation;

 public class RequestHandler
 {
     private readonly BinaryRequest _request = new();
     private static readonly ParserLimits Limits = ParserLimits.Default;

     public async Task ProcessRequests(PipeReader reader)
     {
         while (true)
         {
             ReadResult result = await reader.ReadAsync();
             ReadOnlySequence<byte> buffer = result.Buffer;

             while (TryParseRequest(ref buffer))
             {
                 HandleRequest();
                 ResetForNextRequest();
             }

             reader.AdvanceTo(buffer.Start, buffer.End);

             if (result.IsCompleted) break;
         }
     }

     private bool TryParseRequest(ref ReadOnlySequence<byte> buffer)
     {
         try
         {
             if (!HardenedParser.TryExtractFullHeader(
                     ref buffer, _request, in Limits, out int bytesRead))
                 return false; // incomplete, wait for more data

             // Post-parse security checks
             if (RequestSemantics.HasTransferEncodingWithContentLength(_request))
                 throw new InvalidOperationException("Request smuggling attempt.");

             if (RequestSemantics.HasDotSegments(_request))
                 throw new InvalidOperationException("Path traversal attempt.");

             buffer = buffer.Slice(bytesRead);
             return true;
         }
         catch (InvalidOperationException)
         {
             // Protocol violation — close connection
             throw;
         }
     }

     private void HandleRequest()
     {
         // Access parsed data:
         // _request.Method.Span  → e.g. "GET"
         // _request.Path.Span    → e.g. "/api/users"
         // _request.Version.Span → e.g. "HTTP/1.1"
         // _request.Headers      → KeyValueList of headers
         // _request.QueryParameters → KeyValueList of query params
     }

     private void ResetForNextRequest()
     {
         _request.Headers.Clear();
         _request.QueryParameters.Clear();
     }
 }
 ```

 ---

 ## Benchmarks

 All benchmarks use 3 segments for multi-segment cases. ROM uses a single contiguous buffer. Multi-segment input is linearized (copied to a contiguous array) before parsing.

 | Method | Mean | Allocated |
 |--------|-----:|----------:|
 | Small_ROM (~80B) | 105 ns | 0 B |
 | Small_MultiSegment | 345 ns | 112 B |
 | Header1K_ROM | 497 ns | 0 B |
 | Header1K_MultiSegment | 1,178 ns | 1,056 B |
 | Header4K_ROM | 1,909 ns | 0 B |
 | Header4K_MultiSegment | 3,900 ns | 4,128 B |
 | Header16K_ROM | 6,972 ns | 0 B |
 | Header16K_MultiSegment | 13,746 ns | 16,416 B |
 | Header32K_ROM | 3,813 ns | 0 B |
 | Header32K_MultiSegment | 17,705 ns | 32,808 B |

 **Key takeaways:**
 - ROM path is always zero-allocation
 - Multi-segment linearization provides ROM-speed parsing with a single upfront allocation
 - Incomplete input (no `\r\n\r\n`) returns `false` with zero allocation

 ### Running Benchmarks

 ```bash
 cd src/Benchmarks
 dotnet run -c Release
 ```

 ---

 ## API Reference

 ### HardenedParser

 ```csharp
 namespace Glyph11.Parser.Hardened;

 public static partial class HardenedParser
 {
     // Entry point: dispatches to ROM or linearizes multi-segment to ROM
     public static bool TryExtractFullHeader(
         ref ReadOnlySequence<byte> input,
         BinaryRequest request,
         in ParserLimits limits,
         out int bytesReadCount);

     // Single-segment with limits (zero-allocation)
     public static bool TryExtractFullHeaderROM(
         ref ReadOnlyMemory<byte> input,
         BinaryRequest request,
         in ParserLimits limits,
         out int bytesReadCount);
 }
 ```

 ### ParserLimits

 ```csharp
 namespace Glyph11.Parser;

 public readonly record struct ParserLimits
 {
     public int MaxHeaderCount           { get; init; }  // 100
     public int MaxHeaderNameLength      { get; init; }  // 256
     public int MaxHeaderValueLength     { get; init; }  // 8192
     public int MaxUrlLength             { get; init; }  // 8192
     public int MaxQueryParameterCount   { get; init; }  // 128
     public int MaxMethodLength          { get; init; }  // 16
     public int MaxTotalHeaderBytes      { get; init; }  // 32768

     public static ParserLimits Default { get; }
 }
 ```

 ### RequestSemantics

 ```csharp
 namespace Glyph11.Validation;

 public static class RequestSemantics
 {
     public static bool HasConflictingContentLength(BinaryRequest request);
     public static bool HasTransferEncodingWithContentLength(BinaryRequest request);
     public static bool HasDotSegments(BinaryRequest request);
 }
 ```

 ### BinaryRequest

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

 ### KeyValueList

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

 ### BufferSegment

 ```csharp
 namespace Glyph11.Utils;

 public sealed class BufferSegment : ReadOnlySequenceSegment<byte>
 {
     public BufferSegment(ReadOnlyMemory<byte> memory);
     public BufferSegment Append(ReadOnlyMemory<byte> memory);
 }
 ```
