using System.Text;
using Glyph11;
using Glyph11.Utils;

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
            "Server: Nigero\r\n" +
            "\r\n";

        ReadOnlyMemory<byte> rom = Encoding.ASCII.GetBytes(request);

        var data = new Request11BinaryData();
        data.QueryParameters = new PooledKeyValueList();
        data.Headers = new PooledKeyValueList();

        int position = 0;
        
        var parsed = parser.TryExtractFullHeader(ref rom, ref data, ref position);
        
        Assert.True(parsed);
    }
}