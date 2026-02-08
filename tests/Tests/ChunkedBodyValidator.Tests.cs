using System.Text;
using Glyph11;
using Glyph11.Validation;

namespace Tests;

public class ChunkedBodyValidatorTests
{
    private static byte[] B(string s) => Encoding.ASCII.GetBytes(s);

    // ---- Valid cases ----

    [Fact]
    public void Valid_SingleChunk()
    {
        var body = B("5\r\nHello\r\n0\r\n\r\n");
        Assert.True(ChunkedBodyValidator.TryValidate(body, out var consumed));
        Assert.Equal(body.Length, consumed);
    }

    [Fact]
    public void Valid_MultiChunk()
    {
        var body = B("5\r\nHello\r\n6\r\n World\r\n0\r\n\r\n");
        Assert.True(ChunkedBodyValidator.TryValidate(body, out var consumed));
        Assert.Equal(body.Length, consumed);
    }

    [Fact]
    public void Valid_EmptyBody()
    {
        var body = B("0\r\n\r\n");
        Assert.True(ChunkedBodyValidator.TryValidate(body, out var consumed));
        Assert.Equal(body.Length, consumed);
    }

    [Fact]
    public void Valid_ChunkWithExtension()
    {
        var body = B("5;name=value\r\nHello\r\n0\r\n\r\n");
        Assert.True(ChunkedBodyValidator.TryValidate(body, out var consumed));
        Assert.Equal(body.Length, consumed);
    }

    [Fact]
    public void Valid_HexUppercase()
    {
        var body = B("A\r\n0123456789\r\n0\r\n\r\n");
        Assert.True(ChunkedBodyValidator.TryValidate(body, out var consumed));
        Assert.Equal(body.Length, consumed);
    }

    [Fact]
    public void Valid_HexLowercase()
    {
        var body = B("a\r\n0123456789\r\n0\r\n\r\n");
        Assert.True(ChunkedBodyValidator.TryValidate(body, out var consumed));
        Assert.Equal(body.Length, consumed);
    }

    [Fact]
    public void Valid_WithTrailers()
    {
        var body = B("0\r\nTrailer: value\r\n\r\n");
        Assert.True(ChunkedBodyValidator.TryValidate(body, out var consumed));
        Assert.Equal(body.Length, consumed);
    }

    // ---- Invalid cases ----

    [Fact]
    public void Invalid_MissingCrlfAfterChunkData()
    {
        var body = B("5\r\nHello0\r\n\r\n");
        Assert.Throws<HttpParseException>(() => ChunkedBodyValidator.TryValidate(body, out _));
    }

    [Fact]
    public void Invalid_BareLfTerminator()
    {
        var body = B("5\nHello\r\n0\r\n\r\n");
        Assert.Throws<HttpParseException>(() => ChunkedBodyValidator.TryValidate(body, out _));
    }

    [Fact]
    public void Invalid_HexPrefix()
    {
        var body = B("0x5\r\nHello\r\n0\r\n\r\n");
        Assert.Throws<HttpParseException>(() => ChunkedBodyValidator.TryValidate(body, out _));
    }

    [Fact]
    public void Invalid_UnderscoreInSize()
    {
        var body = B("5_0\r\n" + new string('A', 80) + "\r\n0\r\n\r\n");
        Assert.Throws<HttpParseException>(() => ChunkedBodyValidator.TryValidate(body, out _));
    }

    [Fact]
    public void Invalid_LeadingSpace()
    {
        var body = B(" 5\r\nHello\r\n0\r\n\r\n");
        Assert.Throws<HttpParseException>(() => ChunkedBodyValidator.TryValidate(body, out _));
    }

    [Fact]
    public void Invalid_NegativeSize()
    {
        var body = B("-5\r\nHello\r\n0\r\n\r\n");
        Assert.Throws<HttpParseException>(() => ChunkedBodyValidator.TryValidate(body, out _));
    }

    [Fact]
    public void Invalid_NulInExtension()
    {
        var raw = new byte[] {
            (byte)'5', (byte)';', 0x00, (byte)'\r', (byte)'\n',
            (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o',
            (byte)'\r', (byte)'\n',
            (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n'
        };
        Assert.Throws<HttpParseException>(() => ChunkedBodyValidator.TryValidate(raw, out _));
    }

    [Fact]
    public void Invalid_IntegerOverflow()
    {
        // 17 hex digits → overflow
        var body = B("FFFFFFFFFFFFFFFFF\r\n");
        Assert.Throws<HttpParseException>(() => ChunkedBodyValidator.TryValidate(body, out _));
    }

    [Fact]
    public void Invalid_BareLfInTrailer()
    {
        var body = B("0\r\nTrailer: value\n\r\n");
        Assert.Throws<HttpParseException>(() => ChunkedBodyValidator.TryValidate(body, out _));
    }

    // ---- Incomplete cases ----

    [Fact]
    public void Incomplete_EmptyBuffer()
    {
        Assert.False(ChunkedBodyValidator.TryValidate(ReadOnlySpan<byte>.Empty, out _));
    }

    [Fact]
    public void Incomplete_PartialSize()
    {
        var body = B("5");
        Assert.False(ChunkedBodyValidator.TryValidate(body, out _));
    }

    [Fact]
    public void Incomplete_SizeWithoutData()
    {
        var body = B("5\r\n");
        Assert.False(ChunkedBodyValidator.TryValidate(body, out _));
    }

    [Fact]
    public void Incomplete_MissingFinalCrlf()
    {
        var body = B("0\r\n");
        Assert.False(ChunkedBodyValidator.TryValidate(body, out _));
    }
}
