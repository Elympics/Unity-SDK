using MessagePack;
namespace Elympics.SnapshotAnalysis.Serialization
{
    [MessagePackObject]
    public class SnapshotSerializationPackage
    {
        [Key(0)] public ElympicsSnapshotWithMetadata[] Snapshots;
    }
}
