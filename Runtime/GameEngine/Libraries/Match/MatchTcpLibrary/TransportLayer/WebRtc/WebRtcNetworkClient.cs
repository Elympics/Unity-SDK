#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using Elympics.Communication.Models;
using Elympics.Communication.Utils;
using Elympics.Core.Logger;
using Elympics.ElympicsSystems.Internal;
using Elympics.GameEngine.Libraries.WebRtc;
using MatchTcpClients;
using MatchTcpLibrary.TransportLayer.Interfaces;
using UnityEngine;
using WebRtcWrapper;

namespace MatchTcpLibrary.TransportLayer.WebRtc
{
    internal sealed class WebRtcNetworkClient : INetworkClient
    {
        public event Action? Connected;
        public event Action<string>? ChannelOpened;
        public event Action<string, byte[]>? DataReceived;
        public event Action? Disconnected;

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected == value)
                    return;
                _isConnected = value;
                if (!value)
                    Disconnected?.Invoke();
            }
        }

        public IReadOnlyDictionary<string, IDataChannel> Channels => _channels;
        private readonly Dictionary<string, IDataChannel> _channels = new();

        private readonly IGameServerWebSignalingClient _signalingClient;
        private readonly WebRtcConfig _webRtcConfig;
        private readonly GameServerClientConfig _config;
        private readonly IReadOnlyList<(string Label, bool Reliable)> _channelSpecs;
        private readonly List<string> _candidates = new();
        private readonly ApplicationState _logger;

        private IWebRtcClient? _client;

        public WebRtcNetworkClient(
            IGameServerWebSignalingClient signalingClient,
            WebRtcConfig webRtcConfig,
            GameServerClientConfig config,
            IReadOnlyList<(string Label, bool Reliable)> channelSpecs)
        {
            _signalingClient = signalingClient;
            _webRtcConfig = webRtcConfig;
            _config = config;
            _channelSpecs = channelSpecs;
            _logger = ElympicsLogger.CurrentContext.WithContext(nameof(WebRtcNetworkClient));
        }

        public void CreateAndBind()
        {
            _client?.Dispose();
            _channels.Clear();

            _client = WebRtcFactory.CreateClient(_webRtcConfig);
            _client.IceCandidateCreated += OnIceCandidateCreated;

            foreach (var spec in _channelSpecs)
            {
                var channel = _client.CreateDataChannel(spec.Label, spec.Reliable);
                channel.DataReceived += data => DataReceived?.Invoke(spec.Label, data);
                channel.Error += error => _logger.WithMethodName().LogError($"Channel '{spec.Label}' error: {error}");
                _channels[spec.Label] = channel;
            }
        }

        public async UniTask Connect(CancellationToken ct = default)
        {
            var logger = _logger.WithMethodName();
            if (_client is null)
                throw new InvalidOperationException($"{nameof(CreateAndBind)} has not been called before connecting");

            for (var attempt = 0; attempt < _config.SessionConnectRetries; attempt++)
                try
                {
                    if (attempt > 0)
                        CreateAndBind();

                    var iceServers = await _signalingClient.FetchIceServersAsync(ElympicsTimeout.IceServersTimeout, ct);
                    if (iceServers.Length > 0)
                        _client!.SetIceServers(JsonUtility.ToJson(new IceServersResponse { iceServers = iceServers }));

                    _candidates.Clear();
                    string offer;
                    try
                    {
                        offer = await _client!.CreateOffer(false).WithTimeout(_config.OfferTimeout, ct);
                    }
                    catch (TimeoutException)
                    {
                        logger.LogInfo($"Creating WebRTC offer timed out on attempt #{attempt + 1}, retrying...");
                        continue;
                    }

                    if (string.IsNullOrEmpty(offer))
                        throw ElympicsLogger.LogExceptionAndReturn(new ElympicsException("Created WebRTC offer is null or empty."));

                    var response = await WaitForWebResponseAsync(offer, ct);
                    logger.LogInfo($"Answer:{Environment.NewLine}{response.answer}");
                    await _client!.OnAnswer(response.answer);

                    _client!.ConnectionStateChanged += OnConnectionStateChanged;
                    foreach (var channel in _channels.Values)
                        channel.Disconnected += RaiseDisconnected;

                    IsConnected = true;
                    Connected?.Invoke();
                    return;
                }
                catch (GameServerClosedException)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    logger.LogError($"Attempt #{attempt + 1} to establish a WebRTC connection failed: {e}");
                }

            throw ElympicsLogger.LogExceptionAndReturn(new ElympicsException($"Could not establish a WebRTC connection after {_config.SessionConnectRetries} attempts"));
        }

        private async UniTask<SignalingResponse> WaitForWebResponseAsync(string offer, CancellationToken ct)
        {
            var logger = _logger.WithMethodName();

            for (var i = 0; i < _config.OfferMaxRetries; i++)
            {
                if (ct.IsCancellationRequested)
                    break;

                logger.LogInfo($"Posting created WebRTC offer.\nAttempt #{i + 1}");

                var offerWithCandidates = new OfferWithCandidates
                {
                    offer = offer,
                    candidates = _candidates.ToArray(),
                };

                try
                {
                    return await _signalingClient.PostOfferAsync(offerWithCandidates, _config.OfferTimeout, ct);
                }
                catch (GameServerClosedException)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (TimeoutException)
                {
                    logger.LogWarning("WebRTC answer timed out");
                }
                catch (Exception e)
                {
                    logger.LogWarning($"WebRTC answer error: {e}");
                }
                _ = await UniTask.Delay(_config.OfferRetryDelay, DelayType.Realtime, cancellationToken: ct).SuppressCancellationThrow();
            }
            throw new TimeoutException($"Waiting for WebRTC response timed out after {_config.OfferMaxRetries} attempts");
        }

        private void OnIceCandidateCreated(string? candidate)
        {
            if (!string.IsNullOrEmpty(candidate))
                _candidates.Add(candidate);
        }

        private void OnConnectionStateChanged(string newState)
        {
            if (newState is RtcPeerConnectionStates.Failed or RtcPeerConnectionStates.Closed or RtcPeerConnectionStates.Disconnected)
                IsConnected = false;
        }

        private void RaiseDisconnected() => IsConnected = false;

        public void Disconnect()
        {
            foreach (var channel in _channels.Values)
                channel.Disconnect();
            _client?.Close();
            IsConnected = false;
        }

        public void Dispose()
        {
            Disconnect();
            _client?.Dispose();
        }
    }
}
