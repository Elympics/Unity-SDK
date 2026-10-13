using System;
using System.Threading;
using Elympics.Communication.Models.Public;
using Elympics.Cysharp.Threading.Tasks;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
    internal interface IMatchConnectClient : IDisposable
    {
        event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
        event Action ConnectingFailed;

        event Action<Guid> AuthenticatedUserMatchWithUserId;
        event Action<string> AuthenticatedUserMatchFailedWithError;

        event Action AuthenticatedAsSpectator;
        event Action<string> AuthenticatedAsSpectatorWithError;

        event Action<string> MatchJoinedWithError;
        event Action<MatchInitialData> MatchJoinedWithMatchInitData;

        event Action<Guid> MatchEndedWithMatchId;

        event Action DisconnectedByServer;
        event Action DisconnectedByClient;

        UniTask ConnectAndJoinAsPlayerAsync(CancellationToken ct);
        UniTask ConnectAndJoinAsSpectatorAsync(CancellationToken ct);
        void Disconnect();
    }
}
