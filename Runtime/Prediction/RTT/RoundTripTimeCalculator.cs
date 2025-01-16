using System;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
    public class RoundTripTimeCalculator : IRoundTripTimeCalculator
    {
        public TimeSpan LastRoundTripTime { get; private set; }
        public TimeSpan LastLocalClockOffset { get; private set; }

        private const int MaxRttSamples = 50;
        private readonly RunningAvg _rttRunningAvg = new(MaxRttSamples);

        private const int MaxLcoSamples = 5;
        private readonly RunningMedian _lcoRunningMedian = new(MaxLcoSamples);

        public TimeSpan AverageRoundTripTime { get; private set; }
        public TimeSpan RoundTripTimeStandardDeviation { get; private set; }
        public TimeSpan AverageLocalClockOffset { get; private set; }

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
            _rttRunningAvg.Add(rtt.TotalMilliseconds);

            var (avg, stdDev) = _rttRunningAvg.GetAvgAndStdDev();

            AverageRoundTripTime = TimeSpan.FromMilliseconds(avg);
            RoundTripTimeStandardDeviation = TimeSpan.FromMilliseconds(stdDev);
        }

        private void CalculateNewAverageLocalClockOffset(TimeSpan lcoTicks)
        {
            var newLco = _lcoRunningMedian.AddAndGetMedian(lcoTicks.TotalMilliseconds);
            AverageLocalClockOffset = TimeSpan.FromMilliseconds(newLco);
        }
    }
}
