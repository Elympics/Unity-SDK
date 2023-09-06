using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Elympics.Libraries;
using MatchTcpClients.Synchronizer;
using MatchTcpLibrary;
using Proto.ProtoClient.NetworkClient;
using UnityConnectors.HalfRemote;
using UnityEngine;
using WebRtcWrapper;

#pragma warning disable CS0067

namespace Elympics
{
    public class HalfRemoteMatchConnectClient : IMatchConnectClient
    {
        private const int ConnectMaxRetries = 50;
        private const int WaitTimeToRetryConnectInSeconds = 1;
        private static readonly WaitForSeconds WaitTimeToRetryConnect = new(WaitTimeToRetryConnectInSeconds);

        private static readonly WaitForSeconds OfferWaitingInterval = new(1);
        private const int MaxOfferWaitingIntervals = 5;

        public event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
        public event Action ConnectingFailed;
        public event Action<Guid> AuthenticatedUserMatchWithUserId;
        public event Action<string> AuthenticatedUserMatchFailedWithError;
        public event Action AuthenticatedAsSpectator;
        public event Action<string> AuthenticatedAsSpectatorWithError;
        public event Action<string> MatchJoinedWithError;
        public event Action<Guid> MatchJoinedWithMatchId;
        public event Action<Guid> MatchEndedWithMatchId;
        public event Action DisconnectedByServer;
        public event Action DisconnectedByClient;

        private static readonly Guid MatchId = Guid.NewGuid();

        private readonly HalfRemoteMatchClientAdapter _halfRemoteMatchClientAdapter;
        private readonly string _ip;
        private readonly int _port;
        private readonly Guid _userId;
        private readonly bool _useWeb;
        private readonly HttpSignalingClient _signalingClient;

        private TcpClient _tcpClient;
        private IWebRtcClient _webRtcClient;

        private WebSignalingClientResponse _webResponse;

        public HalfRemoteMatchConnectClient(HalfRemoteMatchClientAdapter halfRemoteMatchClientAdapter, string ip, int port, Guid userId, bool useWeb)
        {
            _halfRemoteMatchClientAdapter = halfRemoteMatchClientAdapter;
            _ip = ip;
            _port = port;
            _userId = userId;
            _useWeb = useWeb;
            if (useWeb)
            {
                var baseUri = new Uri($"http://{_ip}:{_port}");
                _signalingClient = new HttpSignalingClient(new Uri(baseUri, "/doSignaling"));
            }
        }

        public IEnumerator ConnectAndJoinAsPlayer(Action<bool> connectedCallback, CancellationToken ct)
        {
            return _useWeb
                ? ConnectUsingWeb(OnConnectedCallback, ct)
                : ConnectUsingTcp(OnConnectedCallback, ct);

            void OnConnectedCallback(bool connected)
            {
                if (!connected)
                    return;

                _halfRemoteMatchClientAdapter.PlayerConnected();
                ConnectedWithSynchronizationData?.Invoke(new TimeSynchronizationData { LocalClockOffset = TimeSpan.Zero, RoundTripDelay = TimeSpan.Zero, UnreliableReceivedAnyPing = false, UnreliableWaitingForFirstPing = true });
                AuthenticatedUserMatchWithUserId?.Invoke(_userId);
                MatchJoinedWithMatchId?.Invoke(MatchId);
            }
        }

        private IEnumerator ConnectUsingTcp(Action<bool> connectedCallback, CancellationToken ct)
        {
            for (var i = 0; i < ConnectMaxRetries; i++)
            {
                if (!Application.isPlaying || ct.IsCancellationRequested)
                    yield break;
                try
                {
                    _tcpClient = new TcpClient();
                    _tcpClient.Connect(IPAddress.Parse(_ip), _port);
                    ElympicsLogger.Log($"TCP connected to {_ip}:{_port}");
                    break;
                }
                catch (Exception e)
                {
                    _tcpClient = null;
                    _ = ElympicsLogger.LogException(e);
                }

                yield return WaitTimeToRetryConnect;
            }

            if (_tcpClient == null)
            {
                connectedCallback.Invoke(false);
                yield break;
            }

            var client = new HalfRemoteMatchClient(_userId.ToString(), new ProtoNetworkStreamClient(_tcpClient.GetStream()));
            yield return _halfRemoteMatchClientAdapter.ConnectToServer(connectedCallback, _userId.ToString(), client);
        }

        private IEnumerator ConnectUsingWeb(Action<bool> connectedCallback, CancellationToken ct)
        {
            _webRtcClient = WebRtcFactory.CreateInstance();
            string offer = null;
            var offerSet = false;
            _webRtcClient.OfferCreated += s =>
            {
                offer = s;
                offerSet = true;
            };
            _webRtcClient.CreateOffer();

            for (var i = 0; i < MaxOfferWaitingIntervals; i++)
            {
                if (offerSet)
                    break;
                yield return OfferWaitingInterval;
            }

            if (!offerSet)
            {
                ElympicsLogger.LogError("Offer not received from WebRTC client.");
                yield break;
            }
            if (string.IsNullOrEmpty(offer))
            {
                ElympicsLogger.LogError("Offer is null or empty.");
                yield break;
            }

            string answer = null;
            for (var i = 0; i < ConnectMaxRetries; i++)
            {
                if (!Application.isPlaying)
                    yield break;

                _webResponse = null;
                _signalingClient.ReceivedResponse += r => _webResponse = r;
                _signalingClient.PostOfferAsync(offer, 1, ct);
                yield return WaitTimeToRetryConnect;

                if (_webResponse?.IsError == false)
                {
                    answer = _webResponse.Text;
                    break;
                }
                ElympicsLogger.LogError(_webResponse?.IsError == true
                    ? _webResponse.Text
                    : "Response not received from WebRTC client.");
            }

            if (string.IsNullOrEmpty(answer))
            {
                ElympicsLogger.LogError("WebRTC answer is empty because of a connection error "
                    + "or an issue with signaling server.");
                connectedCallback.Invoke(false);
                yield break;
            }


            var channelOpened = false;
            var client = new HalfRemoteMatchClient(_userId.ToString(), _webRtcClient);

            void OnChannelOpened(byte[] data, string playerId)
            {
                channelOpened = true;
            }

            client.InGameDataForPlayerOnUnreliableChannelGenerated += OnChannelOpened;

            _webRtcClient.OnAnswer(answer);

            for (var i = 0; i < ConnectMaxRetries; i++)
            {
                if (!Application.isPlaying)
                    yield break;

                if (channelOpened)
                    break;
                yield return WaitTimeToRetryConnect;
            }

            client.InGameDataForPlayerOnUnreliableChannelGenerated -= OnChannelOpened;

            if (!channelOpened)
            {
                ElympicsLogger.LogError("WebRTC channel not open after "
                    + $"{ConnectMaxRetries * WaitTimeToRetryConnectInSeconds} seconds.");
                connectedCallback.Invoke(false);
                yield break;
            }

            ElympicsLogger.Log("WebRTC received channel opened.");

            yield return _halfRemoteMatchClientAdapter.ConnectToServer(connectedCallback, _userId.ToString(), client);
        }

        public IEnumerator ConnectAndJoinAsSpectator(Action<bool> connectedCallback, CancellationToken ct)
        {
            connectedCallback?.Invoke(false);
            yield break;
        }

        public void Disconnect()
        {
            _halfRemoteMatchClientAdapter.PlayerDisconnected();
            _tcpClient?.Dispose();
            _webRtcClient?.Dispose();
            DisconnectedByClient?.Invoke();
        }

        public void Dispose() => Disconnect();
    }
}
