#nullable enable

using System.Collections.Generic;

namespace Elympics.SnapshotAnalysis.Retrievers
{
    internal abstract class SnapshotAnalysisRetriever
    {
        protected SnapshotReplayData Replay;
        public abstract SnapshotSaverInitData RetrieveInitData();
        public abstract Dictionary<long, ElympicsSnapshotWithMetadata> RetrieveSnapshots();
    }
}
