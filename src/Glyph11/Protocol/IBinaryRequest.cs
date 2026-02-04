using Glyph11.ProprietaryCollections;

namespace Glyph11.Protocol;

public interface IBinaryRequest
{
    ReadOnlyMemory<byte> Version { get; set; }

    ReadOnlyMemory<byte> Method { get; set; }
    
    ReadOnlyMemory<byte> Route { get; set; }

    IKeyValueList QueryParameters { get; }
    
    IKeyValueList Headers { get; }

    ReadOnlyMemory<byte> Body { get; set; }
}