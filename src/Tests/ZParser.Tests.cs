using System.Buffers;
using System.Text;
using Glyph11.Protocol;
using Glyph11.Parser.ZParser;
using Glyph11.Utils;

namespace Tests;

public class ZParserTests : IDisposable
{
    private readonly BinaryRequest _request = new();

    public void Dispose() => _request.Dispose();

    #region Helpers

    private (bool success, int bytesRead) Parse(string raw, bool multiSegment)
    {
        var bytes = Encoding.ASCII.GetBytes(raw);

        if (multiSegment)
        {
            var seq = SplitIntoSegments(bytes);
            return (ZParser.TryExtractFullHeader(ref seq, _request, out var b), b);
        }

        ReadOnlyMemory<byte> rom = bytes;
        return (ZParser.TryExtractFullHeaderROM(ref rom, _request, out var b2), b2);
    }

    private static ReadOnlySequence<byte> SplitIntoSegments(byte[] data)
    {
        if (data.Length < 3)
        {
            var single = new BufferSegment(data);
            return new ReadOnlySequence<byte>(single, 0, single, single.Memory.Length);
        }

        int split1 = data.Length / 3;
        int split2 = 2 * data.Length / 3;

        var first = new BufferSegment(data[..split1]);
        var last = first.Append(data[split1..split2]).Append(data[split2..]);

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private static void AssertHeader(KeyValueList headers, int index, string expectedKey, string expectedValue)
    {
        var kv = headers[index];
        AssertAscii.Equal(expectedKey, kv.Key);
        AssertAscii.Equal(expectedValue, kv.Value);
    }

    private static void AssertQueryParam(KeyValueList query, int index, string expectedKey, string expectedValue)
    {
        var kv = query[index];
        AssertAscii.Equal(expectedKey, kv.Key);
        AssertAscii.Equal(expectedValue, kv.Value);
    }

    #endregion

    // ================================================================
    // Incomplete requests — returns false (ZParser never throws)
    // ================================================================

    #region Incomplete requests

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReturnsFalse_EmptyInput(bool multi)
    {
        var (ok, _) = Parse("", multi);
        Assert.False(ok);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReturnsFalse_MissingDoubleCrlfTerminator(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\nHost: x\r\n", multi);
        Assert.False(ok);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReturnsFalse_OnlyRequestLine(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\n", multi);
        Assert.False(ok);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReturnsFalse_BareLineFeedInsteadOfCrlf(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\nHost: x\n\n", multi);
        Assert.False(ok);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReturnsFalse_RequestLineWithNoSpaces(bool multi)
    {
        var (ok, _) = Parse("INVALIDLINE\r\n\r\n", multi);
        Assert.False(ok);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReturnsFalse_RequestLineWithSingleSpace(bool multi)
    {
        var (ok, _) = Parse("GET /path\r\n\r\n", multi);
        Assert.False(ok);
    }

    #endregion

    // ================================================================
    // Method parsing
    // ================================================================

    #region Method parsing

    [Theory]
    [InlineData("GET", false), InlineData("GET", true)]
    [InlineData("POST", false), InlineData("POST", true)]
    [InlineData("PUT", false), InlineData("PUT", true)]
    [InlineData("DELETE", false), InlineData("DELETE", true)]
    [InlineData("HEAD", false), InlineData("HEAD", true)]
    [InlineData("OPTIONS", false), InlineData("OPTIONS", true)]
    [InlineData("PATCH", false), InlineData("PATCH", true)]
    [InlineData("TRACE", false), InlineData("TRACE", true)]
    public void ParsesHttpMethod(string method, bool multi)
    {
        var (ok, _) = Parse($"{method} / HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        AssertAscii.Equal(method, _request.Method);
    }

    #endregion

    // ================================================================
    // Path parsing
    // ================================================================

    #region Path parsing

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesRootPath(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        AssertAscii.Equal("/", _request.Path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesNestedPath(bool multi)
    {
        var (ok, _) = Parse("GET /api/v1/users HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        AssertAscii.Equal("/api/v1/users", _request.Path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeparatesPathFromQueryString(bool multi)
    {
        var (ok, _) = Parse("GET /search?q=test HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        AssertAscii.Equal("/search", _request.Path);
    }

    #endregion

    // ================================================================
    // Query string parsing
    // ================================================================

    #region Query string parsing

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesSingleQueryParameter(bool multi)
    {
        var (ok, _) = Parse("GET /p?key=val HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        Assert.Equal(1, _request.QueryParameters.Count);
        AssertQueryParam(_request.QueryParameters, 0, "key", "val");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesMultipleQueryParameters(bool multi)
    {
        var (ok, _) = Parse("GET /p?a=1&b=2&c=3 HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        Assert.Equal(3, _request.QueryParameters.Count);
        AssertQueryParam(_request.QueryParameters, 0, "a", "1");
        AssertQueryParam(_request.QueryParameters, 1, "b", "2");
        AssertQueryParam(_request.QueryParameters, 2, "c", "3");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesQueryParameterWithEmptyValue(bool multi)
    {
        var (ok, _) = Parse("GET /p?key= HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        Assert.Equal(1, _request.QueryParameters.Count);
        AssertQueryParam(_request.QueryParameters, 0, "key", "");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoQueryParams_TrailingQuestionMarkOnly(bool multi)
    {
        var (ok, _) = Parse("GET /p? HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        AssertAscii.Equal("/p", _request.Path);
        Assert.Equal(0, _request.QueryParameters.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SkipsQueryParamWithoutEqualsSign(bool multi)
    {
        var (ok, _) = Parse("GET /p?ok=1&bad&also=2 HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        Assert.Equal(2, _request.QueryParameters.Count);
        AssertQueryParam(_request.QueryParameters, 0, "ok", "1");
        AssertQueryParam(_request.QueryParameters, 1, "also", "2");
    }

    #endregion

    // ================================================================
    // Header field parsing
    // ================================================================

    #region Header field parsing

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesSingleHeader(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\nHost: localhost\r\n\r\n", multi);
        Assert.True(ok);
        Assert.Equal(1, _request.Headers.Count);
        AssertHeader(_request.Headers, 0, "Host", "localhost");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesMultipleHeaders(bool multi)
    {
        var raw =
            "GET / HTTP/1.1\r\n" +
            "Host: localhost\r\n" +
            "Content-Type: text/html\r\n" +
            "Accept: */*\r\n" +
            "\r\n";

        var (ok, _) = Parse(raw, multi);
        Assert.True(ok);
        Assert.Equal(3, _request.Headers.Count);
        AssertHeader(_request.Headers, 0, "Host", "localhost");
        AssertHeader(_request.Headers, 1, "Content-Type", "text/html");
        AssertHeader(_request.Headers, 2, "Accept", "*/*");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TrimsLeadingSpacesFromHeaderValue(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\nKey:   value\r\n\r\n", multi);
        Assert.True(ok);
        AssertHeader(_request.Headers, 0, "Key", "value");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TrimsLeadingTabFromHeaderValue(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\nKey:\tvalue\r\n\r\n", multi);
        Assert.True(ok);
        AssertHeader(_request.Headers, 0, "Key", "value");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TrimsLeadingMixedWhitespaceFromHeaderValue(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\nKey: \t \tvalue\r\n\r\n", multi);
        Assert.True(ok);
        AssertHeader(_request.Headers, 0, "Key", "value");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreservesInternalWhitespaceInHeaderValue(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\nX-Data: hello world\r\n\r\n", multi);
        Assert.True(ok);
        AssertHeader(_request.Headers, 0, "X-Data", "hello world");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesHeaderValueContainingColon(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\nHost: localhost:8080\r\n\r\n", multi);
        Assert.True(ok);
        AssertHeader(_request.Headers, 0, "Host", "localhost:8080");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesEmptyHeaderValue(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\nX-Empty:\r\n\r\n", multi);
        Assert.True(ok);
        Assert.Equal(1, _request.Headers.Count);
        AssertHeader(_request.Headers, 0, "X-Empty", "");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesRequestWithNoHeaders(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        Assert.Equal(0, _request.Headers.Count);
    }

    #endregion

    // ================================================================
    // Bytes consumed
    // ================================================================

    #region Bytes consumed

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReportsCorrectBytesConsumed(bool multi)
    {
        var raw = "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n";
        var (ok, bytesRead) = Parse(raw, multi);
        Assert.True(ok);
        Assert.Equal(raw.Length - 1, bytesRead);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BytesConsumedExcludesBody(bool multi)
    {
        var header = "POST / HTTP/1.1\r\nHost: localhost\r\n\r\n";
        var raw = header + "BodyContent";
        var (ok, bytesRead) = Parse(raw, multi);
        Assert.True(ok);
        Assert.Equal(header.Length - 1, bytesRead);
    }

    #endregion

    // ================================================================
    // Full realistic request
    // ================================================================

    #region Full realistic request

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesCompleteRealisticRequest(bool multi)
    {
        var header =
            "POST /api/users?page=1&limit=50 HTTP/1.1\r\n" +
            "Host: example.com\r\n" +
            "Content-Type: application/json\r\n" +
            "Content-Length: 27\r\n" +
            "Authorization: Bearer tok123\r\n" +
            "Accept: application/json\r\n" +
            "\r\n";

        var raw = header + "{\"name\":\"test\"}";

        var (ok, bytesRead) = Parse(raw, multi);
        Assert.True(ok);

        AssertAscii.Equal("POST", _request.Method);
        AssertAscii.Equal("/api/users", _request.Path);

        Assert.Equal(2, _request.QueryParameters.Count);
        AssertQueryParam(_request.QueryParameters, 0, "page", "1");
        AssertQueryParam(_request.QueryParameters, 1, "limit", "50");

        Assert.Equal(5, _request.Headers.Count);
        AssertHeader(_request.Headers, 0, "Host", "example.com");
        AssertHeader(_request.Headers, 1, "Content-Type", "application/json");
        AssertHeader(_request.Headers, 2, "Content-Length", "27");
        AssertHeader(_request.Headers, 3, "Authorization", "Bearer tok123");
        AssertHeader(_request.Headers, 4, "Accept", "application/json");

        Assert.Equal(header.Length - 1, bytesRead);
    }

    #endregion

    // ================================================================
    // Dispatch
    // ================================================================

    #region Dispatch

    [Fact]
    public void DispatchRoutesSingleSegmentToROM()
    {
        var bytes = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n");
        var seq = new ReadOnlySequence<byte>(bytes);
        Assert.True(seq.IsSingleSegment);

        var ok = ZParser.TryExtractFullHeader(ref seq, _request, out _);
        Assert.True(ok);
        AssertAscii.Equal("GET", _request.Method);
    }

    [Fact]
    public void DispatchRoutesMultiSegmentToLinearized()
    {
        var bytes = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n");
        var seq = SplitIntoSegments(bytes);
        Assert.False(seq.IsSingleSegment);

        var ok = ZParser.TryExtractFullHeader(ref seq, _request, out _);
        Assert.True(ok);
        AssertAscii.Equal("GET", _request.Method);
    }

    #endregion
}
