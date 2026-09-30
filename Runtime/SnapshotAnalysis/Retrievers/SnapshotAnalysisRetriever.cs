#nullable enable

using System.Collections.Generic;

namespace Elympics.SnapshotAnalysis.Retrievers
{
    internal abstract class SnapshotAnalysisRetriever
    {
        protected SnapshotReplayData Replay;
        public virtual SnapshotSaverInitData RetrieveInitData() => Replay.InitData;
        public virtual Dictionary<long, ElympicsSnapshotWithMetadata> RetrieveSnapshots() => Replay.Snapshots;
    }
}
