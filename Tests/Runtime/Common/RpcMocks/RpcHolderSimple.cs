namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderSimple : RpcHolder
    {
        public bool PlayerToServerMethodWithArgsCalled { get; private set; }
        public bool ServerToPlayersMethodWithArgsCalled { get; private set; }

        // The following ordering allows for checking if methods in RpcMethods map are sorted correctly by name.

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void ServerToPlayersMethodWithArgs(int _) => ServerToPlayersMethodWithArgsCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PlayerToServerMethodWithArgs(int _) => PlayerToServerMethodWithArgsCalled = true;

        public override void Reset()
        {
            base.Reset();
            PlayerToServerMethodWithArgsCalled = false;
            ServerToPlayersMethodWithArgsCalled = false;
        }
    }
}

