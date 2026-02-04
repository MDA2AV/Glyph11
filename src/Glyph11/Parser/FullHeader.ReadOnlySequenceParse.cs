using System.Buffers;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Glyph11.Protocol;

namespace Glyph11.Parser;

public static partial class Parser11
{
    [Pure]
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryExtractFullHeaderReadOnlySequence(ref ReadOnlySequence<byte> seq, IBinaryRequest request, out int bytesReadCount)
    {
        bytesReadCount = -1;
        
        if (!IsFullHeaderPresent(ref seq))
            return false;

        var reader = new SequenceReader<byte>(seq);

        // ---- Status line: <METHOD> <URL> <VERSION>\r\n
        if (!reader.TryReadTo(out ReadOnlySequence<byte> statusLine, Crlf, advancePastDelimiter: true))
            return false;

        if (!TryParseStatusLine(statusLine, request))
            throw new InvalidOperationException("Invalid HTTP/1.1 request line.");

        // ---- Headers: lines until \r\n
        while (true)
        {
            if (!reader.TryReadTo(out ReadOnlySequence<byte> headerLine, Crlf, advancePastDelimiter: true))
                throw new InvalidOperationException("Invalid headers.");

            // empty line => end of headers
            if (headerLine.Length == 0)
                break;

            TryParseHeaderLine(headerLine, request);
        }

        // reader is now positioned right after the empty line CRLF,
        // meaning we've consumed exactly the full header (including CRLFCRLF).
        bytesReadCount += checked((int)reader.Consumed);
        return true;
    }

    [Pure]
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseStatusLine(in ReadOnlySequence<byte> statusLineSequence, IBinaryRequest request)
    {
        var r = new SequenceReader<byte>(statusLineSequence);

        // METHOD
        if (!r.TryReadTo(out ReadOnlySequence<byte> methodSeq, Space, advancePastDelimiter: true))
            return false;

        // URL (path[?query])
        if (!r.TryReadTo(out ReadOnlySequence<byte> urlSeq, Space, advancePastDelimiter: true))
            return false;

        // VERSION (we don't really need it for header parsing; just validate it's present)
        if (r.Remaining == 0)
            return false;

        request.Method = methodSeq.ToArray();

        // Split URL into route + query
        SequencePosition? qPos = urlSeq.PositionOf(Question); // '?'
        if (qPos is null)
        {
            request.Route = urlSeq.ToArray();
            return true;
        }

        var routeSeq = urlSeq.Slice(0, qPos.Value);
        request.Route = routeSeq.ToArray();

        // Query begins AFTER '?'
        var querySeq = urlSeq.Slice(urlSeq.GetPosition(1, qPos.Value));

        ParseQueryParams(querySeq, request);
        return true;
    }
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ParseQueryParams(in ReadOnlySequence<byte> querySeq, IBinaryRequest request)
    {
        // Split by '&', then key/value by '=' (same behavior as your single segment)
        var qReader = new SequenceReader<byte>(querySeq);

        while (qReader.Remaining > 0)
        {
            // read one pair (until '&' or end)
            if (!qReader.TryReadTo(out ReadOnlySequence<byte> pairSeq, QuerySeparator, advancePastDelimiter: true))
            {
                // last pair = rest
                pairSeq = qReader.Sequence.Slice(qReader.Position, qReader.Sequence.End);
                qReader.Advance(pairSeq.Length);
            }

            if (pairSeq.Length == 0)
                continue;

            var eqPos = pairSeq.PositionOf(Equal); // '='
            if (eqPos is null)
                continue;

            // key = [0..eq), val = (eq+1..end)
            var keySeq = pairSeq.Slice(0, eqPos.Value);

            // If '=' is last char => empty value; your single-seg skips eq>0 only,
            // so we match that spirit: require non-empty key.
            if (keySeq.Length == 0)
                continue;

            var valSeq = pairSeq.Slice(pairSeq.GetPosition(1, eqPos.Value));

            request.QueryParameters.Add(
                keySeq.ToArray(),
                valSeq.ToArray());
        }
    }
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TryParseHeaderLine(in ReadOnlySequence<byte> lineSeq, IBinaryRequest request)
    {
        // Find ':'
        var colonPos = lineSeq.PositionOf(Colon);
        if (colonPos is null)
            return;

        var keySeq = lineSeq.Slice(0, colonPos.Value);

        // value starts after ':'
        var valueSeq = lineSeq.Slice(lineSeq.GetPosition(1, colonPos.Value));

        // Trim leading SP/HTAB in the value
        valueSeq = TrimStartSpacesAndTabs(valueSeq);

        if (keySeq.Length == 0)
            return;

        request.Headers.Add(
            keySeq.ToArray(),
            valueSeq.ToArray());
    }
    
    [Pure]
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySequence<byte> TrimStartSpacesAndTabs(ReadOnlySequence<byte> seq)
    {
        var r = new SequenceReader<byte>(seq);

        while (r.Remaining > 0)
        {
            if (!r.TryPeek(out byte b))
                break;

            if (b != (byte)' ' && b != (byte)'\t')
                break;

            r.Advance(1);
        }

        return seq.Slice(r.Position);
    }

    [Pure]
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsFullHeaderPresent(ref ReadOnlySequence<byte> seq)
    {
        var sequenceReader = new SequenceReader<byte>(seq);
        return sequenceReader.TryReadTo(out ReadOnlySequence<byte> _, CrlfCrlf, advancePastDelimiter: true);
    }
}