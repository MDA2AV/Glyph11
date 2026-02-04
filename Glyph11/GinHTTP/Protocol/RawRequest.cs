using Glyph11.Protocol;
using Glyph11.Utils;

namespace GinHTTP.Protocol;

public class RawRequest : IRawRequest
{
    private readonly IKeyValueList _headers, _queryParameters;
    
    public ReadOnlyMemory<byte> Version { get; set; }
    
    public ReadOnlyMemory<byte> Method { get; set; }
    
    public ReadOnlyMemory<byte> Route { get; set; }

    public IKeyValueList QueryParameters => _queryParameters;
    
    public IKeyValueList Headers => _headers;

    public ReadOnlyMemory<byte> Body { get; set; }
    
    public RawRequest()
    {
        _headers = new PooledKeyValueList(initialCapacity: 16);
        _queryParameters = new PooledKeyValueList(initialCapacity: 16);
    }
}