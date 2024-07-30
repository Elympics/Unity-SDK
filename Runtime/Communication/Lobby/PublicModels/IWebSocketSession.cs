using System;

#nullable enable

namespace Elympics
{
    public interface IWebSocketSession
    {
        public event Action? Connected;
        public event Action<DisconnectionData>? Disconnected;

        public bool IsConnected { get; }
    }
}
