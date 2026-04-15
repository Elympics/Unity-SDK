using System;
using System.Net;
using System.Threading.Tasks;
using GameEngine.Libraries.WebRtc;
using Proto.ProtoClient;
using Proto.ProtoClient.NetworkClient;
using Proto.ProtoClient.Receivers;
using WebRtcWrapper;

namespace UnityConnectors.HalfRemote.Server
{
    internal class WebHalfRemoteGameEngineServer : IHalfRemoteGameEngineServer, IWebClientInitializer
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

        private IWebRtcServer? _webRtcListener;
        private bool _running;

        public WebHalfRemoteGameEngineServer(IPEndPoint listenEndpoint, IGameEngineProtoReceiver gameEngineProtoReceiver, IServerNtpReceiver serverNtpReceiver)
        {
            _listenEndpoint = listenEndpoint;
            _gameEngineProtoReceiver = gameEngineProtoReceiver;
            _serverNtpReceiver = serverNtpReceiver;
        }

        public void Start()
        {
            _webRtcListener = new UnityWebRtcServer(_listenEndpoint.Port, "");
            _webRtcListener.Start();

            _running = true;
        }

        public void Stop()
        {
            if (!_running)
                return;
            ListeningEnded?.Invoke(nameof(WebHalfRemoteGameEngineServer));

            _webRtcListener?.Stop();
            _webRtcListener = null;
        }

        public async Task<string> InitClientAndCreateAnswer(string offer)
        {
            var clientId = Guid.NewGuid();

            if (_webRtcListener is null)
                throw new InvalidOperationException("Listener not started");
            var webRtcServerClient = _webRtcListener.CreateClient();
            var reliableProtoNetworkClient = new ProtoNetworkDatagramClient(webRtcServerClient.AsReliableDatagramCommunication());
            var unreliableProtoNetworkClient = new ProtoNetworkDatagramClient(webRtcServerClient.AsUnreliableDatagramCommunication());
            var reliableClient = new GameEngineProtoClient(reliableProtoNetworkClient, _gameEngineProtoReceiver, _serverNtpReceiver, allowUnreliable: false);
            var unreliableClient = new GameEngineProtoClient(unreliableProtoNetworkClient, _gameEngineProtoReceiver, _serverNtpReceiver, allowReliable: false);

            reliableClient.ReceivingError += error => ReliableClientReceivingError?.Invoke(clientId, error);
            reliableClient.ReceivingEnded += () => ReliableClientReceivingEnded?.Invoke(clientId);
            reliableClient.Receive();

            unreliableClient.ReceivingError += error => UnreliableClientReceivingError?.Invoke(clientId, error);
            unreliableClient.ReceivingEnded += () => UnreliableClientReceivingEnded?.Invoke(clientId);
            unreliableClient.Receive();

            try
            {
                var answer = await webRtcServerClient.CreateAnswerAsync(offer);

                webRtcServerClient.ReceiveReliable();
                webRtcServerClient.ReceiveUnreliable();
                ReliableClientConnected?.Invoke(clientId, reliableClient);
                UnreliableClientConnected?.Invoke(clientId, unreliableClient);

                return answer;
            }
            catch (Exception e)
            {
                webRtcServerClient.Close();
                ClientConnectingError?.Invoke(clientId, e.InnerException?.ToString() ?? "");
            }

            return null;
        }

        public void Dispose()
        {
            _webRtcListener?.Dispose();
        }
    }
}
