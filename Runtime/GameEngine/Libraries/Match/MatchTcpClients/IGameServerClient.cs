using System;
using System.Threading;
using System.Threading.Tasks;
using MatchTcpClients.Synchronizer;
using MatchTcpModels.Messages;

namespace MatchTcpClients
{
    public interface IGameServerClient
    {
        bool IsConnected { get; }
        bool IsUnreliableConnected { get; }
        string SessionToken { get; }

        event Action Connected;
        event Action<TimeSynchronizationData> ConnectedAndSynchronized;
        event Action<TimeSynchronizationData> Synchronized;
        event Action Disconnected;
        event Action<UserMatchAuthenticatedMessage> UserMatchAuthenticated;
        event Action<AuthenticatedAsSpectatorMessage> AuthenticatedAsSpectator;
        event Action<MatchJoinedMessage> MatchJoined;
        event Action<MatchEndedMessage> MatchEnded;
        event Action<InGameDataMessage> InGameDataReliableReceived;
        event Action<InGameDataMessage> InGameDataUnreliableReceived;

        Task<bool> ConnectAsync(CancellationToken ct = default);
        void Disconnect();
        Task AuthenticateMatchUserSecretAsync(string userSecret);
        Task AuthenticateAsSpectatorAsync();
        Task JoinMatchAsync();
        Task SendInGameDataReliableAsync(byte[] data);
        Task SendInGameDataUnreliableAsync(byte[] data);
    }
}
