using System.Reflection;

namespace Elympics.Tests.RpcMocks
{
    public abstract class RpcHolder : ElympicsMonoBehaviour
    {
        public bool PlayerToServerMethodCalled { get; private set; }
        public bool ServerToPlayersMethodCalled { get; private set; }
        public bool ParentPlayerToServerMethodPrivateCalled { get; private set; }

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PlayerToServerMethod() => PlayerToServerMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void ServerToPlayersMethod() => ServerToPlayersMethodCalled = true;

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        private void ParentPlayerToServerMethodPrivate() => ParentPlayerToServerMethodPrivateCalled = true;
        public static MethodInfo ParentPlayerToServerMethodPrivateInfo =>
            typeof(RpcHolder).GetMethod(nameof(ParentPlayerToServerMethodPrivate), BindingFlags.Instance | BindingFlags.NonPublic);
        public void CallParentPlayerToServerMethodPrivate() => ParentPlayerToServerMethodPrivate();

        public virtual void Reset()
        {
            PlayerToServerMethodCalled = false;
            ServerToPlayersMethodCalled = false;
        }
    }
}

