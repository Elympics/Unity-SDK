using System.IO;
using Cysharp.Threading.Tasks;

namespace Elympics.SnapshotAnalysis.Serialization
{
    public abstract class SnapshotSerializer
    {
        public abstract UniTask SerializeVersionToStream(Stream stream, string version);
        public abstract UniTask SerializeToStream<T>(Stream stream, T data);
    }
}
