using System.Runtime.CompilerServices;
using Glyph11.Protocol;

namespace Glyph11.Parser;

public static partial class Parser11x
{
    /// <summary>
    /// Hot path — single-segment parse with full security validation.
    /// Returns false if incomplete; throws InvalidOperationException if structurally invalid.
    /// </summary>
    [SkipLocalsInit]
    public static bool TryExtractFullHeaderROM(
        ref ReadOnlyMemory<byte> input, BinaryRequest request,
        in ParserLimits limits, out int bytesReadCount)
    {
        bytesReadCount = -1;
        var span = input.Span;

        int headerEnd = span.IndexOf(CrlfCrlf);
        if (headerEnd < 0) return false;

        int totalHeaderBytes = headerEnd + 4;
        if (totalHeaderBytes > limits.MaxTotalHeaderBytes)
            throw new InvalidOperationException("Total header size exceeds limit.");

        // ---- Request line: METHOD SP URL SP VERSION CRLF ----

        int requestLineEnd = span.IndexOf(Crlf);
        if (requestLineEnd < 0)
            throw new InvalidOperationException("Invalid HTTP/1.1 request line.");

        var requestLine = span[..requestLineEnd];

        int firstSpace = requestLine.IndexOf(Space);
        if (firstSpace < 0)
            throw new InvalidOperationException("Invalid request line: missing method.");

        int secondSpaceRel = requestLine[(firstSpace + 1)..].IndexOf(Space);
        if (secondSpaceRel < 0)
            throw new InvalidOperationException("Invalid request line: missing version.");

        int secondSpace = firstSpace + 1 + secondSpaceRel;

        // --- Method ---
        var methodSpan = requestLine[..firstSpace];
        if (methodSpan.Length == 0 || methodSpan.Length > limits.MaxMethodLength)
            throw new InvalidOperationException("Method length exceeds limit.");
        if (!IsValidToken(methodSpan))
            throw new InvalidOperationException("Method contains invalid token characters.");

        request.Method = input[..firstSpace];

        // --- URL ---
        int urlStart = firstSpace + 1;
        int urlLen = secondSpace - urlStart;
        if (urlLen > limits.MaxUrlLength)
            throw new InvalidOperationException("URL length exceeds limit.");

        var urlSpan = requestLine.Slice(urlStart, urlLen);

        // --- Version ---
        var versionSpan = requestLine[(secondSpace + 1)..];
        if (!IsValidHttpVersion(versionSpan))
            throw new InvalidOperationException("Invalid HTTP version.");

        request.Version = input.Slice(secondSpace + 1, versionSpan.Length);

        // --- Path + Query ---
        int queryStartIndex = urlSpan.IndexOf(Question);
        if (queryStartIndex >= 0)
        {
            request.Path = input.Slice(urlStart, queryStartIndex);

            int queryAbsStart = urlStart + queryStartIndex + 1;
            int queryLen = urlLen - (queryStartIndex + 1);
            var query = span.Slice(queryAbsStart, queryLen);

            int paramCount = 0;
            int cur = 0;
            while (cur < query.Length)
            {
                int pairAbsStart = queryAbsStart + cur;

                int amp = query[cur..].IndexOf(QuerySeparator);
                int pairLen = (amp < 0) ? (query.Length - cur) : amp;

                var pair = query.Slice(cur, pairLen);
                int eq = pair.IndexOf(Equal);

                if (eq > 0)
                {
                    if (++paramCount > limits.MaxQueryParameterCount)
                        throw new InvalidOperationException("Query parameter count exceeds limit.");

                    request.QueryParameters.Add(
                        input.Slice(pairAbsStart, eq),
                        input.Slice(pairAbsStart + eq + 1, pairLen - (eq + 1)));
                }

                cur += pairLen + (amp < 0 ? 0 : 1);
            }
        }
        else
        {
            request.Path = input.Slice(urlStart, urlLen);
        }

        // ---- Headers ----

        int lineStart = requestLineEnd + 2;
        int headerCount = 0;

        while (true)
        {
            int lineLen = span[lineStart..].IndexOf(Crlf);
            if (lineLen < 0)
                throw new InvalidOperationException("Invalid headers.");

            if (lineLen == 0)
                break;

            var line = span.Slice(lineStart, lineLen);
            int colon = line.IndexOf(Colon);

            if (colon <= 0)
                throw new InvalidOperationException(colon == 0
                    ? "Header name is empty."
                    : "Malformed header line: missing colon.");

            // Validate header name
            var nameSpan = line[..colon];
            if (nameSpan.Length > limits.MaxHeaderNameLength)
                throw new InvalidOperationException("Header name length exceeds limit.");
            if (!IsValidToken(nameSpan))
                throw new InvalidOperationException("Header name contains invalid token characters.");

            // Trim leading OWS from value
            int valAbsStart = lineStart + colon + 1;
            while (valAbsStart < lineStart + lineLen)
            {
                byte b = span[valAbsStart];
                if (b != (byte)' ' && b != (byte)'\t') break;
                valAbsStart++;
            }

            int valLen = (lineStart + lineLen) - valAbsStart;

            // Validate header value
            var valueSpan = span.Slice(valAbsStart, valLen);
            if (valLen > limits.MaxHeaderValueLength)
                throw new InvalidOperationException("Header value length exceeds limit.");
            if (!IsValidFieldValue(valueSpan))
                throw new InvalidOperationException("Header value contains invalid characters.");

            if (++headerCount > limits.MaxHeaderCount)
                throw new InvalidOperationException("Header count exceeds limit.");

            request.Headers.Add(
                input.Slice(lineStart, colon),
                input.Slice(valAbsStart, valLen));

            lineStart += lineLen + 2;
        }

        bytesReadCount += totalHeaderBytes;
        return true;
    }
}
