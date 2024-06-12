using System;

#nullable enable

namespace Elympics
{
    public interface IWebSocketSession
    {
        public event Action? Connected;
        public event Action? Disconnected;

        public bool IsConnected { get; }
    }
}
