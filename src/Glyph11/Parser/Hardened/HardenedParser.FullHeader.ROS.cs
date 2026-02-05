using System.Buffers;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Glyph11.Protocol;

namespace Glyph11.Parser.Hardened;

public static partial class HardenedParser
{
    /// <summary>
    /// Multi-segment parse path with full security validation.
    /// Returns false if incomplete; throws InvalidOperationException if structurally invalid.
    /// </summary>
    [Pure]
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryExtractFullHeaderROS(
        ref ReadOnlySequence<byte> seq, BinaryRequest request,
        in ParserLimits limits, out int bytesReadCount)
    {
        bytesReadCount = -1;

        var reader = new SequenceReader<byte>(seq);

        // ---- Status line: METHOD SP URL SP VERSION\r\n ----
        if (!reader.TryReadTo(out ReadOnlySequence<byte> statusLine, Crlf, advancePastDelimiter: true))
            return false;

        TryParseStatusLineX(statusLine, request, in limits);

        // ---- Headers ----
        int headerCount = 0;

        while (true)
        {
            if (!reader.TryReadTo(out ReadOnlySequence<byte> headerLine, Crlf, advancePastDelimiter: true))
                return false; // incomplete — need more data

            if (headerLine.Length == 0)
                break;

            TryParseHeaderLineX(headerLine, request, in limits, ref headerCount);
        }

        int consumed = checked((int)reader.Consumed);
        if (consumed > limits.MaxTotalHeaderBytes)
            throw new InvalidOperationException("Total header size exceeds limit.");

        bytesReadCount += consumed;
        return true;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TryParseStatusLineX(
        in ReadOnlySequence<byte> statusLineSequence, BinaryRequest request,
        in ParserLimits limits)
    {
        // METHOD — find first SP
        var firstSpacePos = statusLineSequence.PositionOf(Space);
        if (firstSpacePos is null)
            throw new InvalidOperationException("Invalid HTTP/1.1 request line.");

        var methodSeq = statusLineSequence.Slice(0, firstSpacePos.Value);
        if (methodSeq.Length == 0 || methodSeq.Length > limits.MaxMethodLength)
            throw new InvalidOperationException("Method length exceeds limit.");

        // URL — find second SP
        var afterMethod = statusLineSequence.Slice(statusLineSequence.GetPosition(1, firstSpacePos.Value));
        var secondSpacePos = afterMethod.PositionOf(Space);
        if (secondSpacePos is null)
            throw new InvalidOperationException("Invalid request line: missing version.");

        var urlSeq = afterMethod.Slice(0, secondSpacePos.Value);
        if (urlSeq.Length > limits.MaxUrlLength)
            throw new InvalidOperationException("URL length exceeds limit.");

        // VERSION
        var versionSeq = afterMethod.Slice(afterMethod.GetPosition(1, secondSpacePos.Value));
        if (versionSeq.Length == 0)
            throw new InvalidOperationException("Invalid request line: missing version.");
        if (!IsValidHttpVersionSequence(versionSeq))
            throw new InvalidOperationException("Invalid HTTP version.");

        // Copy method, then validate the contiguous span
        var methodArr = methodSeq.ToArray();
        if (!IsValidToken(methodArr))
            throw new InvalidOperationException("Method contains invalid token characters.");

        request.Method = methodArr;
        request.Version = ResolveCachedVersion(versionSeq);

        // Split URL into path + query
        SequencePosition? qPos = urlSeq.PositionOf(Question);
        if (qPos is null)
        {
            request.Path = urlSeq.ToArray();
            return;
        }

        var routeSeq = urlSeq.Slice(0, qPos.Value);
        request.Path = routeSeq.ToArray();

        var querySeq = urlSeq.Slice(urlSeq.GetPosition(1, qPos.Value));
        ParseQueryParamsX(querySeq, request, in limits);
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ParseQueryParamsX(
        in ReadOnlySequence<byte> querySeq, BinaryRequest request,
        in ParserLimits limits)
    {
        var remaining = querySeq;
        int paramCount = 0;

        while (remaining.Length > 0)
        {
            ReadOnlySequence<byte> pairSeq;
            var ampPos = remaining.PositionOf(QuerySeparator);
            if (ampPos is null)
            {
                pairSeq = remaining;
                remaining = remaining.Slice(remaining.End);
            }
            else
            {
                pairSeq = remaining.Slice(0, ampPos.Value);
                remaining = remaining.Slice(remaining.GetPosition(1, ampPos.Value));
            }

            if (pairSeq.Length == 0)
                continue;

            var eqPos = pairSeq.PositionOf(Equal);
            if (eqPos is null)
                continue;

            var keySeq = pairSeq.Slice(0, eqPos.Value);
            if (keySeq.Length == 0)
                continue;

            if (++paramCount > limits.MaxQueryParameterCount)
                throw new InvalidOperationException("Query parameter count exceeds limit.");

            var valSeq = pairSeq.Slice(pairSeq.GetPosition(1, eqPos.Value));

            request.QueryParameters.Add(
                keySeq.ToArray(),
                valSeq.ToArray());
        }
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TryParseHeaderLineX(
        in ReadOnlySequence<byte> lineSeq, BinaryRequest request,
        in ParserLimits limits, ref int headerCount)
    {
        var colonPos = lineSeq.PositionOf(Colon);
        if (colonPos is null)
            throw new InvalidOperationException("Malformed header line: missing colon.");

        var keySeq = lineSeq.Slice(0, colonPos.Value);
        if (keySeq.Length == 0)
            throw new InvalidOperationException("Header name is empty.");

        if (keySeq.Length > limits.MaxHeaderNameLength)
            throw new InvalidOperationException("Header name length exceeds limit.");

        var valueSeq = lineSeq.Slice(lineSeq.GetPosition(1, colonPos.Value));
        valueSeq = TrimStartSpacesAndTabsX(valueSeq);

        if (valueSeq.Length > limits.MaxHeaderValueLength)
            throw new InvalidOperationException("Header value length exceeds limit.");

        if (++headerCount > limits.MaxHeaderCount)
            throw new InvalidOperationException("Header count exceeds limit.");

        // Copy first, then validate the contiguous span (avoids multi-segment iteration)
        var keyArr = keySeq.ToArray();
        if (!IsValidToken(keyArr))
            throw new InvalidOperationException("Header name contains invalid token characters.");

        var valArr = valueSeq.ToArray();
        if (!IsValidFieldValue(valArr))
            throw new InvalidOperationException("Header value contains invalid characters.");

        request.Headers.Add(keyArr, valArr);
    }

    [Pure]
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySequence<byte> TrimStartSpacesAndTabsX(ReadOnlySequence<byte> seq)
    {
        // Fast path: OWS is almost always within the first segment
        var span = seq.FirstSpan;
        int skip = 0;
        while (skip < span.Length)
        {
            byte b = span[skip];
            if (b != (byte)' ' && b != (byte)'\t') break;
            skip++;
        }

        if (skip < span.Length || seq.IsSingleSegment)
            return seq.Slice(skip);

        // Slow path: OWS spans segment boundary (extremely rare)
        var r = new SequenceReader<byte>(seq);
        r.Advance(skip);
        while (r.Remaining > 0)
        {
            if (!r.TryPeek(out byte b)) break;
            if (b != (byte)' ' && b != (byte)'\t') break;
            r.Advance(1);
        }
        return seq.Slice(r.Position);
    }

}
