using System.Buffers;
using Glyph11.Protocol;

namespace Glyph11.Parser;

public partial class Parser11
{
    public static bool TryExtractFullHeader(ref ReadOnlySequence<byte> seq, IBinaryRequest request, ref int position)
    {
        var sliced = position == 0 ? seq : seq.Slice(position);
        
        if (sliced.IsSingleSegment)
        {
            ReadOnlyMemory<byte> mem = sliced.First;

            // mem is already sliced to "position", so parse from localPos=0
            int localPos = 0;
            if (!TryExtractFullHeaderSingleSegment(ref mem, request, ref localPos))
                return false;

            position += localPos;
            return true;
        }
        
        return TryExtractFullHeaderMultiSegment(ref sliced, request, ref position);
    }
}