using Glyph11;
using Glyph11.Parser.Hardened;

namespace Tests;

public partial class HardenedParserTests
{
    // ================================================================
    // StatusCode on HttpParseException
    // ================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MethodLimitThrows400(bool multi)
    {
        // Method exceeds MaxMethodLength (3) but total header size stays under MaxTotalHeaderBytes
        var limits = Defaults with { MaxMethodLength = 3 };
        var raw = "POST / HTTP/1.1\r\n\r\n";

        var ex = Assert.Throws<HttpParseException>(() => Parse(raw, multi, limits));
        Assert.Equal(400, ex.StatusCode);
        Assert.False(ex.IsLimitViolation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeaderNameLimitThrows431(bool multi)
    {
        var limits = Defaults with { MaxHeaderNameLength = 4 };
        var raw = "GET / HTTP/1.1\r\nLongHeaderName: val\r\n\r\n";

        var ex = Assert.Throws<HttpParseException>(() => Parse(raw, multi, limits));
        Assert.Equal(431, ex.StatusCode);
        Assert.True(ex.IsLimitViolation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UrlLimitThrows431(bool multi)
    {
        var limits = Defaults with { MaxUrlLength = 5 };
        var raw = "GET /toolong HTTP/1.1\r\n\r\n";

        var ex = Assert.Throws<HttpParseException>(() => Parse(raw, multi, limits));
        Assert.Equal(431, ex.StatusCode);
        Assert.True(ex.IsLimitViolation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TotalHeaderLimitThrows431(bool multi)
    {
        var limits = Defaults with { MaxTotalHeaderBytes = 20 };
        var raw = "GET / HTTP/1.1\r\nHost: localhost\r\n\r\n";

        var ex = Assert.Throws<HttpParseException>(() => Parse(raw, multi, limits));
        Assert.Equal(431, ex.StatusCode);
        Assert.True(ex.IsLimitViolation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeaderValueLimitThrows431(bool multi)
    {
        var limits = Defaults with { MaxHeaderValueLength = 3 };
        var raw = "GET / HTTP/1.1\r\nKey: longvalue\r\n\r\n";

        var ex = Assert.Throws<HttpParseException>(() => Parse(raw, multi, limits));
        Assert.Equal(431, ex.StatusCode);
        Assert.True(ex.IsLimitViolation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeaderCountLimitThrows431(bool multi)
    {
        var limits = Defaults with { MaxHeaderCount = 1 };
        var raw = "GET / HTTP/1.1\r\nH1: v1\r\nH2: v2\r\n\r\n";

        var ex = Assert.Throws<HttpParseException>(() => Parse(raw, multi, limits));
        Assert.Equal(431, ex.StatusCode);
        Assert.True(ex.IsLimitViolation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StructuralErrorThrows400(bool multi)
    {
        // Missing colon in header → structural 400
        var raw = "GET / HTTP/1.1\r\nBadHeader\r\n\r\n";

        var ex = Assert.Throws<HttpParseException>(() => Parse(raw, multi));
        Assert.Equal(400, ex.StatusCode);
        Assert.False(ex.IsLimitViolation);
    }
}
