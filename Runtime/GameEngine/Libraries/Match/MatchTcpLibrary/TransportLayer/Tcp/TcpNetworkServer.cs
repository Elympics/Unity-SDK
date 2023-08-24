using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Elympics;
using MatchTcpLibrary.TransportLayer.Interfaces;

namespace MatchTcpLibrary.TransportLayer.Tcp
{
    public class TcpNetworkServer : INetworkServer<IReliableNetworkClient>
    {
        public event Action<IReliableNetworkClient> OnAccepted;

        private readonly IPEndPoint _anyEndPoint = new(IPAddress.Any, 0);

        private readonly IMessageEncoder _messageEncoder;
        private readonly TcpProtocolConfig _tcpProtocolConfig;

        public TcpNetworkServer(IMessageEncoder messageEncoder, TcpProtocolConfig tcpProtocolConfig = null)
        {
            _messageEncoder = messageEncoder;
            _tcpProtocolConfig = tcpProtocolConfig ?? TcpProtocolConfig.Default;
        }

        public async Task ListenAsync(IPEndPoint endPoint = null, CancellationToken ct = default)
        {
            var listener = new TcpListener(endPoint ?? _anyEndPoint);
            listener.Start();
            ElympicsLogger.Log($"TCP Network Server started listening on {((IPEndPoint)listener.LocalEndpoint).Port}");


            _ = ct.Register(() =>
            {
                ElympicsLogger.Log("TCP Network Server stopping");
                listener.Stop();
                ElympicsLogger.Log("TCP Network Server stopped");
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
                    ElympicsLogger.Log($"Client {remoteEndPoint} accepted");

                    var tcpNetworkClient = new TcpNetworkClient(_messageEncoder, _tcpProtocolConfig, client);
                    OnAccepted?.Invoke(tcpNetworkClient);

                    ElympicsLogger.Log($"Client {remoteEndPoint} accepted and connected");
                }
            }
            catch (TcpListenerDisposedException)
            {
                // Do nothing because this will be caught on listener.Stop() ~pprzestrzelski 29.05.2020
            }
        }
    }
}
