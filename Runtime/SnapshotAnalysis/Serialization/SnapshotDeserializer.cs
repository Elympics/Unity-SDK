#nullable enable

using System;
using System.IO;
using MessagePack;

namespace Elympics.SnapshotAnalysis.Serialization
{
    internal static partial class SnapshotDeserializer
    {
        public static SnapshotReplayData DeserializeSnapshots(Stream stream)
        {
            var serializerVersion = MessagePackSerializer.Deserialize<string>(stream);
            return CreateMatchingDeserializer(serializerVersion).DeserializeSnapshots(stream);
        }

        private static Deserializer CreateMatchingDeserializer(string version)
        {
            return version switch
            {
                SerializationUtil.LatestVersion => new LatestMessagePackDeserializer(),
                //Legacy deserializers can be addede here in the future
                _ => throw new ArgumentException($"Unknown deserializer version: {version}", nameof(version)),
            };
        }

        private abstract class Deserializer
        {
            public abstract SnapshotReplayData DeserializeSnapshots(Stream stream);
        }
    }
}
