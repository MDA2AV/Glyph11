using System.Buffers;

namespace Glyph11.Utils;

/// <summary>
/// Linked-list segment for constructing multi-segment <see cref="ReadOnlySequence{T}"/> buffers.
/// Primarily useful for testing and benchmarking the multi-segment parser path.
/// </summary>
public sealed class BufferSegment : ReadOnlySequenceSegment<byte>
{
    /// <summary>
    /// Creates a segment backed by the given memory.
    /// </summary>
    public BufferSegment(ReadOnlyMemory<byte> memory)
    {
        Memory = memory;
    }

    /// <summary>
    /// Appends a new segment after this one and returns the new (last) segment.
    /// Chain multiple calls to build a multi-segment sequence.
    /// </summary>
    public BufferSegment Append(ReadOnlyMemory<byte> memory)
    {
        var next = new BufferSegment(memory)
        {
            RunningIndex = RunningIndex + Memory.Length
        };
        Next = next;
        return next;
    }
}