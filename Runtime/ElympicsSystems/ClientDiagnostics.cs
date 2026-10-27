#nullable enable

namespace Elympics
{
    internal readonly struct ClientDiagnostics
    {
        public readonly double RttTicks;
        public readonly double RttMs;
        public readonly bool CanPredict;
        /// <summary>The tick of the last server snapshot used for reconciliation; -1 before the first one.</summary>
        public readonly long LastSnapshotTick;
        public readonly long? TicksAheadOfServer;
        public readonly int ReanchoredCount;
        public readonly int ReplayedCount;

        public ClientDiagnostics(double rttTicks, double rttMs, bool canPredict, long lastSnapshotTick, long? ticksAheadOfServer, int reanchoredCount, int replayedCount)
        {
            RttTicks = rttTicks;
            RttMs = rttMs;
            CanPredict = canPredict;
            LastSnapshotTick = lastSnapshotTick;
            TicksAheadOfServer = ticksAheadOfServer;
            ReanchoredCount = reanchoredCount;
            ReplayedCount = replayedCount;
        }
    }
}
