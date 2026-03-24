namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderReliable : ElympicsMonoBehaviour
    {
        public bool PlayerToServerReliableMethodCalled { get; private set; }
        public bool ServerToPlayersReliableMethodCalled { get; private set; }
        public bool PlayerToServerUnreliableMethodCalled { get; private set; }
        public bool ServerToPlayersUnreliableMethodCalled { get; private set; }

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PlayerToServerMethodReliable() => PlayerToServerReliableMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void ServerToPlayersMethodReliable() => ServerToPlayersReliableMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer, Reliable = false)]
        public void PlayerToServerMethodUnreliable() => PlayerToServerUnreliableMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers, Reliable = false)]
        public void ServerToPlayersMethodUnreliable() => ServerToPlayersUnreliableMethodCalled = true;

        public virtual void Reset()
        {
            PlayerToServerReliableMethodCalled = false;
            ServerToPlayersReliableMethodCalled = false;
            PlayerToServerUnreliableMethodCalled = false;
            ServerToPlayersUnreliableMethodCalled = false;
        }
    }
}

