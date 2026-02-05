using System.Buffers;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Glyph11.Protocol;

namespace Glyph11.Parser;

public static partial class Parser11x
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

        if (!IsFullHeaderPresentROS(ref seq))
            return false;

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
                throw new InvalidOperationException("Invalid headers.");

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
        var r = new SequenceReader<byte>(statusLineSequence);

        // METHOD
        if (!r.TryReadTo(out ReadOnlySequence<byte> methodSeq, Space, advancePastDelimiter: true))
            throw new InvalidOperationException("Invalid HTTP/1.1 request line.");

        if (methodSeq.Length == 0 || methodSeq.Length > limits.MaxMethodLength)
            throw new InvalidOperationException("Method length exceeds limit.");
        if (!IsValidTokenSequence(methodSeq))
            throw new InvalidOperationException("Method contains invalid token characters.");

        // URL
        if (!r.TryReadTo(out ReadOnlySequence<byte> urlSeq, Space, advancePastDelimiter: true))
            throw new InvalidOperationException("Invalid request line: missing version.");

        if (urlSeq.Length > limits.MaxUrlLength)
            throw new InvalidOperationException("URL length exceeds limit.");

        // VERSION
        var versionSeq = statusLineSequence.Slice(r.Position);
        if (versionSeq.Length == 0)
            throw new InvalidOperationException("Invalid request line: missing version.");
        if (!IsValidHttpVersionSequence(versionSeq))
            throw new InvalidOperationException("Invalid HTTP version.");

        request.Method = methodSeq.ToArray();
        request.Version = versionSeq.ToArray();

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
        var qReader = new SequenceReader<byte>(querySeq);
        int paramCount = 0;

        while (qReader.Remaining > 0)
        {
            if (!qReader.TryReadTo(out ReadOnlySequence<byte> pairSeq, QuerySeparator, advancePastDelimiter: true))
            {
                pairSeq = qReader.Sequence.Slice(qReader.Position, qReader.Sequence.End);
                qReader.Advance(pairSeq.Length);
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
        if (!IsValidTokenSequence(keySeq))
            throw new InvalidOperationException("Header name contains invalid token characters.");

        var valueSeq = lineSeq.Slice(lineSeq.GetPosition(1, colonPos.Value));
        valueSeq = TrimStartSpacesAndTabsX(valueSeq);

        if (valueSeq.Length > limits.MaxHeaderValueLength)
            throw new InvalidOperationException("Header value length exceeds limit.");
        if (!IsValidFieldValueSequence(valueSeq))
            throw new InvalidOperationException("Header value contains invalid characters.");

        if (++headerCount > limits.MaxHeaderCount)
            throw new InvalidOperationException("Header count exceeds limit.");

        request.Headers.Add(
            keySeq.ToArray(),
            valueSeq.ToArray());
    }

    [Pure]
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySequence<byte> TrimStartSpacesAndTabsX(ReadOnlySequence<byte> seq)
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
    private static bool IsFullHeaderPresentROS(ref ReadOnlySequence<byte> seq)
    {
        var sequenceReader = new SequenceReader<byte>(seq);
        return sequenceReader.TryReadTo(out ReadOnlySequence<byte> _, CrlfCrlf, advancePastDelimiter: true);
    }
}
