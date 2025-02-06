using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;

namespace Elympics
{
    internal abstract class ConnectionStrategy
    {
        private readonly WebSocketSession _webSocketSession;
        private readonly ElympicsLoggerContext _logger;
        protected ConnectionStrategy(WebSocketSession webSocketSession, ElympicsLoggerContext logger)
        {
            _webSocketSession = webSocketSession;
            _logger = logger;
        }

        public abstract UniTask Connect(SessionConnectionDetails newConnectionDetails);

        protected async UniTask ConnectToLobby(SessionConnectionDetails connectionDetails)
        {
            await _webSocketSession.Connect(connectionDetails);
        }

        protected void DisconnectFromLobby(DisconnectionReason reason)
        {
            if (_webSocketSession.IsConnected)
                _webSocketSession.Disconnect(reason);
        }
    }
}
