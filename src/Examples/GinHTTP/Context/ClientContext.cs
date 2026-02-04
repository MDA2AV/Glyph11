using GinHTTP.Protocol;

namespace GinHTTP.Context;

public class ClientContext
{
    public Request Request { get; set; } = null!;

    internal void Clear()
    {
        Request.Clear();
    }
}