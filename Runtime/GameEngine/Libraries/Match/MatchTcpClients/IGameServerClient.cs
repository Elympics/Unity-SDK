#nullable enable
using System;
using System.Threading;
using Elympics.Cysharp.Threading.Tasks;
using MatchTcpClients.Synchronizer;
using MatchTcpModels.Messages;

namespace MatchTcpClients
{
    public interface IGameServerClient : IDisposable
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
        /// <summary>Sends the join token when set, otherwise the user secret.</summary>
        void AuthenticateMatchUserSecretAsync(string? userSecret, string? joinToken = null);
        /// <summary>The join token must have the spectator role. Game servers that need none ignore it.</summary>
        void AuthenticateAsSpectatorAsync(string? joinToken = null);
        void JoinMatchAsync();
        void SendInGameDataReliable(byte[] data);
        void SendInGameDataUnreliable(byte[] data);
    }
}
