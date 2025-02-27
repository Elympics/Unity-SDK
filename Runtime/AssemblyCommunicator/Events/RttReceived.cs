#nullable enable

using System.IO;

namespace Elympics.AssemblyCommunicator.Events
{
    [ElympicsEvent(ElympicsEventAttribute.ElympicsSDK)]
    public struct RttReceived
    {
        /// <summary>Round trip time in miliseconds.</summary>
        /// <remarks>This is raw RTT from last measurement, for most practical applications consider calculating average RTT from this raw data.</remarks>
        public float Rtt { get; init; }

        /// <summary>Tick in which RTT measurement was finished.</summary>
        public long Tick { get; init; }

        /// <summary>Numer of bytes written to <see cref="BinaryWriter"/> by <see cref="Serialize(BinaryWriter)"/>.</summary>
        public static readonly int ByteSize = sizeof(float) + sizeof(long);

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(Rtt);
            bw.Write(Tick);
        }

        public override string ToString() => $"(Tick: {Tick}, RTT: {Rtt})";
    }
}
