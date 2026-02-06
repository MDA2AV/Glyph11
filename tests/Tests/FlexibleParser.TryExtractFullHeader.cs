using System.Buffers;
using System.Text;
using GenHTTP.Protocol;
using GenHTTP.Types;
using Glyph11.Parser.FlexibleParser;
using Glyph11.Protocol;

namespace Tests;

public class FlexibleParserTryExtractFullHeader_ROM
{
    private const string ExpectedPath = "/route";

    [Fact]
    public void ParseSingleSegmentRequest()
    {
        var request =
            "GET /route?p1=1&p2=2&p3=3&p4=4 HTTP/1.1\r\n" +
            "Content-Length: 100\r\n" +
            "Server: GenHTTP\r\n" +
            "\r\n";

        ReadOnlyMemory<byte> rom = Encoding.ASCII.GetBytes(request);

        var data = new Request();

        var parsed = FlexibleParser.TryExtractFullHeaderReadOnlyMemory(ref rom, data.Source, out var position);

        Assert.True(parsed);
        AssertRequestParsedCorrectly(data);

        // Verify consumed exactly the header bytes
        Assert.Equal(rom.Length - 1, position);
    }

    [Fact]
    public void ParseMultiSegmentRequest()
    {
        ReadOnlySequence<byte> segmented = CreateMultiSegment();

        var data = new Request();

        var parsed = FlexibleParser.TryExtractFullHeader(ref segmented, data.Source, out var position);

        Assert.True(parsed);
        AssertRequestParsedCorrectly(data);

        Assert.Equal((int)segmented.Length - 1, position);
    }

    private static void AssertRequestParsedCorrectly(Request data)
    {
        // Method (enum + raw bytes)
        Assert.Equal(RequestMethod.Get, data.Method);
        AssertAscii.Equal("GET", data.Source.Method);

        // Route (path only)
        AssertAscii.Equal(ExpectedPath, data.Source.Path);

        // Query params
        var qp = data.Source.QueryParameters;
        Assert.Equal(4, qp.Count);

        AssertKeyValue(qp, "p1", "1");
        AssertKeyValue(qp, "p2", "2");
        AssertKeyValue(qp, "p3", "3");
        AssertKeyValue(qp, "p4", "4");

        // Headers
        var headers = data.Source.Headers;
        Assert.Equal(2, headers.Count);

        AssertKeyValue(headers, "Content-Length", "100");
        AssertKeyValue(headers, "Server", "GenHTTP");
    }

    private static void AssertKeyValue(KeyValueList list, string expectedKey, string expectedValue)
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

        Assert.Fail($"Missing key '{expectedKey}'");
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
        var seg3 = "GenHTTP\r\n\r\n"u8.ToArray();

        var first = new Glyph11.Utils.BufferSegment(seg1);
        var last = first.Append(seg2).Append(seg3);

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }
}
