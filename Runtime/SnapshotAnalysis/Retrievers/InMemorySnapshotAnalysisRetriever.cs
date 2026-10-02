#nullable enable

namespace Elympics.SnapshotAnalysis.Retrievers
{
    internal sealed class InMemorySnapshotAnalysisRetriever : SnapshotAnalysisRetriever
    {
        public InMemorySnapshotAnalysisRetriever(SnapshotReplayData replay) => Replay = replay;
    }
}
