using Glyph11.ProprietaryCollections;
using Glyph11.Protocol;

namespace GinHTTP.Protocol;

public class BinaryRequest : IBinaryRequest
{
    private readonly IKeyValueList _headers, _queryParameters;
    
    public ReadOnlyMemory<byte> Version { get; set; }
    
    public ReadOnlyMemory<byte> Method { get; set; }
    
    public ReadOnlyMemory<byte> Route { get; set; }

    public IKeyValueList QueryParameters => _queryParameters;
    
    public IKeyValueList Headers => _headers;

    public ReadOnlyMemory<byte> Body { get; set; }
    
    public BinaryRequest()
    {
        _headers = new PooledKeyValueList(initialCapacity: 16);
        _queryParameters = new PooledKeyValueList(initialCapacity: 16);
    }
}