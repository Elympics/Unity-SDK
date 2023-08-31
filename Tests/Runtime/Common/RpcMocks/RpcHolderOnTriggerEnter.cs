using UnityEngine;

namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderOnTriggerEnter : RpcHolderInContext
    {
        private void OnTriggerEnter(Collider _)
        {
            if (ShouldCallPlayerToServerMethod)
                PlayerToServerMethod();
            if (ShouldCallServerToPlayerMethod)
                ServerToPlayersMethod();
        }
    }
}

