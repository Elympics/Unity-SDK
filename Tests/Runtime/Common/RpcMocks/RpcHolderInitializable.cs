using Elympics.Replication;

namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderInitializable : RpcHolderInContext, IInitializable
    {
        public void Initialize() => CallRpc();

        public override void Setup(ElympicsBaseTest elympicsInstance)
        { }

        public override void Act(ElympicsBaseTest elympicsInstance) =>
            elympicsInstance.ElympicsBehavioursManager.InitializeInternal(elympicsInstance, ElympicsWorld.Current.MaxPlayers);
    }
}

