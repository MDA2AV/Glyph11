using Glyph11.Protocol;

namespace Glyph11.Parser.ZParser;

public static partial class ZParser
{
    public static bool TryExtractFullHeaderROM(ref ReadOnlyMemory<byte> input, BinaryRequest request, out int bytesReadCount)
    {
        bytesReadCount = -1;
        var span = input.Span;

        // Request line end — no CrlfCrlf pre-scan
        int requestLineEnd = span.IndexOf(Crlf);
        if (requestLineEnd < 0) return false;

        var requestLine = span[..requestLineEnd];

        int firstSpace = requestLine.IndexOf(Space);
        if (firstSpace < 0) return false;

        int secondSpaceRel = requestLine[(firstSpace + 1)..].IndexOf(Space);
        if (secondSpaceRel < 0) return false;

        int secondSpace = firstSpace + 1 + secondSpaceRel;

        request.Method = input[..firstSpace];

        int urlStart = firstSpace + 1;
        int urlLen = secondSpace - urlStart;
        var urlSpan = requestLine.Slice(urlStart, urlLen);

        int queryStart = urlSpan.IndexOf(Question);
        if (queryStart >= 0)
        {
            request.Path = input.Slice(urlStart, queryStart);

            int queryAbsStart = urlStart + queryStart + 1;
            int queryLen = urlLen - (queryStart + 1);
            var query = span.Slice(queryAbsStart, queryLen);

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
            int lineLen = span[lineStart..].IndexOf(Crlf);
            if (lineLen < 0) return false;

            if (lineLen == 0)
                break;

            var line = span.Slice(lineStart, lineLen);
            int colon = line.IndexOf(Colon);
            if (colon > 0)
            {
                int valAbsStart = lineStart + colon + 1;

                while (valAbsStart < lineStart + lineLen)
                {
                    byte b = span[valAbsStart];
                    if (b != (byte)' ' && b != (byte)'\t') break;
                    valAbsStart++;
                }

                int valLen = (lineStart + lineLen) - valAbsStart;

                request.Headers.Add(
                    input.Slice(lineStart, colon),
                    input.Slice(valAbsStart, valLen));
            }

            lineStart += lineLen + 2;
        }

        bytesReadCount = lineStart + 1;
        return true;
    }
}
