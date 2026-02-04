using System.Buffers;
using Glyph11.Protocol;

namespace Glyph11.Parser;

public partial class Parser11
{
    public static bool TryExtractFullHeader(ref ReadOnlySequence<byte> input, IBinaryRequest request, out int bytesReadCount)
    {
        if (input.IsSingleSegment)
        {
            ReadOnlyMemory<byte> mem = input.First;
            
            if (!TryExtractFullHeaderSingleSegment(ref mem, request, out bytesReadCount))
                return false;
            
            return true;
        }
        
        return TryExtractFullHeaderMultiSegment(ref input, request, out bytesReadCount);
    }
}