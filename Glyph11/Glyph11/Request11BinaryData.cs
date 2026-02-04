using Glyph11.Utils;

namespace Glyph11;

public class Request11BinaryData
{
    public ReadOnlyMemory<byte> Version;
    
    public ReadOnlyMemory<byte> HttpMethod;
    
    public ReadOnlyMemory<byte> Route;

    public PooledKeyValueList QueryParameters = null!;
    
    public PooledKeyValueList Headers = null!;

    public ReadOnlyMemory<byte> Body;

    public void Clear()
    {
        QueryParameters.Clear();
        Headers.Clear();
    }
}