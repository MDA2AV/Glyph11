using Microsoft.Extensions.ObjectPool;

namespace GinHTTP.Context;

public class ClientContextPolicy : PooledObjectPolicy<ClientContext>
{

    public override ClientContext Create() => new();

    public override bool Return(ClientContext obj)
    {
        obj.Clear();
        return true;
    }

}