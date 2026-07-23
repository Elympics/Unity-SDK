#nullable enable

using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.InternalModels.FromLobby;
using Elympics.Core.Logger;
using Elympics.Lobby;

namespace Elympics
{
    internal abstract class ConnectionStrategy
    {
        private readonly WebSocketSession _webSocketSession;
        protected readonly LoggerConfig Logger = ElympicsLogger.WithElympicsSdkService()
            .WithMonitoringEnabled();
        protected ConnectionStrategy(WebSocketSession webSocketSession) => _webSocketSession = webSocketSession;

        /// <summary>
        /// Connects to lobby services, performing a handshake for exchanging client-side and server-side game details.
        /// </summary>
        /// <param name="newConnectionDetails">Client-side game details and authentication data.</param>
        /// <returns>Server-side game details (only if <paramref name="newConnectionDetails"/> changed since last call).</returns>
        public abstract UniTask<GameDataResponseDto?> Connect(SessionConnectionDetails newConnectionDetails);

        protected async UniTask<GameDataResponseDto> ConnectToLobby(SessionConnectionDetails connectionDetails) =>
            await _webSocketSession.Connect(connectionDetails);

        protected void DisconnectFromLobby(DisconnectionReason reason)
        {
            if (_webSocketSession.IsConnected)
                _webSocketSession.Disconnect(reason);
        }
    }
}
