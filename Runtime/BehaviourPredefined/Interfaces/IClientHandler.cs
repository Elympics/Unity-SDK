using System;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
    [Obsolete("Use " + nameof(IClientHandlerGuid) + " instead")]
    public interface IClientHandler : IObservable
    {
        /// <summary>
        /// Called on standalone client initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
        /// </summary>
        /// <param name="data">Initialization data to be used by the client.</param>
        void OnStandaloneClientInit(InitialMatchPlayerData data)
        { }

        /// <summary>
        /// Called on clients-in-server initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
        /// </summary>
        /// <param name="data">Initialization data of all clients included in the server.</param>
        /// <remarks>Used in "Local Player And Bots" development mode.</remarks>
        void OnClientsOnServerInit(InitialMatchPlayerDatas data)
        { }

        void OnConnected(TimeSynchronizationData data)
        { }
        void OnConnectingFailed()
        { }
        void OnDisconnectedByServer()
        { }
        void OnDisconnectedByClient()
        { }
        void OnSynchronized(TimeSynchronizationData data)
        { }

        /// <summary>Called when client successfully authenticates on game server.</summary>
        /// <param name="userId">User ID. Null when joining as a spectator.</param>
        void OnAuthenticated(string userId)
        { }
        void OnAuthenticatedFailed(string errorMessage)
        { }

        /// <summary>Called when client successfully joins a match on game server.</summary>
        /// <param name="matchId">Match ID. Can be null (this will be fixed in the next release).</param>
        void OnMatchJoined(string matchId)
        { }
        void OnMatchJoinedFailed(string errorMessage)
        { }
        void OnMatchEnded(string matchId)
        { }
    }
}
