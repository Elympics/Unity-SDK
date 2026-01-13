using System;
using System.Collections;
using System.Threading;
using Elympics.Communication.Models.Public;
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

        IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct);
        IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct);
        void Disconnect();
    }
}
