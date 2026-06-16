#nullable enable

using System;

namespace Elympics.AssemblyCommunicator.Events
{
    [Serializable]
    public struct ReceivedStatsUpdated
    {
        public long received;
        public long total;

        public override string ToString() => $"(Received: {received}, Total: {total})";
    }
}
