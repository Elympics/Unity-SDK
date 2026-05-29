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

        UniTask StartContinuousSynchronizingAsync(CancellationToken ct);
        UniTask<TimeSynchronizationData> SynchronizeOnce(CancellationToken ct);

        void ReliablePingReceived(PingClientResponseMessage message);
        void UnreliablePingReceived(PingClientResponseMessage message);
        void SetUnreliableSessionToken(string sessionToken);
    }
}
