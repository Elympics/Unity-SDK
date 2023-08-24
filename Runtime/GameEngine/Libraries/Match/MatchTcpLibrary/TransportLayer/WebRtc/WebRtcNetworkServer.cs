using System;
using Elympics;
using WebRtcWrapper;

namespace MatchTcpLibrary.TransportLayer.WebRtc
{
    public class WebRtcNetworkServer : IDisposable
    {
        private IWebRtcServer _webRtcServer;

        public void Start(int port, string publicIpOverride = null, int? publicPortOverride = null)
        {
            _webRtcServer = new WebRtcServer(port, publicIpOverride, publicPortOverride);
            _webRtcServer.Start();
        }

        public void Stop()
        {
            _webRtcServer?.Stop();
        }

        public IWebRtcServerClient CreateClient()
        {
            ElympicsLogger.Log("WebRtc creating client");
            return _webRtcServer.CreateClient();
        }

        public void Dispose()
        {
            _webRtcServer?.Dispose();
        }
    }
}
