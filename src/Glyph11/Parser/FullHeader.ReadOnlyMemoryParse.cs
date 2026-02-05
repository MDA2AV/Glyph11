using Glyph11.Protocol;

namespace Glyph11.Parser;

public static partial class Parser11
{
    /// <summary>
    /// Hot Path, single segment
    /// </summary>
    public static bool TryExtractFullHeaderReadOnlyMemory(ref ReadOnlyMemory<byte> input, BinaryRequest request, out int bytesReadCount)
    {
        bytesReadCount = -1;
        var slicedInputSpan = input.Span;

        int headerEnd = slicedInputSpan.IndexOf(CrlfCrlf);
        if (headerEnd < 0) return false;

        int requestLineEnd = slicedInputSpan.IndexOf(Crlf);
        if (requestLineEnd < 0) throw new InvalidOperationException("Invalid HTTP/1.1 request line.");

        var requestLine = slicedInputSpan[..requestLineEnd];

        int firstSpaceIndex = requestLine.IndexOf(Space);
        if (firstSpaceIndex < 0) throw new InvalidOperationException("Invalid request line.");

        int secondSpaceRelativeIndex = requestLine[(firstSpaceIndex + 1)..].IndexOf(Space);
        if (secondSpaceRelativeIndex < 0) throw new InvalidOperationException("Invalid request line.");

        int secondSpaceIndex = firstSpaceIndex + 1 + secondSpaceRelativeIndex;

        request.Method = input[..firstSpaceIndex];

        int urlStart = firstSpaceIndex + 1;
        int urlLen = secondSpaceIndex - urlStart;
        var urlSpan = requestLine.Slice(urlStart, urlLen);

        int queryStartIndex = urlSpan.IndexOf(Question); // '?'
        if (queryStartIndex >= 0)
        {
            // Route is path portion only
            request.Path = input.Slice(urlStart, queryStartIndex);

            // Query part after '?'
            int queryAbsStart = urlStart + queryStartIndex + 1;
            int queryLen = urlLen - (queryStartIndex + 1);
            var query = slicedInputSpan.Slice(queryAbsStart, queryLen);

            int cur = 0;
            while (cur < query.Length)
            {
                int pairAbsStart = queryAbsStart + cur;

                int amp = query[cur..].IndexOf(QuerySeparator); // '&'
                int pairLen = (amp < 0) ? (query.Length - cur) : amp;

                var pair = query.Slice(cur, pairLen);
                int eq = pair.IndexOf(Equal); // '='

                if (eq > 0)
                {
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

        int lineStart = requestLineEnd + 2;

        while (true)
        {
            int lineLen = slicedInputSpan[lineStart..].IndexOf(Crlf);
            if (lineLen < 0) throw new InvalidOperationException("Invalid headers.");

            if (lineLen == 0)
                break;

            var line = slicedInputSpan.Slice(lineStart, lineLen);
            int colon = line.IndexOf(Colon);
            if (colon > 0)
            {
                int keyAbsStart = lineStart;
                int valAbsStart = lineStart + colon + 1;

                while (valAbsStart < lineStart + lineLen)
                {
                    byte b = slicedInputSpan[valAbsStart];
                    if (b != (byte)' ' && b != (byte)'\t') break;
                    valAbsStart++;
                }

                int valLen = (lineStart + lineLen) - valAbsStart;

                request.Headers.Add(
                    input.Slice(keyAbsStart, colon),
                    input.Slice(valAbsStart, valLen));
            }

            lineStart += lineLen + 2;
        }

        bytesReadCount += headerEnd + 4;
        return true;
    }
}
