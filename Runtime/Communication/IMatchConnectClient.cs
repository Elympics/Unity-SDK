using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
	public interface IMatchConnectClient
	{
		event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
		event Action                          ConnectingFailed;

		event Action<string> AuthenticatedUserMatchWithUserId;
		event Action<string> AuthenticatedUserMatchFailedWithError;

		event Action<string> MatchJoinedWithError;
		event Action<string> MatchJoinedWithMatchId;

		event Action<string> MatchEndedWithMatchId;

		event Action DisconnectedByServer;
		event Action DisconnectedByClient;

		IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct);
		IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct);
		void        Disconnect();
	}
}
