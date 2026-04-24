using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using GameEngineCore.V1._3;
using Google.Protobuf;
using Proto.ProtoClient;
using Proto.ProtoClient.Receivers;
using ProtoGameEngine;
using ProtoUnityGameEngine;
using UnityConnectors.HalfRemote.Server;

namespace UnityConnectors.HalfRemote
{
    internal class HalfRemoteGameEngineProtoConnector : IDisposable, IGameEngineProtoReceiver, IWebClientInitializer
    {
        private readonly List<IHalfRemoteGameEngineServer> _listeners;
        private readonly IWebClientInitializer _webClientInitializer;
        private readonly IGameEngine _gameEngineAdapter;

        public event Action<Guid, string> ClientConnectingError;

        public event Action<Guid> ReliableClientConnected;
        public event Action<Guid, string> ReliableClientReceivingError;
        public event Action<Guid> ReliableClientReceivingEnded;

        public event Action<Guid> UnreliableClientConnected;
        public event Action<Guid, string> UnreliableClientReceivingError;
        public event Action<Guid> UnreliableClientReceivingEnded;

        public event Action<(string Source, string Error)> ListeningError;
        public event Action<string> ListeningEnded;

        private readonly Dictionary<Guid, GameEngineProtoClient> _reliableClients;
        private readonly Dictionary<Guid, GameEngineProtoClient> _unreliableClients;

        public HalfRemoteGameEngineProtoConnector(IGameEngine gameEngineAdapter, IPEndPoint tcpListenEndpoint, IPEndPoint webListenEndpoint)
        {
            _reliableClients = new Dictionary<Guid, GameEngineProtoClient>();
            _unreliableClients = new Dictionary<Guid, GameEngineProtoClient>();

            var serverNtpReceiver = new ServerNtpReceiver();

            var tcpHalfRemoteGameEngineServer = new TcpHalfRemoteGameEngineServer(tcpListenEndpoint, this, serverNtpReceiver);
            var webHalfRemoteGameEngineServer = new WebHalfRemoteGameEngineServer(webListenEndpoint, this, serverNtpReceiver);

            _webClientInitializer = webHalfRemoteGameEngineServer;
            _listeners = new List<IHalfRemoteGameEngineServer>
            {
                tcpHalfRemoteGameEngineServer,
                webHalfRemoteGameEngineServer,
            };

            foreach (var listener in _listeners)
            {
                listener.ClientConnectingError += OnClientConnectingError;
                listener.ReliableClientConnected += OnReliableClientConnected;
                listener.ReliableClientReceivingError += OnReliableClientReceivingError;
                listener.ReliableClientReceivingEnded += OnReliableClientReceivingEnded;
                listener.UnreliableClientConnected += OnUnreliableClientConnected;
                listener.UnreliableClientReceivingError += OnUnreliableClientReceivingError;
                listener.UnreliableClientReceivingEnded += OnUnreliableClientReceivingEnded;
                listener.ListeningError += OnListeningError;
                listener.ListeningEnded += OnListeningEnded;
            }

            _gameEngineAdapter = gameEngineAdapter;
            _gameEngineAdapter.InGameDataForPlayerOnReliableChannelGenerated += OnInGameDataForPlayerOnReliableChannelGenerated;
            _gameEngineAdapter.InGameDataForPlayerOnUnreliableChannelGenerated += OnInGameDataForPlayerOnUnreliableChannelGenerated;
        }

        private void OnReliableClientConnected(Guid clientId, GameEngineProtoClient client)
        {
            lock (_reliableClients)
                _reliableClients.Add(clientId, client);
            ReliableClientConnected?.Invoke(clientId);
        }

        private void OnReliableClientReceivingEnded(Guid clientId)
        {
            lock (_reliableClients)
                _ = _reliableClients.Remove(clientId);
            ReliableClientReceivingEnded?.Invoke(clientId);
        }

        private void OnReliableClientReceivingError(Guid clientId, string error)
        {
            ReliableClientReceivingError?.Invoke(clientId, error);
        }

        private void OnUnreliableClientConnected(Guid clientId, GameEngineProtoClient client)
        {
            lock (_unreliableClients)
                _unreliableClients.Add(clientId, client);
            UnreliableClientConnected?.Invoke(clientId);
        }

        private void OnClientConnectingError(Guid clientId, string error)
        {
            ClientConnectingError?.Invoke(clientId, error);
        }

        private void OnUnreliableClientReceivingEnded(Guid clientId)
        {
            lock (_unreliableClients)
                _ = _unreliableClients.Remove(clientId);
            UnreliableClientReceivingEnded?.Invoke(clientId);
        }

        private void OnUnreliableClientReceivingError(Guid clientId, string error)
        {
            UnreliableClientReceivingError?.Invoke(clientId, error);
        }

        private void OnListeningEnded(string source) => ListeningEnded?.Invoke(source);
        private void OnListeningError(string source, string error) => ListeningError?.Invoke((source, error));

        public void Listen()
        {
            foreach (var listener in _listeners)
                listener.Start();
        }

        private void OnInGameDataForPlayerOnReliableChannelGenerated(byte[] data, string userId)
        {
            lock (_reliableClients)
            {
                foreach (var client in _reliableClients.Values)
                    client.Send(new InGameDataForPlayerOnReliableChannelGeneratedMsg { UserId = userId, Data = ByteString.CopyFrom(data) });
            }
        }

        private void OnInGameDataForPlayerOnUnreliableChannelGenerated(byte[] data, string userId)
        {
            lock (_unreliableClients)
            {
                foreach (var client in _unreliableClients.Values)
                    client.Send(new InGameDataForPlayerOnUnreliableChannelGeneratedMsg { UserId = userId, Data = ByteString.CopyFrom(data) });
            }
        }

        public void InGameDataFromPlayerReliable(InGameDataReliableReceivedMsg message) => _gameEngineAdapter.OnInGameDataFromPlayerReliableReceived(message.Data.ToByteArray(), message.UserId);
        public void InGameDataFromPlayerUnreliable(InGameDataUnreliableReceivedMsg message) => _gameEngineAdapter.OnInGameDataFromPlayerUnreliableReceived(message.Data.ToByteArray(), message.UserId);

        public void PlayerConnected(PlayerConnectedMsg message) => _gameEngineAdapter.OnPlayerConnected(message.UserId);
        public void PlayerDisconnected(PlayerDisconnectedMsg message) => _gameEngineAdapter.OnPlayerDisconnected(message.UserId);

        public void Tick(TickMsg message) => ThrowNotImplementedException();
        public void Init(InitialMatchDataMsg message) => ThrowNotImplementedException();

        private static void ThrowNotImplementedException() => throw new NotImplementedException("This message is not supported in half-remote mode");

        public void Dispose()
        {
            if (_listeners == null)
                return;

            foreach (var listener in _listeners)
            {
                listener.Stop();
                listener.Dispose();
            }
        }

        public Task<string> InitClientAndCreateAnswer(string offer)
        {
            return _webClientInitializer.InitClientAndCreateAnswer(offer);
        }
    }
}
