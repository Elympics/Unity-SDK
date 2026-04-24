#nullable enable
using System.Collections.Generic;
using Elympics.ElympicsSystems.Internal;
using WebRtcWrapper;

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal class WebRtcServer : IWebRtcServer
    {
        private readonly List<IWebRtcServerClient> _clients = new();

        private readonly int _port;
        private readonly string? _publicIpOverride;
        private readonly int? _publicPortOverride;
        private readonly ElympicsLoggerContext _logger;

        public WebRtcServer(ElympicsLoggerContext logger) =>
            _logger = logger.WithContext(nameof(WebRtcServer));

        public void Start(bool withReceiveThread = true)
        { }

        public void Stop()
        { }

        public IWebRtcServerClient CreateClient()
        {
            var client = new WebRtcServerClient(_logger);
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
