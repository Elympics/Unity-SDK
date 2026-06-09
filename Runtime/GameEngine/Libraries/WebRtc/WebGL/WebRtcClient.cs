#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using Cysharp.Threading.Tasks;
using Elympics.Core.Logger;
using Elympics.ElympicsSystems.Internal;
using MatchTcpLibrary.TransportLayer.Interfaces;
using UnityEngine;
using WebRtcWrapper;

// The goal here is to have two interchangeable types with the same full name.
// Using Platforms and Define Constraints in .asmdef, they are used in alternation.
// ReSharper disable once CheckNamespace
namespace Elympics.GameEngine.Libraries.WebRtc
{
    internal class WebRtcClient : IWebRtcClient
    {
        private static readonly Dictionary<int, WebRtcClient> Instances = new();

        private readonly int _instanceId;
        private readonly LoggerConfig _logger = ElympicsLogger.WithElympicsGameService().WithClassName(nameof(WebRtcClient));
        private readonly Dictionary<int, WebGLDataChannel> _channels = new();

        public WebRtcClient(WebRtcConfig config)
        {
            if (!isInitialized)
                Initialize((int)config.OfferAnnounceDelay.TotalMilliseconds);
            _instanceId = WebRtcAllocate();
            Instances.Add(_instanceId, this);
        }

        private static bool isInitialized;

        private static void Initialize(int offerAnnounceDelayMs)
        {
            WebRtcSetOfferAnnouncingDelay(offerAnnounceDelayMs);
            WebRtcSetOnChannelOpened(DelegateOnChannelOpened);
            WebRtcSetOnChannelReceived(DelegateOnChannelReceived);
            WebRtcSetOnChannelError(DelegateOnChannelError);
            WebRtcSetOnChannelEnded(DelegateOnChannelEnded);
            WebRtcSetOnIceConnectionStateChanged(DelegateOnIceConnectionStateChanged);
            WebRtcSetOnConnectionStateChanged(DelegateOnConnectionStateChanged);
            WebRtcSetOnOffer(DelegateOnOffer);
            WebRtcSetOnIceCandidate(DelegateOnIceCandidate);
            WebRtcSetOnCandidatePairChosen(DelegateOnCandidatePairChosen);
            WebRtcSetOnLog(DelegateOnLog);
            WebRtcSetOnLogWarning(DelegateOnLogWarning);
            WebRtcSetOnLogError(DelegateOnLogError);

            isInitialized = true;
        }

        private static void HandleInstanceDestroy(int instanceId)
        {
            Instances.Remove(instanceId);
            WebRtcFree(instanceId);
        }

        public IDataChannel CreateDataChannel(string label, bool reliable)
        {
            var channelId = WebRtcCreateDataChannel(_instanceId, label, reliable);
            var channel = new WebGLDataChannel(label, _instanceId, channelId);
            _channels.Add(channelId, channel);
            return channel;
        }

        private sealed class WebGLDataChannel : IDataChannel
        {
            private readonly int _instanceId;
            private readonly int _channelId;

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

            public WebGLDataChannel(string label, int instanceId, int channelId)
            {
                Label = label;
                _instanceId = instanceId;
                _channelId = channelId;
            }

            public void OnOpened() => IsConnected = true;
            public void OnReceived(byte[] data) => DataReceived?.Invoke(data);
            public void OnError(string error) => Error?.Invoke(error);
            public void OnEnded() => IsConnected = false;

            // All channels are created before the offer/answer exchange completes.
            // There is no per-channel connect step.
            public void CreateAndBind()
            { }

            public UniTask ConnectAsync(IPEndPoint remoteEndPoint, CancellationToken ct = default) => UniTask.CompletedTask;

            public void Send(byte[] payload) => WebRtcSendOnChannel(_instanceId, _channelId, payload, payload.Length);

            public void Disconnect()
            {
                WebRtcCloseChannel(_instanceId, _channelId);
                IsConnected = false;
            }

            public void Dispose()
            { }
        }

        public void Dispose() => HandleInstanceDestroy(_instanceId);

        public void SetIceServers(string iceServersJson) => WebRtcSetIceServers(_instanceId, iceServersJson);

        private UniTaskCompletionSource<string>? _offerTcs;
        public UniTask<string> CreateOffer(bool restart)
        {
            _offerTcs = new UniTaskCompletionSource<string>();
            WebRtcCreateOffer(_instanceId, restart);
            return _offerTcs.Task;
        }

        private void OnOffer(string offerJson)
        {
            _offerTcs?.TrySetResult(offerJson);
            _offerTcs = null;
        }

        public UniTask OnAnswer(string answerJson)
        {
            WebRtcOnAnswer(_instanceId, answerJson);
            return UniTask.CompletedTask;
        }

        public void Close() => WebRtcClose(_instanceId);

        public event Action<string>? IceConnectionStateChanged;
        public event Action<string>? ConnectionStateChanged;

        public event Action<string>? IceCandidateCreated;
        public event Action<(IceCandidateStats LocalCandidate, IceCandidateStats RemoteCandidate)>? CandidatePairChosen;

        private void OnIceConnectionStateChanged(string newState) => IceConnectionStateChanged?.Invoke(newState);

        private void OnConnectionStateChanged(string newState) => ConnectionStateChanged?.Invoke(newState);

        private void OnIceCandidate(string candidateJson) => IceCandidateCreated?.Invoke(candidateJson);

        private void OnLog(string methodName, string logMessage) => _logger.WithMethodName(methodName).LogInfo(logMessage);
        private void OnLogWarning(string methodName, string logMessage) => _logger.WithMethodName(methodName).LogWarning(logMessage);
        private void OnLogError(string methodName, string logMessage) => _logger.WithMethodName(methodName).LogError(logMessage);

        private void OnCandidatePairChosen(string localCandidateStatsJson, string remoteCandidateStatsJson)
        {
            var localCandidate = JsonUtility.FromJson<IceCandidateStats>(localCandidateStatsJson);
            var remoteCandidate = JsonUtility.FromJson<IceCandidateStats>(remoteCandidateStatsJson);
            if (localCandidate.candidateType == "relay" || localCandidate.HasTurnUrl())
                _ = ElympicsLogger.ApplicationState.SetUsesTurn();
            CandidatePairChosen?.Invoke((localCandidate, remoteCandidate));
        }

        [DllImport("__Internal")] private static extern int WebRtcAllocate();

        [DllImport("__Internal")] private static extern void WebRtcFree(int instanceId);

        [DllImport("__Internal")] private static extern void WebRtcSetIceServers(int instanceId, string iceServersJson);

        [DllImport("__Internal")] private static extern void WebRtcSetOfferAnnouncingDelay(int delayMs);

        [DllImport("__Internal")] private static extern void WebRtcCreateOffer(int instanceId, bool restart);

        [DllImport("__Internal")] private static extern void WebRtcOnAnswer(int instanceId, string answer);

        [DllImport("__Internal")] private static extern int WebRtcCreateDataChannel(int instanceId, string label, bool reliable);

        [DllImport("__Internal")] private static extern int WebRtcSendOnChannel(int instanceId, int channelId, byte[] dataPtr, int dataLength);

        [DllImport("__Internal")] private static extern void WebRtcCloseChannel(int instanceId, int channelId);

        [DllImport("__Internal")] private static extern int WebRtcClose(int instanceId);

        #region Callbacks

        public delegate void OnChannelCallback(int instanceId, int channelId);

        [DllImport("__Internal")] public static extern void WebRtcSetOnChannelOpened(OnChannelCallback callback);

        [MonoPInvokeCallback(typeof(OnChannelCallback))]
        public static void DelegateOnChannelOpened(int instanceId, int channelId)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;
            if (!instanceRef._channels.TryGetValue(channelId, out var channel))
                return;

            channel.OnOpened();
        }

        [DllImport("__Internal")] public static extern void WebRtcSetOnChannelEnded(OnChannelCallback callback);

        [MonoPInvokeCallback(typeof(OnChannelCallback))]
        public static void DelegateOnChannelEnded(int instanceId, int channelId)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;
            if (!instanceRef._channels.TryGetValue(channelId, out var channel))
                return;

            channel.OnEnded();
        }

        public delegate void OnChannelReceivedCallback(int instanceId, int channelId, IntPtr msgPtr, int msgSize);

        [DllImport("__Internal")] public static extern void WebRtcSetOnChannelReceived(OnChannelReceivedCallback callback);

        [MonoPInvokeCallback(typeof(OnChannelReceivedCallback))]
        public static void DelegateOnChannelReceived(int instanceId, int channelId, IntPtr msgPtr, int msgSize)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;
            if (!instanceRef._channels.TryGetValue(channelId, out var channel))
                return;

            var msg = new byte[msgSize];
            Marshal.Copy(msgPtr, msg, 0, msgSize);

            channel.OnReceived(msg);
        }

        public delegate void OnChannelErrorCallback(int instanceId, int channelId, IntPtr errorPtr);

        [DllImport("__Internal")] public static extern void WebRtcSetOnChannelError(OnChannelErrorCallback callback);

        [MonoPInvokeCallback(typeof(OnChannelErrorCallback))]
        public static void DelegateOnChannelError(int instanceId, int channelId, IntPtr errorPtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;
            if (!instanceRef._channels.TryGetValue(channelId, out var channel))
                return;

            var errorMsg = Marshal.PtrToStringAuto(errorPtr);
            channel.OnError(errorMsg ?? "");
        }

        public delegate void OnIceConnectionStateChangedCallback(int instanceId, IntPtr newState);

        [DllImport("__Internal")] public static extern void WebRtcSetOnIceConnectionStateChanged(OnIceConnectionStateChangedCallback callback);

        [MonoPInvokeCallback(typeof(OnIceConnectionStateChangedCallback))]
        public static void DelegateOnIceConnectionStateChanged(int instanceId, IntPtr newStatePtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var newState = Marshal.PtrToStringAuto(newStatePtr);
            instanceRef.OnIceConnectionStateChanged(newState ?? "");
        }

        public delegate void OnConnectionStateChangedCallback(int instanceId, IntPtr newState);

        [DllImport("__Internal")] public static extern void WebRtcSetOnConnectionStateChanged(OnConnectionStateChangedCallback callback);

        [MonoPInvokeCallback(typeof(OnConnectionStateChangedCallback))]
        public static void DelegateOnConnectionStateChanged(int instanceId, IntPtr newStatePtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var newState = Marshal.PtrToStringAuto(newStatePtr);
            instanceRef.OnConnectionStateChanged(newState ?? "");
        }

        public delegate void OnOfferCallback(int instanceId, IntPtr offer);

        [DllImport("__Internal")] public static extern void WebRtcSetOnOffer(OnOfferCallback callback);

        [MonoPInvokeCallback(typeof(OnOfferCallback))]
        public static void DelegateOnOffer(int instanceId, IntPtr offerJsonPtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var offerJson = Marshal.PtrToStringAuto(offerJsonPtr);
            instanceRef.OnOffer(offerJson ?? "");
        }

        public delegate void OnIceCandidateCallback(int instanceId, IntPtr iceCandidate);

        [DllImport("__Internal")] public static extern void WebRtcSetOnIceCandidate(OnIceCandidateCallback callback);

        [MonoPInvokeCallback(typeof(OnIceCandidateCallback))]
        public static void DelegateOnIceCandidate(int instanceId, IntPtr candidateJsonPtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var candidateJson = Marshal.PtrToStringAuto(candidateJsonPtr);
            instanceRef.OnIceCandidate(candidateJson ?? "");
        }

        public delegate void OnCandidatePairChosenCallback(int instanceId, IntPtr localCandidateJsonPtr, IntPtr remoteCandidateJsonPtr);

        [DllImport("__Internal")] public static extern void WebRtcSetOnCandidatePairChosen(OnCandidatePairChosenCallback callback);

        [MonoPInvokeCallback(typeof(OnCandidatePairChosenCallback))]
        public static void DelegateOnCandidatePairChosen(int instanceId, IntPtr localCandidateJsonPtr, IntPtr remoteCandidateJsonPtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var localCandidateJson = Marshal.PtrToStringAuto(localCandidateJsonPtr);
            var remoteCandidateJson = Marshal.PtrToStringAuto(remoteCandidateJsonPtr);
            instanceRef.OnCandidatePairChosen(localCandidateJson ?? "", remoteCandidateJson ?? "");
        }

        public delegate void OnLogCallback(int instanceId, IntPtr methodName, IntPtr logMessage);

        [DllImport("__Internal")] public static extern void WebRtcSetOnLog(OnLogCallback callback);

        [MonoPInvokeCallback(typeof(OnLogCallback))]
        public static void DelegateOnLog(int instanceId, IntPtr methodNamePtr, IntPtr logMessagePtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var methodName = Marshal.PtrToStringAuto(methodNamePtr);
            var logMessage = Marshal.PtrToStringAuto(logMessagePtr);
            instanceRef.OnLog(methodName ?? "", logMessage ?? "");
        }

        [DllImport("__Internal")] public static extern void WebRtcSetOnLogWarning(OnLogCallback callback);

        [MonoPInvokeCallback(typeof(OnLogCallback))]
        public static void DelegateOnLogWarning(int instanceId, IntPtr methodNamePtr, IntPtr logMessagePtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var methodName = Marshal.PtrToStringAuto(methodNamePtr);
            var logMessage = Marshal.PtrToStringAuto(logMessagePtr);
            instanceRef.OnLogWarning(methodName ?? "", logMessage ?? "");
        }

        [DllImport("__Internal")] public static extern void WebRtcSetOnLogError(OnLogCallback callback);

        [MonoPInvokeCallback(typeof(OnLogCallback))]
        public static void DelegateOnLogError(int instanceId, IntPtr methodNamePtr, IntPtr logMessagePtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var methodName = Marshal.PtrToStringAuto(methodNamePtr);
            var logMessage = Marshal.PtrToStringAuto(logMessagePtr);
            instanceRef.OnLogError(methodName ?? "", logMessage ?? "");
        }

        #endregion
    }
}
