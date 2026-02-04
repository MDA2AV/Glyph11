using System.Buffers;
using System.Text;
using GinHTTP.Protocol;
using Glyph11.ProprietaryCollections;
using Parser11 = Glyph11.Parser.Parser11;

namespace Tests;

public class Parser11TryExtractFullHeader_ROM
{
    private const string ExpectedPath = "/route";

    [Fact]
    public void ParseSingleSegmentRequest()
    {
        var request =
            "GET /route?p1=1&p2=2&p3=3&p4=4 HTTP/1.1\r\n" +
            "Content-Length: 100\r\n" +
            "Server: GinHTTP\r\n" +
            "\r\n";

        ReadOnlyMemory<byte> rom = Encoding.ASCII.GetBytes(request);

        var data = new Request();
        int position = 0;

        var parsed = Parser11.TryExtractFullHeaderSingleSegment(ref rom, data.Binary, ref position);

        Assert.True(parsed);
        AssertRequestParsedCorrectly(data);

        // Verify consumed exactly the header bytes
        Assert.Equal(rom.Length, position);
    }

    [Fact]
    public void ParseMultiSegmentRequest()
    {
        ReadOnlySequence<byte> segmented = CreateMultiSegment();

        var data = new Request();
        int position = 0;

        var parsed = Parser11.TryExtractFullHeaderMultiSegment(ref segmented, data.Binary, ref position);

        Assert.True(parsed);
        AssertRequestParsedCorrectly(data);

        // If your multi-seg parser uses "position" as bytes-consumed:
        Assert.Equal((int)segmented.Length, position);
    }

    private static void AssertRequestParsedCorrectly(Request data)
    {
        // Method (enum + raw bytes)
        Assert.Equal(RequestMethod.Get, data.Method);
        AssertAscii.Equal("GET", data.Binary.Method);

        // Route (path only)
        AssertAscii.Equal(ExpectedPath, data.Binary.Route);

        // Query params
        var qp = (PooledKeyValueList)data.Binary.QueryParameters;
        Assert.Equal(4, qp.Count);

        AssertKeyValue(qp, "p1", "1");
        AssertKeyValue(qp, "p2", "2");
        AssertKeyValue(qp, "p3", "3");
        AssertKeyValue(qp, "p4", "4");

        // Headers
        var headers = (PooledKeyValueList)data.Binary.Headers;
        Assert.Equal(2, headers.Count);

        AssertKeyValue(headers, "Content-Length", "100");
        AssertKeyValue(headers, "Server", "GinHTTP");
    }

    private static void AssertKeyValue(PooledKeyValueList list, string expectedKey, string expectedValue)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var kv = list[i];

            // KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
            var key = kv.Key;
            if (AsciiEquals(key.Span, expectedKey))
            {
                AssertAscii.Equal(expectedValue, kv.Value);
                return;
            }
        }

        Assert.True(false, $"Missing key '{expectedKey}'");
    }

    private static bool AsciiEquals(ReadOnlySpan<byte> bytes, string ascii)
    {
        // avoid Encoding allocation for key compares
        if (bytes.Length != ascii.Length) return false;
        for (int i = 0; i < bytes.Length; i++)
            if (bytes[i] != (byte)ascii[i])
                return false;
        return true;
    }

    private static ReadOnlySequence<byte> CreateMultiSegment()
    {
        var seg1 = "GET /route?p1=1&p2=2&p3=3&p4=4 HT"u8.ToArray();
        var seg2 = "TP/1.1\r\nContent-Length: 100\r\nServer: "u8.ToArray();
        var seg3 = "GinHTTP\r\n\r\n"u8.ToArray();

        var first = new Glyph11.Utils.BufferSegment(seg1);
        var last = first.Append(seg2).Append(seg3);

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }
}

static class AssertAscii
{
    public static void Equal(string expectedAscii, ReadOnlyMemory<byte> actual)
        => Assert.Equal(expectedAscii, Encoding.ASCII.GetString(actual.Span));

    public static void Equal(string expectedAscii, ReadOnlySpan<byte> actual)
        => Assert.Equal(expectedAscii, Encoding.ASCII.GetString(actual));
}

























/*
    [Fact]
    public void ParseSingleSegmentRequest()
    {
        var request =
            "GET /route?p1=1&p2=2&p3=3&p4=4 HTTP/1.1\r\n" +
            "Content-Length: 100\r\n" +
            "Server: GinHTTP\r\n" +
            "\r\n";

        ReadOnlyMemory<byte> rom = Encoding.ASCII.GetBytes(request);

        var data = new Request();

        int position = 0;

        var parsed = Parser11.TryExtractFullHeaderSingleSegment(ref rom, data.Binary, ref position);

        Assert.True(parsed);
    }

    [Fact]
    public void ParseMultiSegmentRequest()
    {
        ReadOnlySequence<byte> _segmentedBuffer = CreateMultiSegment();

        var data = new Request();

        int position = 0;

        var parsed = Parser11.TryExtractFullHeaderMultiSegment(ref _segmentedBuffer, data.Binary, ref position);

        Console.WriteLine($"Method: {Encoding.UTF8.GetString(data.Binary.Method.ToArray())}");
        Console.WriteLine($"Route: {Encoding.UTF8.GetString(data.Binary.Route.ToArray())}");

        Assert.True(parsed);
    }

    private static ReadOnlySequence<byte> CreateMultiSegment()
    {
        var seg1 = "GET /route?p1=1&p2=2&p3=3&p4=4 HT"u8.ToArray();
        var seg2 = "TP/1.1\r\nContent-Length: 100\r\nServer: "u8.ToArray();
        var seg3 = "GinHTTP\r\n\r\n"u8.ToArray();

        var first = new Glyph11.Utils.BufferSegment(seg1);
        var last  = first.Append(seg2)
            .Append(seg3);

        return new ReadOnlySequence<byte>(
            first, 0,
            last,  last.Memory.Length);
    }
    */