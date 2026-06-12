#nullable enable

using System;
using System.Linq;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using Elympics.GameEngine.Libraries.WebRtc.Other;
using MatchTcpLibrary.TransportLayer.Interfaces;
using Unity.WebRTC;
using UnityEngine;
using WebRtcWrapper;

#nullable enable

// The goal here is to have two interchangeable types with the same full name.
// Using Platforms and Define Constraints in .asmdef, they are used in alternation.
// ReSharper disable once CheckNamespace
namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal class WebRtcClient : IWebRtcClient
    {
        private readonly WebRtcConfig _config;
        private readonly LoggerConfig _logger = ElympicsLogger.WithElympicsGameService()
            .WithClass(typeof(WebRtcClient));

        private readonly RTCPeerConnection _peerConnection;

        private UniTaskCompletionSource? _offerResolver;

        public event Action<string>? IceConnectionStateChanged;
        public event Action<string>? ConnectionStateChanged;

        public event Action<string>? IceCandidateCreated;
        public event Action<(IceCandidateStats LocalCandidate, IceCandidateStats RemoteCandidate)>? CandidatePairChosen;

        private CancellationTokenSource? _candidatePairCts;

        public WebRtcClient(WebRtcConfig config)
        {
            _config = config;
            var configuration = new RTCConfiguration
            {
                iceServers = config.IceServers.Select(s => new RTCIceServer
                {
                    credential = s.credential,
                    credentialType = RTCIceCredentialType.Password,
                    urls = s.urls,
                    username = s.username,
                }).ToArray(),
            };
            _peerConnection = new RTCPeerConnection(ref configuration)
            {
                OnNegotiationNeeded = OnNegotiationNeeded,
            };

            _peerConnection.OnIceCandidate += OnIceCandidate;
            _peerConnection.OnIceConnectionChange += OnIceConnectionStateChanged;
            _peerConnection.OnConnectionStateChange += OnConnectionStateChanged;
        }

        public IDataChannel CreateDataChannel(string label, bool reliable)
        {
            var dc = reliable
                ? _peerConnection.CreateDataChannel(label)
                : _peerConnection.CreateDataChannel(label, new RTCDataChannelInit { maxRetransmits = 0, ordered = false });
            return new WebRtcDataChannel(label, dc, _logger);
        }

        private sealed class WebRtcDataChannel : IDataChannel
        {
            private readonly RTCDataChannel _dc;
            private readonly LoggerConfig _logger;

            public string Label { get; }

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

            public event Action? Disconnected;
            public event Action<byte[]>? DataReceived;
            public event Action<string>? Error;

            public WebRtcDataChannel(string label, RTCDataChannel dc, LoggerConfig logger)
            {
                Label = label;
                _dc = dc;
                _logger = logger;
                _dc.OnOpen += OnOpen;
                _dc.OnMessage += OnMessage;
                _dc.OnClose += OnClose;
                _dc.OnError += OnError;
            }

            private void OnOpen()
            {
                _logger.WithMethodName().LogInfo($"[WebRTC] Channel '{Label}' has opened");
                IsConnected = true;
            }

            private void OnMessage(byte[] bytes) => DataReceived?.Invoke(bytes);

            private void OnError(RTCError error)
            {
                var logger = _logger.WithMethodName();
                try
                {
                    Error?.Invoke($"{error.errorType}: {error.message}");
                }
                catch (Exception e)
                {
                    logger.LogException(e);
                }
            }

            private void OnClose()
            {
                _logger.WithMethodName().LogInfo($"[WebRTC] Channel '{Label}' has closed");
                IsConnected = false;
            }

            // All channels are created before the offer/answer exchange completes.
            // There is no per-channel connect step.
            public void CreateAndBind()
            { }

            public UniTask ConnectAsync(IPEndPoint remoteEndPoint, CancellationToken ct = default) => UniTask.CompletedTask;

            public void Send(byte[] payload)
            {
                if (_dc.ReadyState is not RTCDataChannelState.Open)
                    return;
                _dc.Send(payload);
            }

            public void Disconnect()
            {
                _dc.Close();
                IsConnected = false;
            }

            public void Dispose()
            {
                _dc.OnOpen -= OnOpen;
                _dc.OnMessage -= OnMessage;
                _dc.OnClose -= OnClose;
                _dc.OnError -= OnError;
                _dc.Dispose();
            }
        }

        public void Close() => _peerConnection.Close();

        public void Dispose()
        {
            Close();
            _peerConnection.Dispose();
        }

        public async UniTask<string> CreateOffer(bool restart)
        {
            var logger = _logger.WithMethodName();
            var options = new RTCOfferAnswerOptions { iceRestart = restart };
            var offerOp = _peerConnection.CreateOffer(ref options);
            await offerOp;
            var offer = offerOp.Desc;
            logger.LogInfo("[WebRTC] Created offer\n" + JsonUtility.ToJson((SessionDescription)offer));
            await _peerConnection.SetLocalDescription(ref offer);
            logger.LogInfo("[WebRTC] Gathering ICE candidates...");

            _offerResolver = new UniTaskCompletionSource();
            var receivedCandidate = await UniTask.Delay(_config.OfferAnnounceDelay,
                DelayType.Realtime,
                cancellationToken: _offerResolver.Task.ToCancellationToken()).SuppressCancellationThrow();
            logger.LogInfo(receivedCandidate
                ? "[WebRTC] ICE candidates gathering ended successfully."
                : "[WebRTC] ICE candidates gathering timed out.");
            _offerResolver = null;

            var updatedOffer = _peerConnection.LocalDescription;

            var offerJson = JsonUtility.ToJson((SessionDescription)updatedOffer);
            logger.LogInfo("[WebRTC] Offer created\n" + offerJson);

            return offerJson;
        }

        public async UniTask OnAnswer(string answerJson)
        {
            var logger = _logger.WithMethodName();
            logger.LogInfo("[WebRTC] Answer received\n" + answerJson);
            var answerCustom = JsonUtility.FromJson<SessionDescription>(answerJson);
            var answer = (RTCSessionDescription)answerCustom;
            var asyncOp = _peerConnection.SetRemoteDescription(ref answer);
            await asyncOp;
            _candidatePairCts?.Cancel();
            _candidatePairCts = new CancellationTokenSource();
            WaitForCandidatePair(_candidatePairCts.Token).Forget();
        }

        private async UniTaskVoid WaitForCandidatePair(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                var asyncOp = _peerConnection.GetStats();
                await asyncOp;
                var report = asyncOp.Value;
                var nominatedPair = report.Stats.Values.Where(s => s.Type == RTCStatsType.CandidatePair)
                    .Cast<RTCIceCandidatePairStats>()
                    .FirstOrDefault(s => s.nominated);
                if (nominatedPair is not null)
                {
                    HandleCandidatePairChosen(report, nominatedPair);
                    return;
                }

                _ = await UniTask.Delay(200, DelayType.Realtime, cancellationToken: ct).SuppressCancellationThrow();
            }
        }

        private void HandleCandidatePairChosen(RTCStatsReport statsReport, RTCIceCandidatePairStats candidatePairStats)
        {
            var logger = _logger.WithMethodName();
            var localCandidate = Cast((RTCIceCandidateStats)statsReport.Stats[candidatePairStats.localCandidateId]);
            var remoteCandidate = Cast((RTCIceCandidateStats)statsReport.Stats[candidatePairStats.remoteCandidateId]);
            if (localCandidate.candidateType is "relay" || localCandidate.HasTurnUrl())
                ElympicsLogger.State.SetUsesTurn();
            logger.LogInfo($"[WebRTC] Chosen candidate pair: {(JsonUtility.ToJson(localCandidate), JsonUtility.ToJson(remoteCandidate))}");
            CandidatePairChosen?.Invoke((localCandidate, remoteCandidate));

            static IceCandidateStats Cast(RTCIceCandidateStats candidate) =>
                new()
                {
                    transportId = candidate.transportId,
                    address = candidate.address,
                    port = candidate.port,
                    protocol = candidate.protocol,
                    candidateType = candidate.candidateType,
                    priority = candidate.priority,
                    url = candidate.url,
                    relayProtocol = candidate.relayProtocol,
                    foundation = candidate.foundation,
                    relatedAddress = candidate.relatedAddress,
                    relatedPort = candidate.relatedPort,
                    usernameFragment = candidate.usernameFragment,
                    tcpType = candidate.tcpType
                };
        }

        public void SetIceServers(string iceServersJson)
        {
            var config = _peerConnection.GetConfiguration();
            config.iceServers = JsonUtility.FromJson<IceServersResponse>(iceServersJson).iceServers;
            var errorType = _peerConnection.SetConfiguration(ref config);
            if (errorType is not RTCErrorType.None)
                throw new InvalidOperationException($"Error updating ICE server list in RTC configuration: {errorType}");
        }

        private void OnIceConnectionStateChanged(RTCIceConnectionState newState)
        {
            var logger = _logger.WithMethodName();
            var stringifiedState = newState.ToString().ToLower();
            logger.LogInfo($"[WebRTC] ICE connection state changed: {stringifiedState}");
            try
            {
                IceConnectionStateChanged?.Invoke(stringifiedState);
            }
            catch (Exception e)
            {
                logger.LogException(e);
            }
        }

        private void OnConnectionStateChanged(RTCPeerConnectionState newState)
        {
            var logger = _logger.WithMethodName();
            var stringifiedState = newState.ToString().ToLower();
            logger.LogInfo($"[WebRTC] Connection state changed: {stringifiedState}");
            try
            {
                ConnectionStateChanged?.Invoke(stringifiedState);
            }
            catch (Exception e)
            {
                logger.LogException(e);
            }
        }

        private void OnNegotiationNeeded()
        {
            // Beware: no renegotiation is supported (the signaling protocol is a single offer/answer exchange).
            _logger.WithMethodName().LogInfo("[WebRTC] Negotiation needed");
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
            var logger = _logger.WithMethodName();
            var candidateJson = JsonUtility.ToJson(candidate.SdpMLineIndex.HasValue
                ? new IceCandidateInitWithSdpMLineIndex(candidate)
                : new IceCandidateInitWithoutSdpMLineIndex(candidate));
            logger.LogInfo("[WebRTC] Candidate received\n" + candidateJson);
            try
            {
                IceCandidateCreated?.Invoke(candidateJson);
            }
            catch (Exception e)
            {
                logger.LogException(e);
            }

            _ = _offerResolver?.TrySetResult();
        }

        [Serializable]
        internal struct IceServersResponse
        {
            public RTCIceServer[] iceServers;
        }
    }
}
