using System;
using MatchTcpClients.Synchronizer;
using UnityEngine;

namespace Elympics
{
	public class RoundTripTimeCalculator : IRoundTripTimeCalculator
	{
		public TimeSpan LastRoundTripTime { get; private set; }


		private          long   _lastRtt;
		private          bool   _lastRttJitterIncrease;
		private          bool   _lastRttJitterDecrease;
		private const    double MaxJitterIncrease = 1.5;
		private const    double MaxJitterDecrease = 1.0 / MaxJitterIncrease;

		public TimeSpan AverageRoundTripTime { get; private set; }

		public RoundTripTimeCalculator(IMatchClient matchClient, IMatchConnectClient matchConnectClient)
		{
			matchConnectClient.ConnectedWithSynchronizationData += OnSynchronized;
			matchClient.Synchronized += OnSynchronized;
		}

		public void OnSynchronized(TimeSynchronizationData data)
		{
			var rtt = data.UnreliableReceivedPingLately ? data.UnreliableRoundTripDelay : data.RoundTripDelay;
			LastRoundTripTime = TimeSpan.FromTicks(rtt.Value.Ticks);
			CalculateNewAverageRoundTripTime(rtt.Value.Ticks);
		}

		private void CalculateNewAverageRoundTripTime(long rtt)
		{
			if (_lastRtt == 0)
			{
				_lastRtt = rtt;
				return;
			}

			var jitterIncrease = false;
			var jitterDecrease = false;
			var div = (double) rtt / _lastRtt;
			if (div > MaxJitterIncrease)
				jitterIncrease = true;
			if (div < MaxJitterDecrease)
				jitterDecrease = true;

			// Constant change
			if (jitterIncrease && _lastRttJitterIncrease || jitterDecrease && _lastRttJitterDecrease)
			{
				// Update rtt to jittered
				_lastRtt = rtt;
				AverageRoundTripTime = TimeSpan.FromTicks(rtt);
				_lastRttJitterDecrease = false;
				_lastRttJitterIncrease = false;
			}
			// Not constant change (last decreased, now increased etc.) or jitter just appeared - hold rtt
			else if (jitterIncrease || jitterDecrease)
			{
				_lastRttJitterIncrease = jitterIncrease;
				_lastRttJitterDecrease = jitterDecrease;
			}
			// No jitter, slowly change average rtt
			else
			{
				_lastRttJitterIncrease = false;
				_lastRttJitterDecrease = false;

				var currentAvg = AverageRoundTripTime.Ticks;
				currentAvg *= 2;
				currentAvg += rtt;
				currentAvg /= 3;
				AverageRoundTripTime = TimeSpan.FromTicks(currentAvg);
			}
		}
	}
}
