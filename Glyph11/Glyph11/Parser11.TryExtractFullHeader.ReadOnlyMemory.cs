namespace Glyph11;

public partial class Parser11
{
    /*
    public bool TryExtractFullHeader(ref ReadOnlyMemory<byte> inputROM, ref Request11BinaryData request11BinaryData, ref int position)
    {
        var slicedInputROM = position != 0 ? inputROM[position..] : inputROM;
        var span = slicedInputROM.Span;

        int headerEnd = span.IndexOf(CrlfCrlf);
        if (headerEnd < 0) return false;

        int requestLineEnd = span.IndexOf(Crlf);
        if (requestLineEnd < 0) throw new InvalidOperationException("Invalid HTTP/1.1 request line.");

        var requestLine = span[..requestLineEnd];

        int firstSpace = requestLine.IndexOf(Space);
        if (firstSpace < 0) throw new InvalidOperationException("Invalid request line.");

        int secondSpaceRelative = requestLine[(firstSpace + 1)..].IndexOf(Space);
        if (secondSpaceRelative < 0) throw new InvalidOperationException("Invalid request line.");

        int secondSpace = firstSpace + 1 + secondSpaceRelative;

        // Method: [0..sp1)
        request11BinaryData.HttpMethod = slicedInputROM[..firstSpace];

        // URL: [sp1+1 .. sp2)
        int urlStart = firstSpace + 1;
        int urlLen = secondSpace - urlStart;
        var urlSpan = requestLine.Slice(urlStart, urlLen);

        int queryStart = urlSpan.IndexOf(Question); // '?'
        if (queryStart >= 0)
        {
            // Route: [urlStart .. urlStart+q)
            request11BinaryData.Route = slicedInputROM.Slice(urlStart, queryStart);

            // Query: after '?'
            int queryAbsStart = urlStart + queryStart + 1;
            var query = span.Slice(queryAbsStart, urlLen - (queryStart + 1));

            int cur = 0;
            while (cur < query.Length)
            {
                int amp = query[cur..].IndexOf(QuerySeparator); // '&'
                int pairLen = (amp < 0) ? (query.Length - cur) : amp;

                var pair = query.Slice(cur, pairLen);
                cur += pairLen + (amp < 0 ? 0 : 1);

                int eq = pair.IndexOf(Equal); // '='
                if (eq <= 0) continue; // skip "a" or "=b"

                // key/value slices as ReadOnlyMemory<byte> into original mem
                int keyAbsStart = queryAbsStart + (cur - (pairLen + (amp < 0 ? 0 : 1))) + 0;
                // simpler: compute from current pair start:
                int pairAbsStart = queryAbsStart + (cur - (pairLen + (amp < 0 ? 0 : 1)));
                
                request11BinaryData.QueryParameters.TryAdd(
                    slicedInputROM.Slice(pairAbsStart, eq),
                    slicedInputROM.Slice(pairAbsStart + eq + 1, pairLen - (eq + 1)));
            }
        }
        else
        {
            // Route is the entire URL
            request11BinaryData.Route = slicedInputROM.Slice(urlStart, urlLen);
        }

        // Headers start right after request line CRLF
        int lineStart = requestLineEnd + 2;

        while (true)
        {
            int lineLen = span[lineStart..].IndexOf(Crlf);
            if (lineLen < 0) throw new InvalidOperationException("Invalid headers.");

            if (lineLen == 0)
            {
                // empty line => end of headers
                break;
            }

            var line = span.Slice(lineStart, lineLen);
            int colon = line.IndexOf(Colon);
            if (colon > 0)
            {
                // "Key: Value" (assumes exactly one space after colon; you may want to trim)
                int keyAbsStart = lineStart;
                int valAbsStart = lineStart + colon + 1;

                // skip optional spaces
                if (valAbsStart < lineStart + lineLen && span[valAbsStart] == (byte)' ')
                    valAbsStart++;

                int valLen = (lineStart + lineLen) - valAbsStart;

                request11BinaryData.Headers.TryAdd(
                    slicedInputROM.Slice(keyAbsStart, colon),
                    slicedInputROM.Slice(valAbsStart, valLen));
            }

            lineStart += lineLen + 2;
        }

        position += headerEnd + 4; // consume through \r\n\r\n
        return true;
    }
    */
    
    public bool TryExtractFullHeader(
    ref ReadOnlyMemory<byte> inputROM,
    ref Request11BinaryData r,
    ref int position)
{
    var mem = position != 0 ? inputROM[position..] : inputROM;
    var span = mem.Span;

    int headerEnd = span.IndexOf(CrlfCrlf);
    if (headerEnd < 0) return false;

    // Reset per-request fields (important if r is reused)
    r.HttpMethod = default;
    r.Route = default;
    r.Body = default;
    r.QueryParameters.Clear();
    r.Headers.Clear();

    int requestLineEnd = span.IndexOf(Crlf);
    if (requestLineEnd < 0) throw new InvalidOperationException("Invalid HTTP/1.1 request line.");

    var requestLine = span[..requestLineEnd];

    int sp1 = requestLine.IndexOf(Space);
    if (sp1 < 0) throw new InvalidOperationException("Invalid request line.");

    int sp2rel = requestLine[(sp1 + 1)..].IndexOf(Space);
    if (sp2rel < 0) throw new InvalidOperationException("Invalid request line.");

    int sp2 = sp1 + 1 + sp2rel;

    // METHOD: [0..sp1)
    r.HttpMethod = mem[..sp1];

    // URL: [sp1+1 .. sp2)
    int urlStart = sp1 + 1;
    int urlLen = sp2 - urlStart;
    var urlSpan = requestLine.Slice(urlStart, urlLen);

    int q = urlSpan.IndexOf(Question); // '?'
    if (q >= 0)
    {
        // Route is path portion only
        r.Route = mem.Slice(urlStart, q);

        // Query part after '?'
        int queryAbsStart = urlStart + q + 1;
        int queryLen = urlLen - (q + 1);
        var query = span.Slice(queryAbsStart, queryLen);

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
                r.QueryParameters.Add(
                    mem.Slice(pairAbsStart, eq),
                    mem.Slice(pairAbsStart + eq + 1, pairLen - (eq + 1)));
            }

            cur += pairLen + (amp < 0 ? 0 : 1);
        }
    }
    else
    {
        // Route is entire URL
        r.Route = mem.Slice(urlStart, urlLen);
    }

    // Headers start after request line CRLF
    int lineStart = requestLineEnd + 2;

    while (true)
    {
        int lineLen = span[lineStart..].IndexOf(Crlf);
        if (lineLen < 0) throw new InvalidOperationException("Invalid headers.");

        if (lineLen == 0)
            break; // empty line => end of headers

        var line = span.Slice(lineStart, lineLen);
        int colon = line.IndexOf(Colon);
        if (colon > 0)
        {
            int keyAbsStart = lineStart;
            int valAbsStart = lineStart + colon + 1;

            // skip optional SP / HTAB
            while (valAbsStart < lineStart + lineLen)
            {
                byte b = span[valAbsStart];
                if (b != (byte)' ' && b != (byte)'\t') break;
                valAbsStart++;
            }

            int valLen = (lineStart + lineLen) - valAbsStart;

            r.Headers.Add(
                mem.Slice(keyAbsStart, colon),
                mem.Slice(valAbsStart, valLen));
        }

        lineStart += lineLen + 2;
    }

    // Advance position to end of headers (\r\n\r\n)
    position += headerEnd + 4;
    return true;
}
}