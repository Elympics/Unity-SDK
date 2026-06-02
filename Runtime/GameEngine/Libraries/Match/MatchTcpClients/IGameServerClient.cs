using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MatchTcpClients.Synchronizer;
using MatchTcpModels.Messages;

namespace MatchTcpClients
{
    public interface IGameServerClient
    {
        bool IsConnected { get; }

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

        UniTask ConnectAsync(CancellationToken ct = default);
        void Disconnect();
        UniTask AuthenticateMatchUserSecretAsync(string userSecret);
        UniTask AuthenticateAsSpectatorAsync();
        UniTask JoinMatchAsync();
        UniTask SendInGameDataReliableAsync(byte[] data);
        UniTask SendInGameDataUnreliableAsync(byte[] data);
    }
}
