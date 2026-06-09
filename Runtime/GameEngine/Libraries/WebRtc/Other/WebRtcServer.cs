#nullable enable
using System.Collections.Generic;
using WebRtcWrapper;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal class WebRtcServer : IWebRtcServer
    {
        private readonly List<IWebRtcServerClient> _clients = new();

        private readonly int _port;
        private readonly string? _publicIpOverride;
        private readonly int? _publicPortOverride;

        public void Start(bool withReceiveThread = true)
        { }

        public void Stop()
        {
            foreach (var client in _clients)
                client.Dispose();
            _clients.Clear();
        }

        public IWebRtcServerClient CreateClient()
        {
            var client = new WebRtcServerClient();
            _clients.Add(client);
            return client;
        }

        public void ReceiveReliableOnce()
        { }

        public void ReceiveUnreliableOnce()
        { }

        public void Dispose() => Stop();
    }
}
