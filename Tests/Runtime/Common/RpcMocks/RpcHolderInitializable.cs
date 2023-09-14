namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderInitializable : RpcHolderInContext, IInitializable
    {
        public void Initialize()
        {
            if (ShouldCallPlayerToServerMethod)
                PlayerToServerMethod();
            if (ShouldCallServerToPlayerMethod)
                ServerToPlayersMethod();
        }
    }
}

