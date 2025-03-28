#nullable enable

using System;

namespace Elympics.AssemblyCommunicator.Events
{
    [Serializable]
    public struct RttReceived
    {
        /// <summary>Round trip time in miliseconds.</summary>
        /// <remarks>This is raw RTT from last measurement, for most practical applications consider calculating average RTT from this raw data.</remarks>
        public float rtt;

        /// <summary>Tick in which RTT measurement was finished.</summary>
        public long tick;

        public override string ToString() => $"(Tick: {tick}, RTT: {rtt})";
    }
}
