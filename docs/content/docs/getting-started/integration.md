---
title: PipeReader Integration
weight: 2
---

A complete integration pattern showing how to use Glyph11 with `System.IO.Pipelines.PipeReader` in a request loop.

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

## Key Patterns

### Reuse `BinaryRequest`

The `BinaryRequest` instance is created once and reused across requests. Between requests, call `Clear()` on the `Headers` and `QueryParameters` lists to reset state without deallocating pooled arrays.

### Advance correctly

Call `reader.AdvanceTo(buffer.Start, buffer.End)` after processing. The first argument marks consumed data; the second marks examined data. This tells the `PipeReader` how much has been processed and how far ahead you've looked.

### Handle incomplete input

`TryExtractFullHeader` returns `false` when the header is incomplete (no `\r\n\r\n` terminator found). This is not an error — the caller should continue reading more data from the pipe.

### Post-parse validation

Always run `RequestSemantics` checks after parsing to catch protocol-level attacks. The parser validates syntax; semantic checks validate meaning.
