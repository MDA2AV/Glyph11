using System.Buffers;
using System.Runtime.CompilerServices;
using Glyph11.Protocol;

namespace Glyph11.Parser;

public partial class Parser11
{
    public static bool TryExtractFullHeaderMultiSegment(ReadOnlySequence<byte> seq, IBinaryRequest request, ref int position)
    {
        var sequenceReader = new SequenceReader<byte>(seq);

        if (!sequenceReader.TryReadTo(out ReadOnlySequence<byte> method, Parser11.Space))
            return false;

        request.Method = method.ToArray();
        
        if (!sequenceReader.TryReadTo(out ReadOnlySequence<byte> urlSequence, (byte)' '))
            return false;
        
        
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    private static void ParseUrl(in ReadOnlySequence<byte> urlSequence, IBinaryRequest request)
    {
        /*var urlSpan = urlSequence.ToSpan();
        var queryStart = urlSpan.IndexOf((byte)'?');

        if (queryStart != -1)
        {
            // URL has query parameters
            var routeSpan = urlSpan[..queryStart];
            request.Route = CachedData.CachedRoutes.GetOrAdd(routeSpan);

            // Parse query parameters
            ParseQueryParameters(urlSpan[(queryStart + 1)..], request);
        }
        else
        {
            // Simple URL without query parameters  
            request.Route = CachedData.CachedRoutes.GetOrAdd(urlSpan);
        }*/
    }
}