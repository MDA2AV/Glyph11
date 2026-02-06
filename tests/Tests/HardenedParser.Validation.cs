using Glyph11;
using Glyph11.Parser.Hardened;

namespace Tests;

public partial class HardenedParserTests
{
    // ================================================================
    // HTTP version validation
    // ================================================================

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
    public void Throws_UnsupportedHttpVersion(string version, bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse($"GET / {version}\r\n\r\n", multi));
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
        Assert.Throws<HttpParseException>(
            () => Parse($"GET / {version}\r\n\r\n", multi));
    }

    // ================================================================
    // Token validation (method + header name)
    // ================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_MethodWithSpace(bool multi)
    {
        // "G T" as method — space is not a token character
        Assert.Throws<HttpParseException>(
            () => Parse("G\x01T / HTTP/1.1\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_MethodWithControlChar(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GE\x01T / HTTP/1.1\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderNameWithControlChar(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET / HTTP/1.1\r\nBad\x00Name: val\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderNameWithAtSign(bool multi)
    {
        // '@' (0x40) is not a token character
        Assert.Throws<HttpParseException>(
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

    // ================================================================
    // Field-value validation (header value)
    // ================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderValueWithNullByte(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET / HTTP/1.1\r\nKey: val\x00ue\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderValueWithDEL(bool multi)
    {
        Assert.Throws<HttpParseException>(
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

    // ================================================================
    // Malformed header line rejection
    // ================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderLineWithoutColon(bool multi)
    {
        // FlexibleParser silently skips these; HardenedParser throws
        Assert.Throws<HttpParseException>(() =>
            Parse("GET / HTTP/1.1\r\nnocolonhere\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_HeaderLineWithEmptyName(bool multi)
    {
        // ":value" — colon at position 0
        Assert.Throws<HttpParseException>(() =>
            Parse("GET / HTTP/1.1\r\n:value\r\n\r\n", multi));
    }

    // ================================================================
    // Bare LF rejection
    // ================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_BareLfInHeaderSection(bool multi)
    {
        // Mix bare LF with CRLF — should throw
        var bytes = "GET / HTTP/1.1\r\nHost: x\r\n"u8.ToArray();
        // Replace the CRLF between headers with bare LF: inject \n after Host line
        var raw = "GET / HTTP/1.1\r\nHost: x\nX-Bad: y\r\n\r\n";
        // The bare LF is at position after "Host: x" — but since \r\n\r\n won't be found
        // with bare LF, we need the actual header to end with \r\n\r\n
        // Construct bytes manually: request-line CRLF, header with bare LF, then CRLF CRLF
        var data = new byte[] { };
        var header = "GET / HTTP/1.1\r\n"u8.ToArray();
        var badLine = "Host: x\n"u8.ToArray(); // bare LF
        var goodLine = "X-Ok: y\r\n"u8.ToArray();
        var terminator = "\r\n"u8.ToArray();
        var all = new byte[header.Length + badLine.Length + goodLine.Length + terminator.Length];
        header.CopyTo(all, 0);
        badLine.CopyTo(all, header.Length);
        goodLine.CopyTo(all, header.Length + badLine.Length);
        terminator.CopyTo(all, header.Length + badLine.Length + goodLine.Length);

        // This contains \r\n\r\n at the end, so headerEnd will be found,
        // but the bare LF scan will catch the \n in "Host: x\n"
        ReadOnlyMemory<byte> rom = all;
        Assert.Throws<HttpParseException>(
            () => HardenedParser.TryExtractFullHeaderROM(ref rom, _request, Defaults, out _));
    }

    // ================================================================
    // Obs-fold rejection
    // ================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_ObsFoldWithSpace(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET / HTTP/1.1\r\nHost: x\r\n continued\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_ObsFoldWithTab(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET / HTTP/1.1\r\nHost: x\r\n\tcontinued\r\n\r\n", multi));
    }

    // ================================================================
    // Whitespace before colon rejection
    // ================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_SpaceBeforeColon(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET / HTTP/1.1\r\nHost : localhost\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_TabBeforeColon(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET / HTTP/1.1\r\nHost\t: localhost\r\n\r\n", multi));
    }

    // ================================================================
    // Multiple spaces in request line
    // ================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_DoubleSpaceAfterMethod(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET  / HTTP/1.1\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_DoubleSpaceBeforeVersion(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET /  HTTP/1.1\r\n\r\n", multi));
    }

    // ================================================================
    // Request-target control character rejection
    // ================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_NullByteInUrl(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET /path\x00evil HTTP/1.1\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_ControlCharInUrl(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET /path\x01evil HTTP/1.1\r\n\r\n", multi));
    }

    // ================================================================
    // Content-Length validation
    // ================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_NegativeContentLength(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET / HTTP/1.1\r\nContent-Length: -1\r\n\r\n", multi));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Throws_ContentLengthLeadingZeros(bool multi)
    {
        Assert.Throws<HttpParseException>(
            () => Parse("GET / HTTP/1.1\r\nContent-Length: 005\r\n\r\n", multi));
    }

    [Theory]
    [InlineData("0", false), InlineData("0", true)]
    [InlineData("42", false), InlineData("42", true)]
    public void AcceptsValidContentLength(string value, bool multi)
    {
        var (ok, _) = Parse($"GET / HTTP/1.1\r\nContent-Length: {value}\r\n\r\n", multi);
        Assert.True(ok);
        AssertHeader(_request.Headers, 0, "Content-Length", value);
    }
}
