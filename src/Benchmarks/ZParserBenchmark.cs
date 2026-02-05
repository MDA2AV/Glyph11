using System.Buffers;
using BenchmarkDotNet.Attributes;
using GenHTTP.Engine.Draft.Types;
using Glyph11.Parser.ZParser;

namespace Benchmarks;

[MemoryDiagnoser]
public class ZParserBenchmark
{
    private readonly Request _into = new();

    private readonly ReadOnlySequence<byte> _buffer =
        new(("GET /route?p1=1&p2=2&p3=3&p4=4 HTTP/1.1\r\n"u8 +
            "Content-Length: 100\r\n"u8 +
            "Server: GenHTTP\r\n\r\n"u8).ToArray());

    private ReadOnlySequence<byte> _segmentedBuffer = CreateMultiSegment();

    private ReadOnlyMemory<byte> _memory;

    public ZParserBenchmark()
    {
        _memory = _buffer.ToArray();
    }

    private static ReadOnlySequence<byte> CreateMultiSegment()
    {
        var seg1 = "GET /route?p1=1&p2=2&p3=3&p4=4 HT"u8.ToArray();
        var seg2 = "TP/1.1\r\nContent-Length: 100\r\nServer: "u8.ToArray();
        var seg3 = "GenHTTP\r\n\r\n"u8.ToArray();

        var first = new Glyph11.Utils.BufferSegment(seg1);
        var last = first.Append(seg2).Append(seg3);

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    [Benchmark]
    public void BenchmarkSingleSegmentParser()
    {
        _into.Reset();
        ZParser.TryExtractFullHeaderROM(ref _memory, _into.Source, out var bytesReadCount);
    }

    [Benchmark]
    public void BenchmarkMultiSegmentParser()
    {
        _into.Reset();
        ZParser.TryExtractFullHeader(ref _segmentedBuffer, _into.Source, out var bytesReadCount);
    }
}
