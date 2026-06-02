using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Models;
using Elympics.Communication.Models.Public;
using Elympics.GameEngine.Libraries.WebRtc;
using MatchTcpClients.Synchronizer;
using MatchTcpLibrary;
using Proto.ProtoClient.NetworkClient;
using UnityConnectors.HalfRemote;
using UnityEngine;
using WebRtcWrapper;

namespace Elympics
{
    internal class HalfRemoteMatchConnectClient : IMatchConnectClient
    {
        private const int ConnectMaxRetries = 50;
        private const int WaitTimeToRetryConnectInSeconds = 1;
        private static readonly TimeSpan ServerReachingTimeout = TimeSpan.FromSeconds(1);

        private static readonly TimeSpan OfferWaitingInterval = TimeSpan.FromSeconds(1);
        private const int MaxOfferWaitingIntervals = 5;

        public event Action<TimeSynchronizationData> ConnectedWithSynchronizationData;
        public event Action ConnectingFailed;
        public event Action<Guid> AuthenticatedUserMatchWithUserId;
        public event Action<string> AuthenticatedUserMatchFailedWithError;
        public event Action AuthenticatedAsSpectator;
        public event Action<string> AuthenticatedAsSpectatorWithError;
        public event Action<string> MatchJoinedWithError;
        public event Action<MatchInitialData> MatchJoinedWithMatchInitData;
        public event Action<Guid> MatchEndedWithMatchId;
        public event Action DisconnectedByServer;
        public event Action DisconnectedByClient;

        private readonly HalfRemoteMatchClientAdapter _halfRemoteMatchClientAdapter;
        private readonly IPAddress _ip;
        private readonly int _port;
        private readonly Guid _userId;
        private readonly MatchInitialData _halfRemoteMatchInitialData;
        private readonly List<PlayerInitialData> _players;
        private readonly bool _useWeb;
        private readonly ClientConnectionSettings _connectionConfig;
        private readonly HttpSignalingClient _signalingClient;

        private TcpClient _tcpClient;
        private IWebRtcClient _webRtcClient;

        public HalfRemoteMatchConnectClient(HalfRemoteMatchClientAdapter halfRemoteMatchClientAdapter, ElympicsGameConfig gameConfig, Guid userId, MatchInitialData halfRemoteMatchInitialData)
        {
            _halfRemoteMatchClientAdapter = halfRemoteMatchClientAdapter;
            _ip = IPAddress.Parse(gameConfig.IpForHalfRemoteMode);
            _port = gameConfig.PortForHalfRemoteMode;
            _userId = userId;
            _halfRemoteMatchInitialData = halfRemoteMatchInitialData;
            _useWeb = gameConfig.UseWeb;
            _connectionConfig = gameConfig.ConnectionConfig;
            if (_useWeb)
            {
                var baseUri = new Uri($"http://{_ip}:{_port}");
                _signalingClient = new HttpSignalingClient(new Uri(baseUri, "/v2"), Guid.Empty);
            }

            halfRemoteMatchClientAdapter.MatchEnded += OnMatchEnded;
            halfRemoteMatchClientAdapter.Disconnected += OnDisconnected;
        }

        private void OnMatchEnded(Guid matchId)
        {
            ElympicsLogger.Log($"Match {matchId} has ended!");
            MatchEndedWithMatchId?.Invoke(matchId);
        }

        private void OnDisconnected()
        {
            ElympicsLogger.Log("Disconnected by server!");
            DisconnectedByServer?.Invoke();
        }

        public async UniTask ConnectAndJoinAsPlayerAsync(CancellationToken ct)
        {
            var client = _useWeb ? await ConnectWebAsync(ct) : await ConnectTcpAsync(ct);

            _halfRemoteMatchClientAdapter.SetupClientCallbacks(_userId.ToString(), client);

            _halfRemoteMatchClientAdapter.PlayerConnected();
            ConnectedWithSynchronizationData?.Invoke(TimeSynchronizationData.Localhost);
            AuthenticatedUserMatchWithUserId?.Invoke(_userId);
            MatchJoinedWithMatchInitData?.Invoke(_halfRemoteMatchInitialData);

            _halfRemoteMatchClientAdapter.StartSynchronization(ct).Forget();
        }

        private async UniTask<HalfRemoteMatchClient> ConnectTcpAsync(CancellationToken ct)
        {
            for (var i = 0; i < ConnectMaxRetries; i++)
            {
                if (!Application.isPlaying || ct.IsCancellationRequested)
                    throw new OperationCanceledException(ct);

                if (i > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(WaitTimeToRetryConnectInSeconds), DelayType.Realtime, cancellationToken: ct);
                    ElympicsLogger.Log($"Retrying...\nConnecting to TCP server {_ip}:{_port}...");
                }

                var tcpClient = new TcpClient();
                try
                {
                    await tcpClient.ConnectAsync(_ip, _port).AsUniTask().WithTimeout(ServerReachingTimeout, ct);
                    ElympicsLogger.Log($"TCP client successfully connected to {_ip}:{_port}");
                    _tcpClient = tcpClient;
                    return new HalfRemoteMatchClient(_userId.ToString(), new ProtoNetworkStreamClient(tcpClient.GetStream()));
                }
                catch (OperationCanceledException)
                {
                    tcpClient.Dispose();
                    throw;
                }
                catch (Exception e)
                {
                    ElympicsLogger.LogError($"TCP client could not connect to {_ip}:{_port}");
                    _ = ElympicsLogger.LogException(e);
                }
                tcpClient.Dispose();
            }

            throw new ElympicsException($"Failed to connect to TCP server {_ip}:{_port} after {ConnectMaxRetries} retries.");
        }

        private async UniTask<HalfRemoteMatchClient> ConnectWebAsync(CancellationToken ct)
        {
            _webRtcClient = WebRtcFactory.CreateClient(new WebRtcConfig
            {
                OfferAnnounceDelay = TimeSpan.FromSeconds(_connectionConfig.webRtcOfferAnnounceDelay),
            });

            var offer = await _webRtcClient.CreateOffer(false).WithTimeout(MaxOfferWaitingIntervals * OfferWaitingInterval, ct);

            if (string.IsNullOrEmpty(offer))
                throw new ElympicsException("Offer not received from WebRTC client.");

            string answer = null;
            for (var i = 0; i < ConnectMaxRetries; i++)
            {
                if (!Application.isPlaying || ct.IsCancellationRequested)
                    throw new OperationCanceledException(ct);

                if (i > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(WaitTimeToRetryConnectInSeconds), DelayType.Realtime, cancellationToken: ct);
                    ElympicsLogger.Log("Retrying...\nSending the offer to the signaling server...");
                }

                WebSignalingClientResponse result;
                try
                {
                    result = await _signalingClient.PostOfferAsync(offer, ServerReachingTimeout, ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception e)
                {
                    result = new WebSignalingClientResponse { IsError = true, Text = e.Message + '\n' + e.StackTrace };
                }

                if (result.IsError)
                    ElympicsLogger.LogError("Error occurred while awaiting an answer from the signaling server: " + result.Text);
                else
                    try
                    {
                        var signalingResponse = JsonUtility.FromJson<SignalingResponse>(result.Text);
                        answer = signalingResponse.answer;
                        break;
                    }
                    catch (Exception ex)
                    {
                        ElympicsLogger.LogError($"Failed to deserialize the answer from the signaling server: {ex.Message}\n{result.Text}");
                    }
            }

            if (string.IsNullOrEmpty(answer))
                throw new ElympicsException("WebRTC answer is empty because of a connection error or an issue with signaling server.");

            var channelOpenedTcs = new UniTaskCompletionSource();
            var client = new HalfRemoteMatchClient(_userId.ToString(), _webRtcClient);

            void OnChannelOpened() => channelOpenedTcs.TrySetResult();
            _webRtcClient.UnreliableChannelOpened += OnChannelOpened;
            await _webRtcClient.OnAnswer(answer);

            try
            {
                await channelOpenedTcs.Task.WithTimeout(TimeSpan.FromSeconds(ConnectMaxRetries * WaitTimeToRetryConnectInSeconds), ct);
            }
            catch (TimeoutException)
            {
                throw new ElympicsException($"WebRTC channel not open after {ConnectMaxRetries * WaitTimeToRetryConnectInSeconds} seconds.");
            }
            finally
            {
                _webRtcClient.UnreliableChannelOpened -= OnChannelOpened;
            }

            ElympicsLogger.Log("WebRTC received channel opened.");
            return client;
        }

        public UniTask ConnectAndJoinAsSpectatorAsync(CancellationToken ct) =>
            UniTask.FromException(new ElympicsException("HalfRemote mode does not support spectator connections."));

        public void Disconnect()
        {
            _halfRemoteMatchClientAdapter.PlayerDisconnected();
            _tcpClient?.Dispose();
            _webRtcClient?.Dispose();
            ElympicsLogger.Log("Disconnected by client!");
            DisconnectedByClient?.Invoke();
        }

        public void Dispose() => Disconnect();
    }
}
