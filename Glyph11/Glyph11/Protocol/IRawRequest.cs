using Glyph11.Utils;

namespace Glyph11.Protocol;

public interface IRawRequest
{
    ReadOnlyMemory<byte> Version { get; set; }

    ReadOnlyMemory<byte> Method { get; set; }
    
    ReadOnlyMemory<byte> Route { get; set; }

    IKeyValueList QueryParameters { get; }
    
    IKeyValueList Headers { get; }

    ReadOnlyMemory<byte> Body { get; set; }
}