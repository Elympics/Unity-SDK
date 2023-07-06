using System;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
    public class RoundTripTimeCalculator : IRoundTripTimeCalculator
    {
        public TimeSpan LastRoundTripTime { get; private set; }
        public TimeSpan LastLocalClockOffset { get; private set; }

        private const int MaxRttSamples = 3;
        private readonly RunningAvg _rttRunningAvg = new(MaxRttSamples);

        private const int MaxLcoSamples = 5;
        private readonly RunningMedian _lcoRunningMedian = new(MaxLcoSamples);

        public TimeSpan AverageRoundTripTime { get; private set; }
        public TimeSpan AverageLocalClockOffset { get; private set; }

        public RoundTripTimeCalculator(IMatchClient matchClient, IMatchConnectClient matchConnectClient)
        {
            matchConnectClient.ConnectedWithSynchronizationData += OnSynchronized;
            matchClient.Synchronized += OnSynchronized;
        }

        public void OnSynchronized(TimeSynchronizationData data)
        {
            var rtt = data.UnreliableReceivedPingLately ? data.UnreliableRoundTripDelay.Value : data.RoundTripDelay;
            LastRoundTripTime = TimeSpan.FromTicks(rtt.Ticks);
            CalculateNewAverageRoundTripTime(rtt);
            var lco = data.UnreliableReceivedPingLately ? data.UnreliableLocalClockOffset.Value : data.LocalClockOffset;
            LastLocalClockOffset = TimeSpan.FromTicks(lco.Ticks);
            CalculateNewAverageLocalClockOffset(lco);
        }

        private void CalculateNewAverageRoundTripTime(TimeSpan rtt)
        {
            // Log-normal distribution
            var rttLn = Math.Log(rtt.TotalMilliseconds);
            var newRttAvg = _rttRunningAvg.AddAndGetAvg(rttLn);
            AverageRoundTripTime = TimeSpan.FromMilliseconds(Math.Exp(newRttAvg));
        }

        private void CalculateNewAverageLocalClockOffset(TimeSpan lcoTicks)
        {
            var newLco = _lcoRunningMedian.AddAndGetMedian(lcoTicks.TotalMilliseconds);
            AverageLocalClockOffset = TimeSpan.FromMilliseconds(newLco);
        }
    }
}
