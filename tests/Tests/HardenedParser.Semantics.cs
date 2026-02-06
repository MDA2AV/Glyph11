using Glyph11;
using Glyph11.Parser.Hardened;
using Glyph11.Validation;

namespace Tests;

public partial class HardenedParserTests
{
    // ================================================================
    // RequestSemantics — post-parse checks
    // ================================================================

    // ---- HasConflictingContentLength ----

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

    // ---- HasTransferEncodingWithContentLength ----

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

    // ---- HasDotSegments ----

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

    // ---- HasInvalidHostHeaderCount ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_ValidHostCount(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nHost: example.com\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasInvalidHostHeaderCount(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_MissingHost(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nAccept: */*\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasInvalidHostHeaderCount(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_DuplicateHost(bool multi)
    {
        var raw = "GET / HTTP/1.1\r\nHost: a.com\r\nHost: b.com\r\n\r\n";
        Parse(raw, multi);
        Assert.True(RequestSemantics.HasInvalidHostHeaderCount(_request));
    }

    // ---- HasInvalidContentLengthFormat ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_ValidContentLength(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length: 42\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasInvalidContentLengthFormat(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NonDigitContentLength(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length: abc\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasInvalidContentLengthFormat(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_EmptyContentLength(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length:\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasInvalidContentLengthFormat(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_ContentLengthWithSpaces(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length: 1 2\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasInvalidContentLengthFormat(_request));
    }

    // ---- HasContentLengthWithLeadingZeros ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoLeadingZeros(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length: 200\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasContentLengthWithLeadingZeros(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_LeadingZeros(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length: 0200\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasContentLengthWithLeadingZeros(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_SingleZeroIsValid(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length: 0\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasContentLengthWithLeadingZeros(_request));
    }

    // ---- HasConflictingCommaSeparatedContentLength ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_CommaSeparatedCL_Same(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length: 42, 42\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasConflictingCommaSeparatedContentLength(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_CommaSeparatedCL_Different(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nContent-Length: 42, 0\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasConflictingCommaSeparatedContentLength(_request));
    }

    // ---- HasFragmentInRequestTarget ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoFragment(bool multi)
    {
        Parse("GET /path HTTP/1.1\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasFragmentInRequestTarget(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_HasFragment(bool multi)
    {
        Parse("GET /path#frag HTTP/1.1\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasFragmentInRequestTarget(_request));
    }

    // ---- HasBackslashInPath ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoBackslash(bool multi)
    {
        Parse("GET /api/users HTTP/1.1\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasBackslashInPath(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_HasBackslash(bool multi)
    {
        Parse("GET /api\\..\\etc HTTP/1.1\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasBackslashInPath(_request));
    }

    // ---- HasDoubleEncoding ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoDoubleEncoding(bool multi)
    {
        Parse("GET /api/%2e%2e HTTP/1.1\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasDoubleEncoding(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_HasDoubleEncoding(bool multi)
    {
        Parse("GET /api/%252e%252e HTTP/1.1\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasDoubleEncoding(_request));
    }

    // ---- HasEncodedNullByte ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoEncodedNull(bool multi)
    {
        Parse("GET /file.txt HTTP/1.1\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasEncodedNullByte(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_HasEncodedNull(bool multi)
    {
        Parse("GET /file.txt%00.jpg HTTP/1.1\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasEncodedNullByte(_request));
    }

    // ---- HasOverlongUtf8 ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoOverlongUtf8(bool multi)
    {
        Parse("GET /api/users HTTP/1.1\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasOverlongUtf8(_request));
    }

    [Fact]
    public void Semantics_HasOverlongUtf8_C0_RejectedByParser()
    {
        // 0xC0 0xAF is overlong encoding of '/' — now rejected at parse time (non-ASCII in request-target)
        var header = "GET "u8.ToArray();
        var path = new byte[] { 0x2F, 0xC0, 0xAF };
        var tail = " HTTP/1.1\r\n\r\n"u8.ToArray();
        var all = new byte[header.Length + path.Length + tail.Length];
        header.CopyTo(all, 0);
        path.CopyTo(all, header.Length);
        tail.CopyTo(all, header.Length + path.Length);

        ReadOnlyMemory<byte> rom = all;
        Assert.Throws<HttpParseException>(
            () => HardenedParser.TryExtractFullHeaderROM(ref rom, _request, Defaults, out _));
    }

    // ---- HasInvalidTransferEncoding ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_ValidTE_Chunked(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasInvalidTransferEncoding(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_InvalidTE_Obfuscated(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nTransfer-Encoding: xchunked\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasInvalidTransferEncoding(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_InvalidTE_Quoted(bool multi)
    {
        // "chunked" (quoted) is not valid
        Parse("GET / HTTP/1.1\r\nTransfer-Encoding: \"chunked\"\r\n\r\n", multi);
        Assert.True(RequestSemantics.HasInvalidTransferEncoding(_request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Semantics_NoTE_NotInvalid(bool multi)
    {
        Parse("GET / HTTP/1.1\r\nHost: x\r\n\r\n", multi);
        Assert.False(RequestSemantics.HasInvalidTransferEncoding(_request));
    }
}
