namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderWaitForTick : ElympicsMonoBehaviour
    {
        public bool PlayerToServerWaitingMethodCalled { get; private set; }
        public bool ServerToPlayersWaitingMethodCalled { get; private set; }
        public bool PlayerToServerNotWaitingMethodCalled { get; private set; }
        public bool ServerToPlayersNotWaitingMethodCalled { get; private set; }

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PlayerToServerMethodWaiting() => PlayerToServerWaitingMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void ServerToPlayersMethodWaiting() => ServerToPlayersWaitingMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer, WaitForTick = false)]
        public void PlayerToServerMethodNotWaiting() => PlayerToServerNotWaitingMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers, WaitForTick = false)]
        public void ServerToPlayersMethodNotWaiting() => ServerToPlayersNotWaitingMethodCalled = true;

        public virtual void Reset()
        {
            PlayerToServerWaitingMethodCalled = false;
            ServerToPlayersWaitingMethodCalled = false;
            PlayerToServerNotWaitingMethodCalled = false;
            ServerToPlayersNotWaitingMethodCalled = false;
        }
    }
}

