using Elympics.Replication;

namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderValueChanged : RpcHolderInContext
    {
        private readonly ElympicsBool _winConditionMet = new();

        public override void Setup(ElympicsBaseTest elympicsInstance)
        {
            elympicsInstance.ElympicsBehavioursManager.InitializeInternal(elympicsInstance, ElympicsWorld.Current.MaxPlayers);
            _winConditionMet.ValueChanged += OnWinConditionMet;
        }

        public override void Act(ElympicsBaseTest elympicsInstance)
        {
            _winConditionMet.Value = !_winConditionMet.Value;
            elympicsInstance.ElympicsBehavioursManager.CommitVars();
        }

        private void OnWinConditionMet(bool previous, bool current)
        {
            CallRpc();
            _winConditionMet.ValueChanged -= OnWinConditionMet;
        }
    }
}
