using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using GenHTTP.Engine.Draft.Types;
using Glyph11.Parser.FlexibleParser;

namespace Benchmarks;

public static class Program
{
    public sealed class FastConfig : ManualConfig
    {
        public FastConfig()
        {
            AddJob(Job.Default
                .WithMinIterationCount(1)
                .WithMaxIterationCount(3));

            // optional but useful (removes your other warnings)
            AddLogger(ConsoleLogger.Default);
            AddExporter(MarkdownExporter.Default);
            AddExporter(JsonExporter.FullCompressed);
            AddColumnProvider(DefaultColumnProviders.Instance);
        }
    }
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run([ typeof(FlexibleParserBenchmark), typeof(HardenedParserBenchmark) ], new FastConfig());
    }
}

[MemoryDiagnoser]
public class FlexibleParserBenchmark
{
    private readonly Request _into = new();

    // ---- Small (~80B) ----

    private readonly ReadOnlySequence<byte> _buffer =
        new(("GET /route?p1=1&p2=2&p3=3&p4=4 HTTP/1.1\r\n"u8 +
            "Content-Length: 100\r\n"u8 +
            "Server: GenHTTP\r\n\r\n"u8).ToArray());

    private ReadOnlySequence<byte> _segmentedBuffer = CreateMultiSegment();

    private ReadOnlyMemory<byte> _memory;

    // ---- Large headers: 1KB, 4KB, 16KB, 32KB ----

    private static readonly byte[] _header1K = BenchmarkData.BuildHeader(1024);
    private static readonly byte[] _header4K = BenchmarkData.BuildHeader(4096);
    private static readonly byte[] _header16K = BenchmarkData.BuildHeader(16384);
    private static readonly byte[] _header32K = BenchmarkData.BuildHeader(32768);

    private ReadOnlyMemory<byte> _rom1K;
    private ReadOnlyMemory<byte> _rom4K;
    private ReadOnlyMemory<byte> _rom16K;
    private ReadOnlyMemory<byte> _rom32K;

    private ReadOnlySequence<byte> _seg1K;
    private ReadOnlySequence<byte> _seg4K;
    private ReadOnlySequence<byte> _seg16K;
    private ReadOnlySequence<byte> _seg32K;

    public FlexibleParserBenchmark()
    {
        _memory = _buffer.ToArray();

        _rom1K = _header1K;
        _rom4K = _header4K;
        _rom16K = _header16K;
        _rom32K = _header32K;

        _seg1K = BenchmarkData.ToThreeSegments(_header1K);
        _seg4K = BenchmarkData.ToThreeSegments(_header4K);
        _seg16K = BenchmarkData.ToThreeSegments(_header16K);
        _seg32K = BenchmarkData.ToThreeSegments(_header32K);
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

    // ---- Small: ROM / MultiSegment ----

    [Benchmark]
    public void Small_ROM()
    {
        _into.Reset();
        FlexibleParser.TryExtractFullHeaderReadOnlyMemory(ref _memory, _into.Source, out _);
    }

    [Benchmark]
    public void Small_MultiSegment()
    {
        _into.Reset();
        FlexibleParser.TryExtractFullHeader(ref _segmentedBuffer, _into.Source, out _);
    }

    // ---- 1KB ----

    [Benchmark]
    public void Header1K_ROM()
    {
        _into.Reset();
        FlexibleParser.TryExtractFullHeaderReadOnlyMemory(ref _rom1K, _into.Source, out _);
    }

    [Benchmark]
    public void Header1K_MultiSegment()
    {
        _into.Reset();
        FlexibleParser.TryExtractFullHeader(ref _seg1K, _into.Source, out _);
    }

    // ---- 4KB ----

    [Benchmark]
    public void Header4K_ROM()
    {
        _into.Reset();
        FlexibleParser.TryExtractFullHeaderReadOnlyMemory(ref _rom4K, _into.Source, out _);
    }

    [Benchmark]
    public void Header4K_MultiSegment()
    {
        _into.Reset();
        FlexibleParser.TryExtractFullHeader(ref _seg4K, _into.Source, out _);
    }

    // ---- 16KB ----

    //[Benchmark]
    public void Header16K_ROM()
    {
        _into.Reset();
        FlexibleParser.TryExtractFullHeaderReadOnlyMemory(ref _rom16K, _into.Source, out _);
    }

    //[Benchmark]
    public void Header16K_MultiSegment()
    {
        _into.Reset();
        FlexibleParser.TryExtractFullHeader(ref _seg16K, _into.Source, out _);
    }

    // ---- 32KB ----

    //[Benchmark]
    public void Header32K_ROM()
    {
        _into.Reset();
        FlexibleParser.TryExtractFullHeaderReadOnlyMemory(ref _rom32K, _into.Source, out _);
    }

    //[Benchmark]
    public void Header32K_MultiSegment()
    {
        _into.Reset();
        FlexibleParser.TryExtractFullHeader(ref _seg32K, _into.Source, out _);
    }
}
