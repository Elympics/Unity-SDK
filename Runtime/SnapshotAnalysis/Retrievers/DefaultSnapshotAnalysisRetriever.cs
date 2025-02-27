#nullable enable
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Elympics.SnapshotAnalysis.Retrievers
{
    internal class DefaultSnapshotAnalysisRetriever : SnapshotAnalysisRetriever
    {
        private readonly IMatchLauncher _matchLauncher;
        private readonly ISnapshotDownloader _downloader;
        public DefaultSnapshotAnalysisRetriever(Uri source, IMatchLauncher matchLauncher)
        {
            _matchLauncher = matchLauncher;
            _downloader = SnapshotDownloaderFactory.GetDownloader(source);
        }

        public UniTask RetrieveSnapshotReplay(string matchId) =>
            throw new NotImplementedException(); //This will have to be called before RetrieveInitData or RetrieveSnapshots
        public override SnapshotSaverInitData RetrieveInitData() => Replay.InitData;
        public override Dictionary<long, ElympicsSnapshotWithMetadata> RetrieveSnapshots() => Replay.Snapshots;
    }
}
