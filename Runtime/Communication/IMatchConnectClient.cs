using System;
using System.Collections;
using System.Threading;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
	public interface IMatchConnectClient : IDisposable
	{
		event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
		event Action                          ConnectingFailed;

		event Action<Guid>   AuthenticatedUserMatchWithUserId;
		event Action<string> AuthenticatedUserMatchFailedWithError;

		event Action         AuthenticatedAsSpectator;
		event Action<string> AuthenticatedAsSpectatorWithError;

		event Action<string> MatchJoinedWithError;
		event Action<Guid>   MatchJoinedWithMatchId;

		event Action<Guid> MatchEndedWithMatchId;

		event Action DisconnectedByServer;
		event Action DisconnectedByClient;

		IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct);
		IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct);
		void        Disconnect();
	}
}
