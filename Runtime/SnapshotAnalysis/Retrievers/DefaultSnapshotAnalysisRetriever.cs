#nullable enable
using System;
using Cysharp.Threading.Tasks;

namespace Elympics.SnapshotAnalysis.Retrievers
{
    internal class DefaultSnapshotAnalysisRetriever : SnapshotAnalysisRetriever
    {
        private readonly ISnapshotDownloader _downloader;
        public DefaultSnapshotAnalysisRetriever(Uri source)
        {
            _downloader = SnapshotDownloaderFactory.GetDownloader(source);
        }

        public UniTask RetrieveSnapshotReplay(string matchId) =>
            throw new NotImplementedException(); //This will have to be called before RetrieveInitData or RetrieveSnapshots
    }
}
