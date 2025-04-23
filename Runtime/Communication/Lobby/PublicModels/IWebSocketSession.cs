using System;

#nullable enable

namespace Elympics
{
    public interface IWebSocketSession
    {
        [Obsolete("Use instead " + nameof(ElympicsLobbyClient.ElympicsConnectionEstablished))]
        public event Action? Connected;
        [Obsolete("Use instead " + nameof(ElympicsLobbyClient.ElympicsConnectionLost))]
        public event Action<DisconnectionData>? Disconnected;

        public bool IsConnected { get; }
    }
}
