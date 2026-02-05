using GenHTTP.Api.Draft.Protocol.Raw;

namespace GenHTTP.Api.Draft.Protocol;

public interface IRequest
{

    IRawRequest Raw { get; }

    RequestMethod Method { get; }

}
