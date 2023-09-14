namespace Elympics.Tests.RpcMocks
{
    public abstract class RpcHolder : ElympicsMonoBehaviour
    {
        public bool PlayerToServerMethodCalled { get; private set; }
        public bool ServerToPlayersMethodCalled { get; private set; }

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PlayerToServerMethod() => PlayerToServerMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void ServerToPlayersMethod() => ServerToPlayersMethodCalled = true;

        public virtual void Reset()
        {
            PlayerToServerMethodCalled = false;
            ServerToPlayersMethodCalled = false;
        }
    }
}

