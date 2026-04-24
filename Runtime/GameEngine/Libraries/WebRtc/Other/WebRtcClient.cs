using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.GameEngine.Libraries.WebRtc.Other;
using Unity.WebRTC;
using UnityEngine;
using WebRtcWrapper;

#nullable enable

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal class WebRtcClient : IWebRtcClient
    {
        private const string ReliableChannelLabel = "reliable";
        private const string UnreliableChannelLabel = "unreliable";

        private readonly WebRtcConfig _config;
        private readonly ElympicsLoggerContext _logger;

        private readonly RTCPeerConnection _peerConnection;
        private readonly RTCDataChannel _reliableDc;
        private readonly RTCDataChannel _unreliableDc;

        private UniTaskCompletionSource? _offerResolver;

        public event Action<byte[]>? ReliableReceived;
        public event Action<string>? ReliableReceivingError;
        public event Action? ReliableReceivingEnded;

        public event Action<byte[]>? UnreliableReceived;
        public event Action<string>? UnreliableReceivingError;
        public event Action? UnreliableReceivingEnded;

        public event Action<string>? IceConnectionStateChanged;
        public event Action<string>? ConnectionStateChanged;

        public event Action<string>? OfferCreated;
        public event Action<string>? IceCandidateCreated;
        public event Action<(string LocalCandidate, string RemoteCandidate)>? CandidatePairChosen;

        private CancellationTokenSource? _candidatePairCts;

        public WebRtcClient(WebRtcConfig config)
        {
            _config = config;
            _logger = ElympicsLogger.CurrentContext.WithContext(nameof(WebRtcClient));
            var configuration = new RTCConfiguration
            {
                iceServers = Array.Empty<RTCIceServer>(),
            };
            _peerConnection = new RTCPeerConnection(ref configuration);

            _reliableDc = _peerConnection.CreateDataChannel(ReliableChannelLabel);
            _reliableDc.OnOpen += OnReliableOpen;
            _reliableDc.OnMessage += OnReliableReceived;
            _reliableDc.OnClose += OnReliableEnded;
            _reliableDc.OnError += OnReliableError;

            _unreliableDc = _peerConnection.CreateDataChannel(UnreliableChannelLabel,
                new RTCDataChannelInit
                {
                    maxRetransmits = 0,
                    ordered = false,
                });
            _unreliableDc.OnOpen += OnUnreliableOpen;
            _unreliableDc.OnMessage += OnUnreliableReceived;
            _unreliableDc.OnClose += OnUnreliableEnded;
            _unreliableDc.OnError += OnUnreliableError;

            _peerConnection.OnIceCandidate += OnIceCandidate;
            _peerConnection.OnIceConnectionChange += OnIceConnectionStateChanged;
            _peerConnection.OnConnectionStateChange += OnConnectionStateChanged;
        }

        private void OnReliableOpen() => OnChannel(ReliableChannelLabel, "opened");
        private void OnUnreliableOpen() => OnChannel(UnreliableChannelLabel, "opened");

        private void OnReliableReceived(byte[] bytes) => ReliableReceived?.Invoke(bytes);
        private void OnUnreliableReceived(byte[] bytes) => UnreliableReceived?.Invoke(bytes);

        private void OnReliableError(RTCError error)
        {
            var logger = _logger.WithMethodName();
            try
            {
                ReliableReceivingError?.Invoke(error.ToString());
            }
            catch (Exception e)
            {
                logger.Exception(e);
            }
        }

        private void OnUnreliableError(RTCError error)
        {
            var logger = _logger.WithMethodName();
            try
            {
                UnreliableReceivingError?.Invoke(error.ToString());
            }
            catch (Exception e)
            {
                logger.Exception(e);
            }
        }

        private void OnReliableEnded()
        {
            var logger = _logger.WithMethodName();
            OnChannel(ReliableChannelLabel, "closed");
            try
            {
                ReliableReceivingEnded?.Invoke();
            }
            catch (Exception e)
            {
                logger.Exception(e);
            }
        }

        private void OnUnreliableEnded()
        {
            var logger = _logger.WithMethodName();
            OnChannel(UnreliableChannelLabel, "closed");
            try
            {
                UnreliableReceivingEnded?.Invoke();
            }
            catch (Exception e)
            {
                logger.Exception(e);
            }
        }

        private void OnChannel(string name, string eventType)
        {
            var logger = _logger.WithMethodName();
            // TODO: log chosen candidates ~dsygocki 2026-04-10
            logger.Log($"[WebRTC] Channel '{name}' has {eventType}");
        }

        public void SendReliable(byte[] data)
        {
            if (_reliableDc.ReadyState is not RTCDataChannelState.Open)
                return;
            _reliableDc.Send(data);
        }

        public void SendUnreliable(byte[] data)
        {
            if (_unreliableDc.ReadyState is not RTCDataChannelState.Open)
                return;
            _unreliableDc.Send(data);
        }

        public void Close()
        {
            _reliableDc.Close();
            _unreliableDc.Close();
            _peerConnection.Close();
        }

        public void Dispose()
        {
            Close();
            _reliableDc.Dispose();
            _unreliableDc.Dispose();
            _peerConnection.Dispose();
        }

        #region Unused

        public void ReceiveWithThread()
        { }

        public bool ReceiveReliableOnce() => true;
        public bool ReceiveUnreliableOnce() => true;

        #endregion

        private async UniTask CreateOfferAsync(bool restart)
        {
            var logger = _logger.WithMethodName();
            var options = new RTCOfferAnswerOptions { iceRestart = restart };
            var offerOp = _peerConnection.CreateOffer(ref options);
            await offerOp;
            var offer = offerOp.Desc;
            logger.Log("[WebRTC] Created offer\n" + JsonUtility.ToJson((SessionDescription)offer));
            await _peerConnection.SetLocalDescription(ref offer);
            logger.Log("[WebRTC] Gathering ICE candidates...");

            _offerResolver = new UniTaskCompletionSource();
            var receivedCandidate = await UniTask.Delay(_config.OfferAnnounceDelay,
                DelayType.Realtime,
                cancellationToken: _offerResolver.Task.ToCancellationToken()).SuppressCancellationThrow();
            logger.Log(receivedCandidate
                ? "[WebRTC] ICE candidates gathering ended successfully."
                : "[WebRTC] ICE candidates gathering timed out.");
            _offerResolver = null;

            var updatedOffer = _peerConnection.LocalDescription;
            // TODO: log chosen candidates ~dsygocki 2026-04-10

            var offerJson = JsonUtility.ToJson((SessionDescription)updatedOffer);
            logger.Log("[WebRTC] Offer created\n" + offerJson);
            OfferCreated?.Invoke(offerJson);
        }

        public async void CreateOffer(bool restart)
        {
            var logger = _logger.WithMethodName();
            // TODO: handle async ~dsygocki 2026-04-10
            try
            {
                await CreateOfferAsync(restart);
            }
            catch (Exception e)
            {
                logger.Exception(e);
            }
        }

        public void OnAnswer(string answerJson)
        {
            var logger = _logger.WithMethodName();
            logger.Log("[WebRTC] Answer received\n" + answerJson);
            var answerCustom = JsonUtility.FromJson<SessionDescription>(answerJson);
            var answer = (RTCSessionDescription)answerCustom;
            _ = _peerConnection.SetRemoteDescription(ref answer); // TODO: handle async ~dsygocki 2026-04-10
            _candidatePairCts?.Cancel();
            _candidatePairCts = new CancellationTokenSource();
            WaitForCandidatePair(_candidatePairCts.Token).Forget();
        }

        private async UniTask WaitForCandidatePair(CancellationToken ct = default)
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
            var localCandidate = (RTCIceCandidateStats)statsReport.Stats[candidatePairStats.localCandidateId];
            var remoteCandidate = (RTCIceCandidateStats)statsReport.Stats[candidatePairStats.remoteCandidateId];
            if (localCandidate.candidateType is "relay")
                _ = _logger.SetUsesTurn();
            var pair = (localCandidate.ToJson(), remoteCandidate.ToJson());
            Debug.Log("Chosen pair: " + pair);
            CandidatePairChosen?.Invoke(pair);
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
            logger.Log($"[WebRTC] ICE connection state changed: {stringifiedState}");
            try
            {
                IceConnectionStateChanged?.Invoke(stringifiedState);
            }
            catch (Exception e)
            {
                logger.Exception(e);
            }
        }

        private void OnConnectionStateChanged(RTCPeerConnectionState newState)
        {
            var logger = _logger.WithMethodName();
            var stringifiedState = newState.ToString().ToLower();
            logger.Log($"[WebRTC] Connection state changed: {stringifiedState}");
            try
            {
                ConnectionStateChanged?.Invoke(stringifiedState);
            }
            catch (Exception e)
            {
                logger.Exception(e);
            }
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
            var logger = _logger.WithMethodName();
            var candidateJson = JsonUtility.ToJson(candidate.SdpMLineIndex.HasValue
                ? new IceCandidateInitWithSdpMLineIndex(candidate)
                : new IceCandidateInitWithoutSdpMLineIndex(candidate));
            logger.Log("[WebRTC] Candidate received\n" + candidateJson);
            try
            {
                IceCandidateCreated?.Invoke(candidateJson);
            }
            catch (Exception e)
            {
                logger.Exception(e);
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
