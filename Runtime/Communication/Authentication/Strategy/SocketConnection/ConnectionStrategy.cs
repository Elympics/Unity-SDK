#nullable enable
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Rooms.Models;

namespace Elympics
{
    internal abstract class ConnectionStrategy
    {
        private readonly WebSocketSession _webSocketSession;
        protected readonly ElympicsLoggerContext Logger;
        protected ConnectionStrategy(WebSocketSession webSocketSession, ElympicsLoggerContext logger)
        {
            _webSocketSession = webSocketSession;
            Logger = logger;
        }

        public abstract UniTask<GameDataResponse?> Connect(SessionConnectionDetails newConnectionDetails);

        protected async UniTask<GameDataResponse> ConnectToLobby(SessionConnectionDetails connectionDetails) => await _webSocketSession.Connect(connectionDetails);

        protected void DisconnectFromLobby(DisconnectionReason reason)
        {
            if (_webSocketSession.IsConnected)
                _webSocketSession.Disconnect(reason);
        }
    }
}
