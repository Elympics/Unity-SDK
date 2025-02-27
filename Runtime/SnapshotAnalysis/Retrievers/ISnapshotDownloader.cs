#nullable enable

namespace Elympics.SnapshotAnalysis.Retrievers
{
    public interface ISnapshotDownloader
    {
        public byte[] DownloadReplay(string source);
    }
}
