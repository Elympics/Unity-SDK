using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        /// Performs a single synchronization (request-response).
        /// </summary>
        /// <param name="sessionToken">The ID of the game server connection received after the session is established.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Delay to wait for next run in seconds</returns>
        UniTask<TimeSynchronizationData> SynchronizeOnce(string sessionToken, CancellationToken ct);

        /// <summary>
        /// Performs continuous synchronization (multiple request-response).
        /// </summary>
        /// <param name="sessionToken">The ID of the game server connection received after the session is established.</param>
        /// <param name="ct">Cancellation token to stop the synchronization.</param>
        /// <returns>Correct synchronization data or null if canceled or timed out</returns>
        UniTask StartContinuousSynchronizingAsync(string sessionToken, CancellationToken ct);

        void ReliablePingReceived(PingClientResponseMessage message);
        void UnreliablePingReceived(PingClientResponseMessage message);
    }
}
