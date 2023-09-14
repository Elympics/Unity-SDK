namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderUpdatable : RpcHolderInContext, IUpdatable
    {
        public void ElympicsUpdate()
        {
            if (ShouldCallPlayerToServerMethod)
                PlayerToServerMethod();
            if (ShouldCallServerToPlayerMethod)
                ServerToPlayersMethod();
        }
    }
}

