namespace Glyph11.Protocol;

public interface IRequest
{
    IRawRequest Raw { get; }

    RequestMethod Method { get; }
}