using Cysharp.Threading.Tasks;
using Elympics.Lobby;

namespace Elympics
{
    internal abstract class ConnectionStrategy
    {
        protected readonly WebSocketSession WebSocketSession;

        protected ConnectionStrategy(WebSocketSession webSocketSession) => WebSocketSession = webSocketSession;

        public abstract UniTask Connect(SessionConnectionDetails newConnectionDetails);

        protected async UniTask ConnectToLobby(SessionConnectionDetails connectionDetails)
        {
            ElympicsLogger.Log("Connecting to lobby...");
            await WebSocketSession.Connect(connectionDetails);
            ElympicsLogger.Log($"Successfully connected to lobby.\n Connection details: {connectionDetails}");
        }

        protected void DisconnectFromLobby(DisconnectionReason reason)
        {
            if (WebSocketSession.IsConnected)
                WebSocketSession.Disconnect(reason);
        }
    }
}
