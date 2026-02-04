using Glyph11.Utils;

namespace Glyph11;

public struct Request11BinaryData
{
    public ReadOnlyMemory<byte> HttpMethod;
    
    public ReadOnlyMemory<byte> Route;
    
    public PooledKeyValueList QueryParameters;
    
    public PooledKeyValueList Headers;

    public ReadOnlyMemory<byte> Body;
}