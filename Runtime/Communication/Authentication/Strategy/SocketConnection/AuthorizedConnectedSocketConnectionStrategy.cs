using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;

namespace Elympics
{
    internal class AuthorizedConnectedSocketConnectionStrategy : ConnectionStrategy
    {
        private readonly SessionConnectionDetails _currentSession;

        public AuthorizedConnectedSocketConnectionStrategy(WebSocketSession webSocketSession, SessionConnectionDetails currentSession, ElympicsLoggerContext logger) : base(webSocketSession, logger) => _currentSession = currentSession;

        public override async UniTask Connect(SessionConnectionDetails newConnectionDetails)
        {
            if (ConnectionDetailsChanged(newConnectionDetails))
            {
                DisconnectFromLobby(DisconnectionReason.Reconnection);
                await ConnectToLobby(newConnectionDetails);
            }
            else
                ElympicsLogger.LogWarning("No change in connection data.");
        }
        private bool ConnectionDetailsChanged(SessionConnectionDetails connectionDetails) => !_currentSession.Equals(connectionDetails);
    }
}
