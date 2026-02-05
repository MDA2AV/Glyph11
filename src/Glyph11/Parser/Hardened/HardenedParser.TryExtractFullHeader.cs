using System.Buffers;
using Glyph11.Protocol;

namespace Glyph11.Parser.Hardened;

public static partial class HardenedParser
{
    /// <summary>
    /// Security-hardened entry point.
    /// Single-segment input uses zero-copy ROM path. Multi-segment input is linearized to a contiguous array first.
    /// Returns false if incomplete; throws InvalidOperationException if structurally invalid.
    /// </summary>
    public static bool TryExtractFullHeader(
        ref ReadOnlySequence<byte> input, BinaryRequest request,
        in ParserLimits limits, out int bytesReadCount)
    {
        if (input.IsSingleSegment)
        {
            ReadOnlyMemory<byte> singleMemorySegment = input.First;
            return TryExtractFullHeaderROM(ref singleMemorySegment, request, in limits, out bytesReadCount);
        }

        var reader = new SequenceReader<byte>(input);
        if (!reader.TryReadTo(out ReadOnlySequence<byte> _, CrlfCrlf, advancePastDelimiter: true))
        {
            bytesReadCount = -1;
            return false;
        }

        ReadOnlyMemory<byte> mem = input.ToArray();
        return TryExtractFullHeaderROM(ref mem, request, in limits, out bytesReadCount);
    }
}
