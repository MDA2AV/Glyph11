using Glyph11.Protocol;

namespace GinHTTP.Protocol;

public interface IRequest
{
    IBinaryRequest Binary { get; }

    RequestMethod Method { get; }
}