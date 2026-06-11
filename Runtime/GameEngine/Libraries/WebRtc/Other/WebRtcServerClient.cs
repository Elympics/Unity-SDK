using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using Elympics.GameEngine.Libraries.WebRtc.Other;
using Unity.WebRTC;
using UnityEngine;
using WebRtcWrapper;

#nullable enable

namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal class WebRtcServerClient : IWebRtcServerClient
    {
        private const string ReliableChannelLabel = "reliable";
        private const string UnreliableChannelLabel = "unreliable";

        private readonly LoggerConfig _logger = ElympicsLogger.WithElympicsGameService()
            .WithClass(typeof(WebRtcServerClient));

        private readonly RTCPeerConnection _peerConnection;
        private RTCDataChannel? _reliableDc;
        private RTCDataChannel? _unreliableDc;

        public event Action<byte[]>? ReliableReceived;
        public event Action<string>? ReliableReceivingError;
        public event Action? ReliableReceivingEnded;

        public event Action<byte[]>? UnreliableReceived;
        public event Action<string>? UnreliableReceivingError;
        public event Action? UnreliableReceivingEnded;

        public event Action<string>? IceConnectionStateChanged;
        public event Action<string>? ConnectionStateChanged;

        public WebRtcServerClient()
        {
            _peerConnection = new RTCPeerConnection();

            _peerConnection.OnDataChannel += OnDataChannel;
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

            _peerConnection.OnIceConnectionChange += OnIceConnectionStateChanged;
            _peerConnection.OnConnectionStateChange += OnConnectionStateChanged;
        }

        private void OnDataChannel(RTCDataChannel channel)
        {
            var logger = _logger.WithMethodName();
            logger.LogInfo($"[WebRTC] Data channel created: {channel.Label}");
            if (channel.Label == ReliableChannelLabel)
            {
                _reliableDc = channel;
                _reliableDc.OnOpen += OnReliableOpen;
                _reliableDc.OnMessage += OnReliableReceived;
                _reliableDc.OnClose += OnReliableEnded;
                _reliableDc.OnError += OnReliableError;
            }
            else if (channel.Label == UnreliableChannelLabel)
            {
                _unreliableDc = channel;
                _unreliableDc.OnOpen += OnUnreliableOpen;
                _unreliableDc.OnMessage += OnUnreliableReceived;
                _unreliableDc.OnClose += OnUnreliableEnded;
                _unreliableDc.OnError += OnUnreliableError;
            }
            else
                logger.LogWarning($"[WebRTC] Unknown data channel: {channel.Label}");
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
                logger.LogException(e);
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
                logger.LogException(e);
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
                logger.LogException(e);
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
                logger.LogException(e);
            }
        }

        private void OnChannel(string name, string eventType)
        {
            var logger = _logger.WithMethodName();
            // TODO: log chosen candidates ~dsygocki 2026-04-10
            logger.LogInfo($"[WebRTC] Channel '{name}' has {eventType}");
        }

        public void SendReliable(byte[] data)
        {
            if (_reliableDc?.ReadyState is not RTCDataChannelState.Open)
                return;
            _reliableDc.Send(data);
        }

        public void SendUnreliable(byte[] data)
        {
            if (_unreliableDc?.ReadyState is not RTCDataChannelState.Open)
                return;
            _unreliableDc.Send(data);
        }

        private async UniTask<string> CreateAnswer(string offerJson)
        {
            var logger = _logger.WithMethodName();
            var offerCustom = JsonUtility.FromJson<SessionDescription>(offerJson);
            var offer = (RTCSessionDescription)offerCustom;
            await _peerConnection.SetRemoteDescription(ref offer);
            var answerOp = _peerConnection.CreateAnswer();
            await answerOp;
            var answer = answerOp.Desc;
            var answerCustom = (SessionDescription)answer;
            var answerJson = JsonUtility.ToJson(answerCustom);
            logger.LogInfo("[WebRTC] Created answer\n" + answerJson);
            await _peerConnection.SetLocalDescription(ref answer);
            return answerJson;
            // TODO: log chosen candidates ~dsygocki 2026-04-10
        }

        public Task<string> CreateAnswerAsync(string offerJson)
        {
            // TODO: handle async ~dsygocki 2026-04-10
            return CreateAnswer(offerJson).AsTask();
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

        #region Unused

        public void ReceiveReliable()
        { }

        public void ReceiveUnreliable()
        { }

        public bool ReceiveReliableOnce() => true;
        public bool ReceiveUnreliableOnce() => true;

        #endregion

        public void Close()
        {
            _reliableDc?.Close();
            _unreliableDc?.Close();
            _peerConnection.Close();
        }

        public void Dispose()
        {
            Close();
            _reliableDc?.Dispose();
            _reliableDc = null;
            _unreliableDc?.Dispose();
            _unreliableDc = null;
            _peerConnection.Dispose();
        }
    }
}
