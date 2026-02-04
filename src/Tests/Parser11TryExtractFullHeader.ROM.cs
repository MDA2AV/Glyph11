using System.Text;
using GinHTTP.Protocol;
using Glyph11;
using Parser11 = Glyph11.Parser.Parser11;

namespace Tests;

public class Parser11TryExtractFullHeader_ROM
{
    [Fact]
    public void ParseRequest()
    {
        var parser = new Parser11();
        
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
}