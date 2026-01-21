#nullable enable
using System.Collections.Generic;
using System.IO;
using MessagePack;
using UnityEngine;

namespace Elympics.SnapshotAnalysis.Serialization
{
    internal static partial class SnapshotDeserializer
    {
        private class LatestMessagePackDeserializer : Deserializer
        {
            private MessagePackSerializerOptions? _options;
            private readonly Dictionary<int, byte[]> _unpackSourceData = new();
            public override SnapshotReplayData DeserializeSnapshots(Stream stream)
            {
                _options ??= MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4BlockArray);
                var initData = MessagePackSerializer.Deserialize<SnapshotSaverInitData>(stream, _options);
                var snapshots = new Dictionary<long, ElympicsSnapshotWithMetadata>();

                var lastSnapshot = 0L;
                while (stream.Position < stream.Length)
                {

                    var deserializedSnapshots = MessagePackSerializer.Deserialize<SnapshotSerializationPackage>(stream, _options);
                    Debug.Log($"Found snapshots {deserializedSnapshots.Snapshots.Length}");
                    foreach (var deserialized in deserializedSnapshots.Snapshots)
                    {
                        foreach (var (id, state) in deserialized.Data!)
                        {
                            if (state != null)
                                _unpackSourceData[id] = state;
                            else
                                deserialized.Data[id] = _unpackSourceData[id];
                        }

                        snapshots.Add(deserialized.Tick, deserialized);
                        lastSnapshot = deserialized.Tick;
                    }
                }
                Debug.Log($"Last deserialized snapshot is {lastSnapshot}");

                return new SnapshotReplayData { InitData = initData, Snapshots = snapshots };
            }
        }
    }
}
