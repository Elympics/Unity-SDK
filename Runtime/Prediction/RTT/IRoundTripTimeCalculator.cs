using System;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
	public interface IRoundTripTimeCalculator
	{
		TimeSpan LastRoundTripTime    { get; }
		TimeSpan LastLocalClockOffset { get; }

		TimeSpan AverageRoundTripTime    { get; }
		TimeSpan AverageLocalClockOffset { get; }

		void OnSynchronized(TimeSynchronizationData data);
	}
}
