using System.Buffers;
using Glyph11.Protocol;

namespace Glyph11.Parser.ZParser;

public static partial class ZParser
{
    public static bool TryExtractFullHeader(ref ReadOnlySequence<byte> input, BinaryRequest request, out int bytesReadCount)
    {
        if (input.IsSingleSegment)
        {
            ReadOnlyMemory<byte> singleSegment = input.First;
            return TryExtractFullHeaderROM(ref singleSegment, request, out bytesReadCount);
        }

        return TryExtractFullHeaderLinearized(ref input, request, out bytesReadCount);
    }

    private static bool TryExtractFullHeaderLinearized(
        ref ReadOnlySequence<byte> seq, BinaryRequest request, out int bytesReadCount)
    {
        byte[] buffer = seq.ToArray();
        ReadOnlyMemory<byte> mem = buffer;
        return TryExtractFullHeaderROM(ref mem, request, out bytesReadCount);
    }
}
