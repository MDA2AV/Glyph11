using System.Buffers;
using Glyph11.Protocol;

namespace Glyph11.Parser.FlexParser;

public static partial class FlexibleParser
{
    /// <summary>
    /// Tries to extract a full header(status line plus headers), will not yield any progress unless full header is present.
    /// </summary>
    public static bool TryExtractFullHeader(ref ReadOnlySequence<byte> input, BinaryRequest request, out int bytesReadCount,
        bool linearize = false)
    {
        if (input.IsSingleSegment)
        {
            ReadOnlyMemory<byte> singleMemorySegment = input.First;

            return TryExtractFullHeaderReadOnlyMemory(ref singleMemorySegment, request, out bytesReadCount);
        }

        if (linearize)
        {
            if (!IsFullHeaderPresent(ref input))
            {
                bytesReadCount = -1;
                return false;
            }

            ReadOnlyMemory<byte> mem = input.ToArray();
            return TryExtractFullHeaderReadOnlyMemory(ref mem, request, out bytesReadCount);
        }

        return TryExtractFullHeaderReadOnlySequence(ref input, request, out bytesReadCount);
    }
}
