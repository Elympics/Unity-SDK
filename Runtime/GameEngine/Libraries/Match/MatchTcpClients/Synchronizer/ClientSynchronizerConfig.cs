using System;

namespace MatchTcpClients.Synchronizer
{
	public struct ClientSynchronizerConfig
	{
		public TimeSpan TimeoutTime { get; set; }
		public TimeSpan ContinuousSynchronizationMinimumInterval { get; set; }
		public TimeSpan UnreliablePingTimeoutInMilliseconds { get; set; }
	}
}
