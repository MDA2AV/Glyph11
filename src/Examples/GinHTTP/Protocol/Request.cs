using GinHTTP.Utils;
using Glyph11.Protocol;

namespace GinHTTP.Protocol;

public class Request : IRequest
{
    private readonly BinaryRequest _binary = new();
    
    private RequestMethod? _method;
    
    public IBinaryRequest Binary => _binary;

    public RequestMethod Method
    {
        get
        {
            if (_method == null)
            {
                var m = _binary.Method.Span;

                _method = m.Length switch
                {
                    3 when AsciiComparer.EqualsIgnoreCase(m, "GET"u8) => RequestMethod.Get,
                    4 when AsciiComparer.EqualsIgnoreCase(m, "POST"u8) => RequestMethod.Post,
                    3 when AsciiComparer.EqualsIgnoreCase(m, "PUT"u8) => RequestMethod.Put,
                    6 when AsciiComparer.EqualsIgnoreCase(m, "DELETE"u8) => RequestMethod.Delete,
                    4 when AsciiComparer.EqualsIgnoreCase(m, "HEAD"u8) => RequestMethod.Head,
                    7 when AsciiComparer.EqualsIgnoreCase(m, "OPTIONS"u8) => RequestMethod.Options,
                    5 when AsciiComparer.EqualsIgnoreCase(m, "PATCH"u8) => RequestMethod.Patch,
                    5 when AsciiComparer.EqualsIgnoreCase(m, "TRACE"u8) => RequestMethod.Trace,
                    7 when AsciiComparer.EqualsIgnoreCase(m, "CONNECT"u8) => RequestMethod.Connect,
                    _ => RequestMethod.Other
                };
            }

            return _method.Value;
        }
    }
    
    public void Clear()
    {
        Binary.QueryParameters.Clear();
        Binary.Headers.Clear();
    }
}