#nullable enable

using System;

namespace Elympics.SnapshotAnalysis.Retrievers
{
    public class WebDownloader : ISnapshotDownloader
    {
        private readonly Uri _source;
        public WebDownloader(Uri source) => _source = source;
        public byte[] DownloadReplay(string source) => throw new NotImplementedException();
    }
}
