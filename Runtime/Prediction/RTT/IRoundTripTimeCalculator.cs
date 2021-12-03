using System;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
	public interface IRoundTripTimeCalculator
	{
		TimeSpan LastRoundTripTime    { get; }
		TimeSpan AverageRoundTripTime { get; }

		void OnSynchronized(TimeSynchronizationData data);
	}
}
