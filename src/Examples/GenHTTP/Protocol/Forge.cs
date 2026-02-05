using System.Buffers;

namespace GinHTTP.Protocol;

public static class Forge
{

    public static ReadOnlyMemory<byte> ToMemory(this ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsEmpty)
            return ReadOnlyMemory<byte>.Empty;

        if (sequence.IsSingleSegment)
            return sequence.First;

        var buffer = new byte[sequence.Length];
        sequence.CopyTo(buffer);

        return buffer;
    }

}