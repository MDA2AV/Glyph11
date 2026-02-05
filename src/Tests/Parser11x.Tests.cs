using System.Buffers;
using System.Text;
using Glyph11.Protocol;
using Glyph11.Parser;
using Glyph11.Utils;
using Glyph11.Validation;

namespace Tests;

/// <summary>
/// Tests for Parser11x (security-hardened parser) and RequestSemantics.
/// Each parsing test runs against both ROM and ROS paths via [Theory].
/// </summary>
public class Parser11xTests : IDisposable
{
    private readonly BinaryRequest _request = new();
    private static readonly ParserLimits Defaults = ParserLimits.Default;

    public void Dispose() => _request.Dispose();

    #region Helpers

    private (bool success, int bytesRead) Parse(string raw, bool multiSegment)
        => Parse(raw, multiSegment, Defaults);

    private (bool success, int bytesRead) Parse(string raw, bool multiSegment, ParserLimits limits)
    {
        var bytes = Encoding.ASCII.GetBytes(raw);

        if (multiSegment)
        {
            var seq = SplitIntoSegments(bytes);
            return (Parser11x.TryExtractFullHeaderROS(ref seq, _request, in limits, out var b), b);
        }

        ReadOnlyMemory<byte> rom = bytes;
        return (Parser11x.TryExtractFullHeaderROM(ref rom, _request, in limits, out var b2), b2);
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
    // Functional parity with Parser11
    // ================================================================

    #region Incomplete requests — returns false

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

    #endregion

    #region Invalid request lines — throws

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_RequestLineWithNoSpaces(bool multi)
    {
        Assert.Throws<InvalidOperationException>(
            () => Parse("INVALIDLINE\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_RequestLineWithSingleSpace(bool multi)
    {
        Assert.Throws<InvalidOperationException>(
            () => Parse("GET /path\r\n\r\n", multi));
    }

    #endregion

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
    // HTTP version validation
    // ================================================================

    #region Version validation

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StoresHttpVersion(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.1\r\n\r\n", multi);
        Assert.True(ok);
        AssertAscii.Equal("HTTP/1.1", _request.Version);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcceptsHttp10(bool multi)
    {
        var (ok, _) = Parse("GET / HTTP/1.0\r\n\r\n", multi);
        Assert.True(ok);
        AssertAscii.Equal("HTTP/1.0", _request.Version);
    }

    [Theory]
    [InlineData("HTTP/2.0", false), InlineData("HTTP/2.0", true)]
    [InlineData("HTTP/9.9", false), InlineData("HTTP/9.9", true)]
    public void AcceptsAnyValidHttpVersionDigits(string version, bool multi)
    {
        var (ok, _) = Parse($"GET / {version}\r\n\r\n", multi);
        Assert.True(ok);
        AssertAscii.Equal(version, _request.Version);
    }

    [Theory]
    [InlineData("http/1.1", false), InlineData("http/1.1", true)]
    [InlineData("HTTP/1.1.1", false), InlineData("HTTP/1.1.1", true)]
    [InlineData("HTTP/1.", false), InlineData("HTTP/1.", true)]
    [InlineData("HTTP/11", false), InlineData("HTTP/11", true)]
    [InlineData("XTTP/1.1", false), InlineData("XTTP/1.1", true)]
    [InlineData("HTTP1.1", false), InlineData("HTTP1.1", true)]
    [InlineData("1.1", false), InlineData("1.1", true)]
    public void Throws_InvalidHttpVersion(string version, bool multi)
    {
        Assert.Throws<InvalidOperationException>(
            () => Parse($"GET / {version}\r\n\r\n", multi));
    }

    #endregion

    // ================================================================
    // Token validation (method + header name)
    // ================================================================

    #region Token validation

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_MethodWithSpace(bool multi)
    {
        // "G T" as method — space is not a token character
        Assert.Throws<InvalidOperationException>(
            () => Parse("G\x01T / HTTP/1.1\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_MethodWithControlChar(bool multi)
    {
        Assert.Throws<InvalidOperationException>(
            () => Parse("GE\x01T / HTTP/1.1\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderNameWithControlChar(bool multi)
    {
        Assert.Throws<InvalidOperationException>(
            () => Parse("GET / HTTP/1.1\r\nBad\x00Name: val\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderNameWithAtSign(bool multi)
    {
        // '@' (0x40) is not a token character
        Assert.Throws<InvalidOperationException>(
            () => Parse("GET / HTTP/1.1\r\nBad@Name: val\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcceptsValidTokenCharsInHeaderName(bool multi)
    {
        // Token chars include ! # $ % & ' * + - . ^ _ ` | ~
        var (ok, _) = Parse("GET / HTTP/1.1\r\nX-Custom_Header.Name: val\r\n\r\n", multi);
        Assert.True(ok);
        AssertHeader(_request.Headers, 0, "X-Custom_Header.Name", "val");
    }

    #endregion

    // ================================================================
    // Field-value validation (header value)
    // ================================================================

    #region Field-value validation

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderValueWithNullByte(bool multi)
    {
        Assert.Throws<InvalidOperationException>(
            () => Parse("GET / HTTP/1.1\r\nKey: val\x00ue\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderValueWithDEL(bool multi)
    {
        Assert.Throws<InvalidOperationException>(
            () => Parse("GET / HTTP/1.1\r\nKey: val\x7Fue\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcceptsTabInHeaderValue(bool multi)
    {
        // HTAB is allowed as OWS in field-value
        var (ok, _) = Parse("GET / HTTP/1.1\r\nKey: val\tue\r\n\r\n", multi);
        Assert.True(ok);
        AssertHeader(_request.Headers, 0, "Key", "val\tue");
    }

    #endregion

    // ================================================================
    // Malformed header line rejection
    // ================================================================

    #region Malformed header lines

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderLineWithoutColon(bool multi)
    {
        // Parser11 silently skips these; Parser11x throws
        Assert.Throws<InvalidOperationException>(() =>
            Parse("GET / HTTP/1.1\r\nnocolonhere\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderLineWithEmptyName(bool multi)
    {
        // ":value" — colon at position 0
        Assert.Throws<InvalidOperationException>(() =>
            Parse("GET / HTTP/1.1\r\n:value\r\n\r\n", multi));
    }

    #endregion

    // ================================================================
    // Resource limits
    // ================================================================

    #region Limits — header count

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_WhenHeaderCountExceedsLimit(bool multi)
    {
        var limits = Defaults with { MaxHeaderCount = 2 };
        var raw =
            "GET / HTTP/1.1\r\n" +
            "H1: v1\r\n" +
            "H2: v2\r\n" +
            "H3: v3\r\n" +
            "\r\n";

        Assert.Throws<InvalidOperationException>(
            () => Parse(raw, multi, limits));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcceptsExactlyMaxHeaderCount(bool multi)
    {
        var limits = Defaults with { MaxHeaderCount = 2 };
        var raw =
            "GET / HTTP/1.1\r\n" +
            "H1: v1\r\n" +
            "H2: v2\r\n" +
            "\r\n";

        var (ok, _) = Parse(raw, multi, limits);
        Assert.True(ok);
        Assert.Equal(2, _request.Headers.Count);
    }

    #endregion

    #region Limits — header name length

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_WhenHeaderNameExceedsLimit(bool multi)
    {
        var limits = Defaults with { MaxHeaderNameLength = 4 };
        var raw = "GET / HTTP/1.1\r\nLongName: val\r\n\r\n";

        Assert.Throws<InvalidOperationException>(
            () => Parse(raw, multi, limits));
    }

    #endregion

    #region Limits — header value length

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_WhenHeaderValueExceedsLimit(bool multi)
    {
        var limits = Defaults with { MaxHeaderValueLength = 3 };
        var raw = "GET / HTTP/1.1\r\nKey: longvalue\r\n\r\n";

        Assert.Throws<InvalidOperationException>(
            () => Parse(raw, multi, limits));
    }

    #endregion

    #region Limits — method length

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_WhenMethodExceedsLimit(bool multi)
    {
        var limits = Defaults with { MaxMethodLength = 3 };
        var raw = "POST / HTTP/1.1\r\n\r\n";

        Assert.Throws<InvalidOperationException>(
            () => Parse(raw, multi, limits));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcceptsMethodAtExactLimit(bool multi)
    {
        var limits = Defaults with { MaxMethodLength = 3 };
        var raw = "GET / HTTP/1.1\r\n\r\n";

        var (ok, _) = Parse(raw, multi, limits);
        Assert.True(ok);
    }

    #endregion

    #region Limits — URL length

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_WhenUrlExceedsLimit(bool multi)
    {
        var limits = Defaults with { MaxUrlLength = 5 };
        var raw = "GET /toolong HTTP/1.1\r\n\r\n";

        Assert.Throws<InvalidOperationException>(
            () => Parse(raw, multi, limits));
    }

    #endregion

    #region Limits — query parameter count

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_WhenQueryParamCountExceedsLimit(bool multi)
    {
        var limits = Defaults with { MaxQueryParameterCount = 2 };
        var raw = "GET /p?a=1&b=2&c=3 HTTP/1.1\r\n\r\n";

        Assert.Throws<InvalidOperationException>(
            () => Parse(raw, multi, limits));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcceptsExactlyMaxQueryParamCount(bool multi)
    {
        var limits = Defaults with { MaxQueryParameterCount = 2 };
        var raw = "GET /p?a=1&b=2 HTTP/1.1\r\n\r\n";

        var (ok, _) = Parse(raw, multi, limits);
        Assert.True(ok);
        Assert.Equal(2, _request.QueryParameters.Count);
    }

    #endregion

    #region Limits — total header bytes

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_WhenTotalHeaderBytesExceedsLimit(bool multi)
    {
        var limits = Defaults with { MaxTotalHeaderBytes = 20 };
        var raw = "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n";

        Assert.Throws<InvalidOperationException>(
            () => Parse(raw, multi, limits));
    }

    #endregion

    // ================================================================
    // Entry point dispatch
    // ================================================================

    #region Dispatch

    [Fact]
    public void DispatchRoutesSingleSegmentToROM()
    {
        var bytes = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n");
        var seq = new ReadOnlySequence<byte>(bytes);
        Assert.True(seq.IsSingleSegment);

        var ok = Parser11x.TryExtractFullHeader(ref seq, _request, Defaults, out _);
        Assert.True(ok);
        AssertAscii.Equal("GET", _request.Method);
    }

    [Fact]
    public void DispatchRoutesMultiSegmentToROS()
    {
        var bytes = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n");
        var seq = SplitIntoSegments(bytes);
        Assert.False(seq.IsSingleSegment);

        var ok = Parser11x.TryExtractFullHeader(ref seq, _request, Defaults, out _);
        Assert.True(ok);
        AssertAscii.Equal("GET", _request.Method);
    }

    #endregion

    // ================================================================
    // ParserLimits
    // ================================================================

    #region ParserLimits

    [Fact]
    public void DefaultLimitsHaveExpectedValues()
    {
        var d = ParserLimits.Default;
        Assert.Equal(100, d.MaxHeaderCount);
        Assert.Equal(256, d.MaxHeaderNameLength);
        Assert.Equal(8192, d.MaxHeaderValueLength);
        Assert.Equal(8192, d.MaxUrlLength);
        Assert.Equal(128, d.MaxQueryParameterCount);
        Assert.Equal(16, d.MaxMethodLength);
        Assert.Equal(32768, d.MaxTotalHeaderBytes);
    }

    [Fact]
    public void LimitsCanBeCustomizedWithWith()
    {
        var custom = ParserLimits.Default with { MaxHeaderCount = 50 };
        Assert.Equal(50, custom.MaxHeaderCount);
        Assert.Equal(256, custom.MaxHeaderNameLength); // unchanged
    }

    #endregion

    // ================================================================
    // RequestSemantics
    // ================================================================

    #region HasConflictingContentLength

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoConflict_SingleContentLength(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length: 10\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasConflictingContentLength(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoConflict_DuplicateSameValue(bool multi)
    {
        var raw =
            "GET / HTTP/1.1\r\n" +
            "Content-Length: 10\r\n" +
            "Content-Length: 10\r\n" +
            "\r\n";
        Parse(raw, multi);
        Assert.False(RequestSemantics.HasConflictingContentLength(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_Conflict_DifferentContentLengths(bool multi)
    {
        var raw =
            "GET / HTTP/1.1\r\n" +
            "Content-Length: 10\r\n" +
            "Content-Length: 20\r\n" +
            "\r\n";
        Parse(raw, multi);
        Assert.True(RequestSemantics.HasConflictingContentLength(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_CaseInsensitiveContentLength(bool multi)
    {
        var raw =
            "GET / HTTP/1.1\r\n" +
            "content-length: 10\r\n" +
            "Content-Length: 20\r\n" +
            "\r\n";
        Parse(raw, multi);
        Assert.True(RequestSemantics.HasConflictingContentLength(_request));
    }

    #endregion

    #region HasTransferEncodingWithContentLength

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoSmuggling_OnlyCL(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length: 10\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasTransferEncodingWithContentLength(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoSmuggling_OnlyTE(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasTransferEncodingWithContentLength(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_Smuggling_BothTEandCL(bool multi)
    {
        var raw =
            "GET / HTTP/1.1\r\n" +
            "Transfer-Encoding: chunked\r\n" +
            "Content-Length: 10\r\n" +
            "\r\n";
        Parse(raw, multi);
        Assert.True(RequestSemantics.HasTransferEncodingWithContentLength(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_CaseInsensitiveTE(bool multi)
    {
        var raw =
            "GET / HTTP/1.1\r\n" +
            "transfer-encoding: chunked\r\n" +
            "content-length: 10\r\n" +
            "\r\n";
        Parse(raw, multi);
        Assert.True(RequestSemantics.HasTransferEncodingWithContentLength(_request));
    }

    #endregion

    #region HasDotSegments

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoDotSegments_CleanPath(bool multi)
    {
        Parse("GET /api/v1/users HTTP/1.1\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasDotSegments(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_DotSegments_ParentTraversal(bool multi)
    {
        Parse("GET /api/../etc/passwd HTTP/1.1\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasDotSegments(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_DotSegments_CurrentDir(bool multi)
    {
        Parse("GET /api/./users HTTP/1.1\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasDotSegments(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_DotSegments_TrailingDotDot(bool multi)
    {
        Parse("GET /api/.. HTTP/1.1\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasDotSegments(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_DotSegments_TrailingDot(bool multi)
    {
        Parse("GET /api/. HTTP/1.1\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasDotSegments(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoDotSegments_DotsInFilename(bool multi)
    {
        Parse("GET /api/file.tar.gz HTTP/1.1\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasDotSegments(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoDotSegments_DotSuffix(bool multi)
    {
        // "/api/test..." is fine — not a dot segment
        Parse("GET /api/test... HTTP/1.1\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasDotSegments(_request));
    }

    #endregion
}
