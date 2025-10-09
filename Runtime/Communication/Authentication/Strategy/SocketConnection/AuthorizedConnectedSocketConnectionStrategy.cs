using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.InternalModels.FromLobby;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;

#nullable enable

namespace Elympics
{
    internal class AuthorizedConnectedSocketConnectionStrategy : ConnectionStrategy
    {
        private readonly SessionConnectionDetails _currentSession;

        public AuthorizedConnectedSocketConnectionStrategy(WebSocketSession webSocketSession, SessionConnectionDetails currentSession, ElympicsLoggerContext logger) : base(webSocketSession, logger) =>
            _currentSession = currentSession;

        public override async UniTask<GameDataResponseDto?> Connect(SessionConnectionDetails newConnectionDetails)
        {
            if (ConnectionDetailsChanged(newConnectionDetails))
            {
                DisconnectFromLobby(DisconnectionReason.Reconnection);
                return await ConnectToLobby(newConnectionDetails);
            }
            ElympicsLogger.LogWarning("No change in connection data.");
            return null;
        }

        private bool ConnectionDetailsChanged(SessionConnectionDetails connectionDetails) =>
            !_currentSession.Equals(connectionDetails);
    }
}
