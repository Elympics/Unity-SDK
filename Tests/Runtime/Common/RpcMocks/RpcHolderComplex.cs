using System.Reflection;

namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderComplex : RpcHolder
    {
        public bool PlayerToServerMethodPrivateCalled { get; private set; }
        public bool PingServerToPlayersCalled { get; private set; }
        public bool PongPlayerToServerCalled { get; private set; }
        public bool PingPlayerToServerCalled { get; private set; }
        public bool PongServerToPlayersCalled { get; private set; }

        public (bool, byte, sbyte, ushort, short, uint, int, ulong, long, float, double, char, string)? PlayerToServerMethodLastCallArguments { get; private set; }
        public (bool, byte, sbyte, ushort, short, uint, int, ulong, long, float, double, char, string)? ServerToPlayersMethodLastCallArguments { get; private set; }

        // The following ordering allows for checking if methods in RpcMethods map are sorted correctly by name.

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void PingServerToPlayers()
        {
            PingServerToPlayersCalled = true;
            PongPlayerToServer();
        }

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PongPlayerToServer() => PongPlayerToServerCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PingPlayerToServer()
        {
            PingPlayerToServerCalled = true;
            PongServerToPlayers();
        }

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void PongServerToPlayers() => PongServerToPlayersCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void ServerToPlayersMethodWithArgs(bool boolArg, byte byteArg, sbyte sbyteArg, ushort ushortArg, short shortArg, uint uintArg, int intArg, ulong ulongArg, long longArg, float floatArg, double doubleArg, char charArg, string stringArg) =>
            ServerToPlayersMethodLastCallArguments = (boolArg, byteArg, sbyteArg, ushortArg, shortArg, uintArg, intArg, ulongArg, longArg, floatArg, doubleArg, charArg, stringArg);

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PlayerToServerMethodWithArgs(bool boolArg, byte byteArg, sbyte sbyteArg, ushort ushortArg, short shortArg, uint uintArg, int intArg, ulong ulongArg, long longArg, float floatArg, double doubleArg, char charArg, string stringArg) =>
            PlayerToServerMethodLastCallArguments = (boolArg, byteArg, sbyteArg, ushortArg, shortArg, uintArg, intArg, ulongArg, longArg, floatArg, doubleArg, charArg, stringArg);

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        private void PlayerToServerMethodPrivate() => PlayerToServerMethodPrivateCalled = true;

        public void CallPlayerToServerMethodPrivate() => PlayerToServerMethodPrivate();
        public MethodInfo PlayerToServerMethodPrivateInfo => GetType().GetMethod(nameof(PlayerToServerMethodPrivate), BindingFlags.Instance | BindingFlags.NonPublic);

        public override void Reset()
        {
            base.Reset();
            PlayerToServerMethodLastCallArguments = null;
            ServerToPlayersMethodLastCallArguments = null;
            PlayerToServerMethodPrivateCalled = false;
            PingServerToPlayersCalled = false;
            PongPlayerToServerCalled = false;
            PingPlayerToServerCalled = false;
            PongServerToPlayersCalled = false;
        }
    }
}

