#nullable enable
using Elympics.SnapshotAnalysis;
using Elympics.SnapshotAnalysis.Retrievers;

namespace Elympics
{
    internal class PlayerSnapshotReplayInitializer : SnapshotReplayInitializer
    {
        private readonly SnapshotAnalysisRetriever _snapshotRetriever;

        public PlayerSnapshotReplayInitializer(SnapshotAnalysisRetriever snapshotRetriever) => _snapshotRetriever = snapshotRetriever;

        public override void Initialize(
            ElympicsClient client,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager)
        {
            InitializeInternal(server, gameConfig, behavioursManager, _snapshotRetriever, new NullReplayManipulator() /*TO DO: Pass a manipulator connected to UI (from game or PlayPad)*/);
        }
    }
}
