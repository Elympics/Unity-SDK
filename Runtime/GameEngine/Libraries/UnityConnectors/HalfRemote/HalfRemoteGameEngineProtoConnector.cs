using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Elympics.Communication.Models;
using GameEngineCore;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Proto.ProtoClient;
using Proto.ProtoClient.Receivers;
using ProtoGameEngine;
using ProtoUnityGameEngine;
using UnityConnectors.HalfRemote.Server;
#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine;
#endif

namespace UnityConnectors.HalfRemote
{
    internal class HalfRemoteGameEngineProtoConnector : IDisposable, IGameEngineProtoReceiver, IWebClientInitializer
    {
        private readonly List<IHalfRemoteGameEngineServer> _listeners;
        private readonly IWebClientInitializer _webClientInitializer;
        private readonly IGameEngine _gameEngineAdapter;

        private static readonly Guid MatchId = Guid.NewGuid();

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

        private readonly Action _onAfterGameEnded;

        /// <summary>Set once the server disconnects its clients, which then aren't reported as leaving.</summary>
        private volatile bool _stopped;

        /// <param name="tcpListenEndpoint">Where to listen for TCP/UDP clients, or null not to.</param>
        /// <param name="webListenEndpoint">Where to listen for WebRTC clients, or null not to.</param>
        public HalfRemoteGameEngineProtoConnector(IGameEngine gameEngineAdapter, IPEndPoint tcpListenEndpoint, IPEndPoint webListenEndpoint, Action onAfterGameEnded = null)
        {
            if (tcpListenEndpoint == null && webListenEndpoint == null)
                throw new ArgumentException("At least one listen endpoint is required.");

            _reliableClients = new Dictionary<Guid, GameEngineProtoClient>();
            _unreliableClients = new Dictionary<Guid, GameEngineProtoClient>();

            var serverNtpReceiver = new ServerNtpReceiver();

            _listeners = new List<IHalfRemoteGameEngineServer>();
            if (tcpListenEndpoint != null)
                _listeners.Add(new TcpHalfRemoteGameEngineServer(tcpListenEndpoint, this, serverNtpReceiver));
            if (webListenEndpoint != null)
            {
                var webHalfRemoteGameEngineServer = new WebHalfRemoteGameEngineServer(webListenEndpoint, this, serverNtpReceiver);
                _webClientInitializer = webHalfRemoteGameEngineServer;
                _listeners.Add(webHalfRemoteGameEngineServer);
            }

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
            _onAfterGameEnded = onAfterGameEnded ?? QuitAfterGameEnded;
            _gameEngineAdapter.GameEnded += OnGameEnded;
        }

        private void OnGameEnded(ResultMatchUserDatas result)
        {
            _stopped = true;
            var msg = new NullableGameEndedMsg();
            if (result == null)
                msg.Null = NullValue.NullValue;
            else
                msg.Data = new UserDatas
                {
                    Data =
                    {
                        result.Select(x => new UserDatas.Types.UserData
                        {
                            UserId = $"{MatchId}.{x.UserId}",
                            MatchmakerData = { x.MatchmakerData ?? Array.Empty<float>() },
                            GameEngineData = x.GameEngineData == null ? ByteString.Empty : ByteString.CopyFrom(x.GameEngineData),
                        }),
                    },
                };

            lock (_reliableClients)
                foreach (var client in _reliableClients.Values)
                    client.Send(msg);

            DisconnectClients();
            _onAfterGameEnded();
        }

        private static void QuitAfterGameEnded()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void DisconnectClients()
        {
            _stopped = true;
            foreach (var listener in _listeners)
                listener.Stop();
        }

        private void OnReliableClientConnected(Guid clientId, GameEngineProtoClient client)
        {
            lock (_reliableClients)
                _reliableClients.Add(clientId, client);
            ReliableClientConnected?.Invoke(clientId);
        }

        private void OnReliableClientReceivingEnded(Guid clientId)
        {
            string userId = null;
            lock (_reliableClients)
                if (_reliableClients.Remove(clientId, out var client))
                {
                    userId = client.PlayerUserId;
                    // The player may have rejoined over another client before this one timed out (e.g. a refreshed browser tab).
                    if (userId != null && _reliableClients.Values.Any(other => other.PlayerUserId == userId))
                        userId = null;
                }
            ReliableClientReceivingEnded?.Invoke(clientId);

            // A closed browser tab or a quitting player can't send PlayerDisconnectedMsg.
            if (userId != null && !_stopped)
                _gameEngineAdapter.OnPlayerDisconnected(userId);
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

            _stopped = true;
            foreach (var listener in _listeners)
            {
                listener.Stop();
                listener.Dispose();
            }
        }

        public Task<string> InitClientAndCreateAnswer(OfferWithCandidates offer)
        {
            return (_webClientInitializer ?? throw new InvalidOperationException("The half-remote server does not listen on WebRTC.")).InitClientAndCreateAnswer(offer);
        }
    }
}
