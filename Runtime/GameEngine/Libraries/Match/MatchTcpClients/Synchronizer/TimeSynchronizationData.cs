using System;

namespace MatchTcpClients.Synchronizer
{
    public class TimeSynchronizationData
    {
        public static readonly TimeSynchronizationData Localhost = new() { LocalClockOffset = TimeSpan.Zero, RoundTripDelay = TimeSpan.Zero, UnreliableReceivedAnyPing = false, UnreliableWaitingForFirstPing = true };

        public TimeSpan LocalClockOffset { get; set; }
        public TimeSpan RoundTripDelay { get; set; }

        public bool UnreliableWaitingForFirstPing { get; set; }
        public bool UnreliableReceivedAnyPing { get; set; }
        public bool UnreliableReceivedPingLately { get; set; }
        public DateTime? UnreliableLastReceivedPingDateTime { get; set; }
        public TimeSpan? UnreliableLocalClockOffset { get; set; }
        public TimeSpan? UnreliableRoundTripDelay { get; set; }
    }
}
