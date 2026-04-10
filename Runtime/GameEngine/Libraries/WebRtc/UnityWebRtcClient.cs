using System;
using Cysharp.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;
using WebRtcWrapper;

#nullable enable

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal class UnityWebRtcClient : IWebRtcClient
    {
        private readonly WebRtcConfig _config;

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

        public UnityWebRtcClient(WebRtcConfig config)
        {
            _config = config;
            var configuration = new RTCConfiguration
            {
                iceServers = Array.Empty<RTCIceServer>(),
            };
            _peerConnection = new RTCPeerConnection(ref configuration);

            _reliableDc = _peerConnection.CreateDataChannel("reliable");
            _reliableDc.OnOpen += OnReliableOpen;
            _reliableDc.OnMessage += OnReliableReceived;
            _reliableDc.OnClose += OnReliableEnded;
            _reliableDc.OnError += OnReliableError;

            _unreliableDc = _peerConnection.CreateDataChannel("unreliable",
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

        private void OnReliableOpen() => OnChannel("reliable", "opened");
        private void OnUnreliableOpen() => OnChannel("unreliable", "opened");

        private void OnReliableReceived(byte[] bytes) => ReliableReceived?.Invoke(bytes);
        private void OnUnreliableReceived(byte[] bytes) => UnreliableReceived?.Invoke(bytes);

        public void OnReliableError(RTCError error)
        {
            try
            {
                ReliableReceivingError?.Invoke(error.ToString());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void OnUnreliableError(RTCError error)
        {
            try
            {
                UnreliableReceivingError?.Invoke(error.ToString());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnReliableEnded()
        {
            OnChannel("reliable", "closed");
            try
            {
                ReliableReceivingEnded?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnUnreliableEnded()
        {
            OnChannel("unreliable", "closed");
            try
            {
                UnreliableReceivingEnded?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void OnChannel(string name, string eventType)
        {
            // TODO: log chosen candidates ~dsygocki 2026-04-10
            Debug.Log($"[WebRTC] Channel '{name}' has {eventType}");
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
            var options = new RTCOfferAnswerOptions { iceRestart = restart };
            var offerOp = _peerConnection.CreateOffer(ref options);
            await offerOp;
            var offer = offerOp.Desc;
            Debug.Log("[WebRTC] Created offer\n" + JsonUtility.ToJson(offer));
            await _peerConnection.SetLocalDescription(ref offer);
            Debug.Log("[WebRTC] Gathering ICE candidates...");

            _offerResolver = new UniTaskCompletionSource();
            var receivedCandidate = await UniTask.Delay(_config.OfferAnnounceDelay,
                DelayType.Realtime,
                cancellationToken: _offerResolver.Task.ToCancellationToken()).SuppressCancellationThrow();
            Debug.Log(receivedCandidate
                ? "[WebRTC] ICE candidates gathering ended successfully."
                : "[WebRTC] ICE candidates gathering timed out.");
            _offerResolver = null;

            var updatedOffer = _peerConnection.LocalDescription;
            // TODO: log chosen candidates ~dsygocki 2026-04-10

            var offerJson = JsonUtility.ToJson(updatedOffer);
            Debug.Log("[WebRTC] Offer created\n" + offerJson);
            OfferCreated?.Invoke(offerJson);
        }

        public async void CreateOffer(bool restart)
        {
            // TODO: handle async ~dsygocki 2026-04-10
            try
            {
                await CreateOfferAsync(restart);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void OnAnswer(string answerJson)
        {
            Debug.Log("[WebRTC] Answer received\n" + answerJson);
            var answer = JsonUtility.FromJson<RTCSessionDescription>(answerJson);
            _ = _peerConnection.SetRemoteDescription(ref answer); // TODO: handle async ~dsygocki 2026-04-10
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
            var stringifiedState = newState.ToString().ToLower();
            Debug.Log("[WebRTC] ICE connection state changed\n" + stringifiedState);
            try
            {
                IceConnectionStateChanged?.Invoke(stringifiedState);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnConnectionStateChanged(RTCPeerConnectionState newState)
        {
            var stringifiedState = newState.ToString().ToLower();
            Debug.Log("[WebRTC] Connection state changed\n" + stringifiedState);
            try
            {
                ConnectionStateChanged?.Invoke(stringifiedState);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
            var candidateJson = candidate.Candidate;
            Debug.Log("[WebRTC] Candidate received\n" + candidateJson);
            try
            {
                IceCandidateCreated?.Invoke(candidateJson);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
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
