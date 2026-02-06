using System.Buffers;
using Glyph11.Protocol;

namespace Glyph11.Parser.FlexibleParser;

public static partial class FlexibleParser
{
    /// <summary>
    /// Entry point: tries to extract a complete HTTP/1.1 header block (request line + headers).
    /// <para>
    /// Single-segment input is dispatched to the zero-copy ROM path.
    /// Multi-segment input is checked for completeness (<c>\r\n\r\n</c>), then linearized
    /// via <c>ToArray()</c> and parsed through the ROM path.
    /// </para>
    /// </summary>
    /// <param name="input">Input buffer from the network layer.</param>
    /// <param name="request">Target to populate with parsed request data.</param>
    /// <param name="bytesReadCount">Bytes consumed on success, or -1 if incomplete.</param>
    /// <returns><c>true</c> if a complete header was parsed; <c>false</c> if more data is needed.</returns>
    public static bool TryExtractFullHeader(ref ReadOnlySequence<byte> input, BinaryRequest request, out int bytesReadCount)
    {
        if (input.IsSingleSegment)
        {
            ReadOnlyMemory<byte> singleMemorySegment = input.First;
            return TryExtractFullHeaderReadOnlyMemory(ref singleMemorySegment, request, out bytesReadCount);
        }

        // Check for header completeness before allocating
        var reader = new SequenceReader<byte>(input);
        if (!reader.TryReadTo(out ReadOnlySequence<byte> _, CrlfCrlf, advancePastDelimiter: true))
        {
            bytesReadCount = -1;
            return false;
        }

        // Linearize: copy all segments into a single contiguous array, then parse via ROM
        ReadOnlyMemory<byte> mem = input.ToArray();
        return TryExtractFullHeaderReadOnlyMemory(ref mem, request, out bytesReadCount);
    }
}
