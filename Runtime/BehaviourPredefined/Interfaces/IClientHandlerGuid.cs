using System;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
	public interface IClientHandlerGuid : IObservable
	{
		/// <summary>
		/// Called on standalone client initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
		/// </summary>
		/// <param name="data">Initialization data to be used by the client.</param>
		void OnStandaloneClientInit(InitialMatchPlayerDataGuid data);

		/// <summary>
		/// Called on clients-in-server initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
		/// </summary>
		/// <param name="data">Initialization data of all clients included in the server.</param>
		/// <remarks>Used in "Local Player And Bots" development mode.</remarks>
		void OnClientsOnServerInit(InitialMatchPlayerDatasGuid data);

		void OnConnected(TimeSynchronizationData data);
		void OnConnectingFailed();
		void OnDisconnectedByServer();
		void OnDisconnectedByClient();
		void OnSynchronized(TimeSynchronizationData data);
		void OnAuthenticated(Guid userId);
		void OnAuthenticatedFailed(string errorMessage);
		void OnMatchJoined(Guid matchId);
		void OnMatchJoinedFailed(string errorMessage);
		void OnMatchEnded(Guid matchId);
	}
}
