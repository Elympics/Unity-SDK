using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MatchTcpModels.Commands;
using MatchTcpModels.Messages;

namespace MatchTcpClients.Synchronizer
{
	internal interface IClientSynchronizer
	{
		event Action<PingClientCommand> ReliablePingGenerated;
		event Action<PingClientCommand> UnreliablePingGenerated;
		event Action<AuthenticateUnreliableSessionTokenCommand> AuthenticateUnreliableGenerated;

		event Action<TimeSynchronizationData> Synchronized;
		event Action TimedOut;

		/// <summary>
		/// Starts synchronizing with IEnumerator
		/// </summary>
		/// <param name="ct"></param>
		/// <returns>Delay to wait for next run in seconds</returns>
		Task StartContinuousSynchronizingAsync(CancellationToken ct);

		/// <summary>
		/// Method to synchronize times once with timeout
		/// </summary>
		/// <param name="ct">Cancellation token to stop synchronizing</param>
		/// <returns>Correct synchronization data or null if cancelled or timed out</returns>
		Task<TimeSynchronizationData> SynchronizeOnce(CancellationToken ct);

		void ReliablePingReceived(PingClientResponseMessage message);
		void UnreliablePingReceived(PingClientResponseMessage message);
		void SetUnreliableSessionToken(string sessionToken);
	}
}
