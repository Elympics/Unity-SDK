using System;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
    /// <summary>A set of callbacks invoked by Elympics in client-specific scenarios.</summary>
    /// <remarks>
    /// Because there are many methods on this interface, they were all provided a default (empty) implementation.
    /// Check the source code for the full list of method you can override.
    /// </remarks>
    public interface IClientHandlerGuid : IObservable
    {
        /// <summary>
        /// Called on standalone client initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
        /// </summary>
        /// <param name="data">Initialization data to be used by the client.</param>
        void OnStandaloneClientInit(InitialMatchPlayerDataGuid data)
        { }

        /// <summary>
        /// Called on clients-in-server initialization (after processing all initial <see cref="ElympicsBehaviour"/>s).
        /// </summary>
        /// <param name="data">Initialization data of all clients included in the server.</param>
        /// <remarks>Used in "Local Player And Bots" development mode.</remarks>
        void OnClientsOnServerInit(InitialMatchPlayerDatasGuid data)
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
        /// <param name="userId">User ID. Empty when joining as a spectator.</param>
        void OnAuthenticated(Guid userId)
        { }
        void OnAuthenticatedFailed(string errorMessage)
        { }

        /// <summary>Called when client successfully joins a match on game server.</summary>
        /// <param name="matchId">Match ID. Can be empty (this will be fixed in the next release).</param>
        void OnMatchJoined(Guid matchId)
        { }
        void OnMatchJoinedFailed(string errorMessage)
        { }
        void OnMatchEnded(Guid matchId)
        { }
    }
}
