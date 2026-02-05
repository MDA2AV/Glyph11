using System.Buffers;
using Glyph11.Protocol;

namespace Glyph11.Parser.Hardened;

public static partial class HardenedParser
{
    /// <summary>
    /// Security-hardened entry point. Dispatches to ROM or ROS path based on segment layout.
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

        return TryExtractFullHeaderROS(ref input, request, in limits, out bytesReadCount);
    }
}
