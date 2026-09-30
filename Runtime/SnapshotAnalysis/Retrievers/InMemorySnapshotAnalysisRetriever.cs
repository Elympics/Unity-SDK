#nullable enable

namespace Elympics.SnapshotAnalysis.Retrievers
{
    /// <summary>A replay already deserialized, e.g. after a download.</summary>
    internal sealed class InMemorySnapshotAnalysisRetriever : SnapshotAnalysisRetriever
    {
        public InMemorySnapshotAnalysisRetriever(SnapshotReplayData replay) => Replay = replay;
    }
}
