using Elympics.Replication;

namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderNoContext : RpcHolderInContext
    {
        public override void Setup(ElympicsBaseTest elympicsInstance) =>
            elympicsInstance.ElympicsBehavioursManager.InitializeInternal(elympicsInstance, ElympicsWorld.Current.MaxPlayers);

        public override void Act(ElympicsBaseTest elympicsInstance) => CallRpc();
    }
}
