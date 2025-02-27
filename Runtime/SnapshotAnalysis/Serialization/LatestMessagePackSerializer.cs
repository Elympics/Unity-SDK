#nullable enable
using System.IO;
using Cysharp.Threading.Tasks;
using MessagePack;

namespace Elympics.SnapshotAnalysis.Serialization
{
    public class LatestMessagePackSerializer : SnapshotSerializer
    {
        private MessagePackSerializerOptions? _options;
        public override async UniTask SerializeVersionToStream(Stream stream, string version) => await MessagePackSerializer.SerializeAsync(stream, version);
        public override async UniTask SerializeToStream<T>(Stream stream, T data) => await MessagePackSerializer.SerializeAsync(stream,
            data,
            _options ??= MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4BlockArray));
    }
}
