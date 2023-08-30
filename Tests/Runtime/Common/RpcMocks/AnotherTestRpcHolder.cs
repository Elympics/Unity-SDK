namespace Elympics.Tests.RpcMocks
{
    public class AnotherTestRpcHolder : ElympicsMonoBehaviour
    {
        public bool PlayerToServerMethodCalled { get; private set; }
        public bool PlayerToServerMethodWithArgsCalled { get; private set; }
        public bool ServerToPlayersMethodCalled { get; private set; }
        public bool ServerToPlayersMethodWithArgsCalled { get; private set; }

        // The following ordering allows for checking if methods in RpcMethods map are sorted correctly by name.

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PlayerToServerMethod() => PlayerToServerMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void ServerToPlayersMethodWithArgs(int _) => ServerToPlayersMethodWithArgsCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void ServerToPlayersMethod() => ServerToPlayersMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PlayerToServerMethodWithArgs(int _) => PlayerToServerMethodWithArgsCalled = true;

        public void Reset()
        {
            PlayerToServerMethodCalled = false;
            ServerToPlayersMethodCalled = false;
            PlayerToServerMethodWithArgsCalled = false;
            ServerToPlayersMethodWithArgsCalled = false;
        }
    }
}

