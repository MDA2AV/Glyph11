using System.Buffers;
using Glyph11.Protocol;

namespace Glyph11.Parser;

public static partial class Parser11
{
    /// <summary>
    /// Tries to extract a full header(status line plus headers), will not yield any progress unless full header is present.
    /// </summary>
    public static bool TryExtractFullHeader(ref ReadOnlySequence<byte> input, BinaryRequest request, out int bytesReadCount)
    {
        if (input.IsSingleSegment)
        {
            ReadOnlyMemory<byte> singleMemorySegment = input.First;

            return TryExtractFullHeaderReadOnlyMemory(ref singleMemorySegment, request, out bytesReadCount);
        }

        return TryExtractFullHeaderReadOnlySequence(ref input, request, out bytesReadCount);
    }
}
