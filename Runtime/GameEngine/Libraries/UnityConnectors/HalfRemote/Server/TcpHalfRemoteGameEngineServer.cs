using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Proto.ProtoClient;
using Proto.ProtoClient.NetworkClient;
using Proto.ProtoClient.Receivers;

namespace UnityConnectors.HalfRemote.Server
{
    internal class TcpHalfRemoteGameEngineServer : IHalfRemoteGameEngineServer
    {
        public event Action<Guid, string> ClientConnectingError;

        public event Action<Guid, GameEngineProtoClient> ReliableClientConnected;
        public event Action<Guid, string> ReliableClientReceivingError;
        public event Action<Guid> ReliableClientReceivingEnded;

        public event Action<Guid, GameEngineProtoClient> UnreliableClientConnected;
        public event Action<Guid, string> UnreliableClientReceivingError;
        public event Action<Guid> UnreliableClientReceivingEnded;

        public event Action<string, string> ListeningError;
        public event Action<string> ListeningEnded;

        private readonly IPEndPoint _listenEndpoint;
        private readonly IGameEngineProtoReceiver _gameEngineProtoReceiver;
        private readonly IServerNtpReceiver _serverNtpReceiver;

        private TcpListener _listener;
        private bool _running;

        public TcpHalfRemoteGameEngineServer(IPEndPoint listenEndpoint, IGameEngineProtoReceiver gameEngineProtoReceiver, IServerNtpReceiver serverNtpReceiver)
        {
            _listenEndpoint = listenEndpoint;
            _gameEngineProtoReceiver = gameEngineProtoReceiver;
            _serverNtpReceiver = serverNtpReceiver;
        }

        public void Start()
        {
            _listener = new TcpListener(_listenEndpoint);
            _listener.Start();
            _running = true;
            _ = Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning);
        }

        private async Task Run()
        {
            try
            {
                while (_running)
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    var protoNetworkClient = new ProtoNetworkStreamClient(tcpClient.GetStream());
                    var clientId = Guid.NewGuid();
                    var client = new GameEngineProtoClient(protoNetworkClient, _gameEngineProtoReceiver, _serverNtpReceiver);

                    client.ReceivingError += error =>
                    {
                        ReliableClientReceivingError?.Invoke(clientId, error);
                        UnreliableClientReceivingError?.Invoke(clientId, error);
                    };
                    client.ReceivingEnded += () =>
                    {
                        ReliableClientReceivingEnded?.Invoke(clientId);
                        UnreliableClientReceivingEnded?.Invoke(clientId);
                    };
                    ReliableClientConnected?.Invoke(clientId, client);
                    UnreliableClientConnected?.Invoke(clientId, client);

                    client.Receive();
                }
            }
            catch (ObjectDisposedException)
            {
                // Stopped
            }
            catch (Exception e)
            {
                ListeningError?.Invoke(nameof(TcpHalfRemoteGameEngineServer), e.Message);
            }
            finally
            {
                ListeningEnded?.Invoke(nameof(TcpHalfRemoteGameEngineServer));
            }
        }

        public void Stop()
        {
            if (!_running)
                return;
            _running = false;
            _listener.Stop();
            _listener = null;
        }

        public void Dispose()
        {
        }
    }
}
