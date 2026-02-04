using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GinHTTP.Protocol;
using Glyph11;

namespace Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<Parser>();
    }
}

[MemoryDiagnoser]
public class Parser
{
    private readonly Request _into = new();

    private readonly ReadOnlySequence<byte> _buffer =
        new("GET / HTTP/1.1\r\nHost: test\r\n\r\n"u8.ToArray());

    private ReadOnlyMemory<byte> _memory;
    private int _i;

    public Parser()
    {
        _memory = _buffer.ToArray();
        _i = 0;
    }

    [Benchmark]
    public void BenchmarkParser()
    {
        _i = 0; // IMPORTANT: reset position per-iteration
        Parser11.TryExtractFullHeader(ref _memory, _into, ref _i);
    }
}