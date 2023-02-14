using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MatchTcpLibrary.TransportLayer.Interfaces;

namespace MatchTcpLibrary.TransportLayer.Tcp
{
	public class TcpNetworkServer : INetworkServer<IReliableNetworkClient>
	{
		public event Action<IReliableNetworkClient> OnAccepted;

		private readonly IPEndPoint _anyEndPoint = new IPEndPoint(IPAddress.Any, 0);

		private readonly IMatchTcpLibraryLogger _logger;
		private readonly IMessageEncoder        _messageEncoder;
		private readonly TcpProtocolConfig      _tcpProtocolConfig;

		public TcpNetworkServer(IMatchTcpLibraryLogger logger, IMessageEncoder messageEncoder, TcpProtocolConfig tcpProtocolConfig = null)
		{
			_logger = logger;
			_messageEncoder = messageEncoder;
			_tcpProtocolConfig = tcpProtocolConfig ?? TcpProtocolConfig.Default;
		}

		public async Task ListenAsync(IPEndPoint endPoint = null, CancellationToken ct = default)
		{
			var listener = new TcpListener(endPoint ?? _anyEndPoint);
			listener.Start();
			_logger.Debug($"TCP Network Server started listening on {((IPEndPoint) listener.LocalEndpoint).Port}");


			ct.Register(() =>
			{
				_logger.Debug($"TCP Network Server stopping");
				listener.Stop();
				_logger.Debug($"TCP Network Server stopped");
			});

			try
			{
				while (!ct.IsCancellationRequested)
				{
					TcpClient client;
					try
					{
						client = await listener.AcceptTcpClientAsync();
					}
					catch (ObjectDisposedException)
					{
						throw new TcpListenerDisposedException();
					}

					var remoteEndPoint = client.Client.RemoteEndPoint;
					_logger.Debug("Client {0} accepted", remoteEndPoint);

					var tcpNetworkClient = new TcpNetworkClient(_logger, _messageEncoder, _tcpProtocolConfig, client);
					OnAccepted?.Invoke(tcpNetworkClient);

					_logger.Info("Client {0} accepted and connected", remoteEndPoint);
				}
			}
			catch (TcpListenerDisposedException)
			{
				// Do nothing because this will be caught on listener.Stop() ~pprzestrzelski 29.05.2020
			}
		}
	}
}
