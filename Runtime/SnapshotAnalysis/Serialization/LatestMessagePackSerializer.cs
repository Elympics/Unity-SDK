#nullable enable
using System.IO;
using System.Threading.Tasks;
using MessagePack;

namespace Elympics.SnapshotAnalysis.Serialization
{
    public class LatestMessagePackSerializer : SnapshotSerializer
    {
        private MessagePackSerializerOptions? _options;
        public override Task SerializeVersionToStream(Stream stream, string version) => MessagePackSerializer.SerializeAsync(stream, version);
        public override Task SerializeToStream<T>(Stream stream, T data) => MessagePackSerializer.SerializeAsync(stream,
            data,
            _options ??= MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4BlockArray));
    }
}
