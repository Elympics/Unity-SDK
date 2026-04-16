using Elympics.Replication;

namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderUpdatable : RpcHolderInContext, IUpdatable
    {
        public void ElympicsUpdate() => CallRpc();

        public override void Setup(ElympicsBaseTest elympicsInstance) =>
            elympicsInstance.ElympicsBehavioursManager.InitializeInternal(elympicsInstance, ElympicsWorld.Current.MaxPlayers);

        public override void Act(ElympicsBaseTest elympicsInstance) =>
            elympicsInstance.ElympicsBehavioursManager.ElympicsUpdate();
    }
}

