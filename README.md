[![NuGet](https://img.shields.io/nuget/v/Glyph11.svg)](https://www.nuget.org/packages/Glyph11/)

# Glyph11

Glyph11 is a dependency free, low allocation HTTP/1.1 parser for C#. It does not rely on any specific network technology but can be used with any (such as `Socket`, `NetworkStream`, `PipeReader` or anything else).

> [!IMPORTANT]  
> This is an early stage development project which currently lacks conformity tests.

## Usage

```csharp
ReadOnlySequence<byte> buffer = ...; // e.g. read from pipe reader

IBinaryRequest request = ...; // a re-usable/poolable request implementation to parse into

if (Parser11.TryExtractFullHeader(ref buffer, request, out var bytesRead))
{
    // handle the request and access request.Path, .Body etc.
    // advance the reader by bytesRead
}
```

## Performance

 Method                       | Mean       | Error    | StdDev   | Gen0   | Allocated |
----------------------------- |-----------:|---------:|---------:|-------:|----------:|
 Request in Single Segment |   125.4 ns | 38.41 ns | 35.93 ns |      - |         - |
 Request in Multiple Segments  | 1,043.6 ns | 19.78 ns | 17.53 ns | 0.0725 |     456 B |
