using System;
using Elympics.Util;

namespace Elympics.SnapshotAnalysis.Retrievers
{
    internal static class SnapshotDownloaderFactory
    {
        private const string CustomUserSource = "custom";

        public static ISnapshotDownloader GetDownloader(Uri source)
        {
            if (UnityUtil.IsWebGL && source.Host.Equals(CustomUserSource))
                return new JsDownloader();
            return new WebDownloader(source);
        }
    }
}
