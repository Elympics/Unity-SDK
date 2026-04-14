#nullable enable
using System.Collections.Generic;
using WebRtcWrapper;

namespace GameEngine.Libraries.WebRtc
{
    public class UnityWebRtcServer : IWebRtcServer
    {
        private readonly List<IWebRtcServerClient> _clients = new();

        private readonly int _port;
        private readonly string? _publicIpOverride;
        private readonly int? _publicPortOverride;

        public UnityWebRtcServer(int port, string? publicIpOverride = null, int? publicPortOverride = null)
        {
            _port = port;
            _publicIpOverride = publicIpOverride;
            _publicPortOverride = publicPortOverride;
        }

        public void Start(bool withReceiveThread = true)
        { }

        public void Stop()
        { }

        public IWebRtcServerClient CreateClient()
        {
            var client = new UnityWebRtcServerClient();
            _clients.Add(client);
            return client;
        }

        public void ReceiveReliableOnce()
        { }

        public void ReceiveUnreliableOnce()
        { }

        public void Dispose()
        {
            foreach (var client in _clients)
                client.Dispose();
            _clients.Clear();
        }
    }
}
