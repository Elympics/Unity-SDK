using System;
using WebRtcWrapper;

namespace MatchTcpLibrary.TransportLayer.WebRtc
{
	public class WebRtcNetworkServer : IDisposable
	{
		private readonly IMatchTcpLibraryLogger _logger;
		private          IWebRtcServer          _webRtcServer;

		public WebRtcNetworkServer(IMatchTcpLibraryLogger logger)
		{
			_logger = logger;
		}

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
			_logger.Debug("WebRtc creating client");
			return _webRtcServer.CreateClient();
		}

		public void Dispose()
		{
			_webRtcServer?.Dispose();
		}
	}
}
