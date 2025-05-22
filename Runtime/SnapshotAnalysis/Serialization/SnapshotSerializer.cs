using System.IO;
using System.Threading.Tasks;

namespace Elympics.SnapshotAnalysis.Serialization
{
    public abstract class SnapshotSerializer
    {
        public abstract Task SerializeVersionToStream(Stream stream, string version);
        public abstract Task SerializeToStream<T>(Stream stream, T data);
    }
}
