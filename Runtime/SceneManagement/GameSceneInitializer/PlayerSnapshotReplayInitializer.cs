#nullable enable
using Elympics.SnapshotAnalysis;

namespace Elympics
{
    internal class PlayerSnapshotReplayInitializer : SnapshotReplayInitializer
    {
        public override void Initialize(
            ElympicsClient client,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager)
        {

            var snapshotRetriever = LobbyRegister.GetSnapshotRetriever();
            InitializeInternal(server, gameConfig, behavioursManager, snapshotRetriever, new NullReplayManipulator() /*TO DO: Pass a manipulator connected to UI (from game or PlayPad)*/);
        }
    }
}
