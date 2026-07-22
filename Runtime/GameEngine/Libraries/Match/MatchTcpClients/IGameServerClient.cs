#nullable enable
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

        event Action? Connected;
        event Action<TimeSynchronizationData>? ConnectedAndSynchronized;
        event Action<TimeSynchronizationData>? Synchronized;
        event Action? Disconnected;
        event Action<UserMatchAuthenticatedMessage>? UserMatchAuthenticated;
        event Action<AuthenticatedAsSpectatorMessage>? AuthenticatedAsSpectator;
        event Action<MatchJoinedMessage>? MatchJoined;
        event Action<MatchEndedMessage>? MatchEnded;
        event Action<string, InGameDataMessage>? InGameDataReceived;

        UniTask ConnectAsync(CancellationToken ct = default);
        void Disconnect();
        void AuthenticateMatchUserSecretAsync(string userSecret);
        void AuthenticateAsSpectatorAsync();
        void JoinMatchAsync();
        void SendInGameDataReliable(byte[] data);
        void SendInGameDataUnreliable(byte[] data);
    }
}
