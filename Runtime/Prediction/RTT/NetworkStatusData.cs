#nullable enable

using System.IO;

namespace Elympics
{
    public struct NetworkStatusData
    {
        public float Rtt { get; init; }
        public long Tick { get; init; }

        public static readonly int ByteSize = sizeof(float) + sizeof(long);

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(Rtt);
            bw.Write(Tick);
        }
    }
}
