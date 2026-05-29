#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
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
        private readonly ElympicsLoggerContext _logger;

        public WebRtcClient(WebRtcConfig config)
        {
            if (!isInitialized)
                Initialize((int)config.OfferAnnounceDelay.TotalMilliseconds);
            _logger = ElympicsLogger.CurrentContext.WithContext(nameof(WebRtcClient));
            _instanceId = WebRtcAllocate();
            Instances.Add(_instanceId, this);
        }

        private static bool isInitialized;

        private static void Initialize(int offerAnnounceDelayMs)
        {
            WebRtcSetOfferAnnouncingDelay(offerAnnounceDelayMs);
            WebRtcSetOnReliableOpened(DelegateOnReliableOpened);
            WebRtcSetOnReliableReceived(DelegateOnReliableReceived);
            WebRtcSetOnReliableError(DelegateOnReliableError);
            WebRtcSetOnReliableEnded(DelegateOnReliableEnded);
            WebRtcSetOnUnreliableOpened(DelegateOnUnreliableOpened);
            WebRtcSetOnUnreliableReceived(DelegateOnUnreliableReceived);
            WebRtcSetOnUnreliableError(DelegateOnUnreliableError);
            WebRtcSetOnUnreliableEnded(DelegateOnUnreliableEnded);
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

        public void SendReliable(byte[] data) => WebRtcSendReliable(_instanceId, data, data.Length);

        public void SendUnreliable(byte[] data) => WebRtcSendUnreliable(_instanceId, data, data.Length);

        public event Action? ReliableChannelOpened;
        public event Action<byte[]>? ReliableReceived;
        public event Action<string>? ReliableReceivingError;
        public event Action? ReliableReceivingEnded;

        public event Action? UnreliableChannelOpened;
        public event Action<byte[]>? UnreliableReceived;
        public event Action<string>? UnreliableReceivingError;
        public event Action? UnreliableReceivingEnded;

        public event Action<string>? IceConnectionStateChanged;
        public event Action<string>? ConnectionStateChanged;

        public event Action<string>? IceCandidateCreated;
        public event Action<(IceCandidateStats LocalCandidate, IceCandidateStats RemoteCandidate)>? CandidatePairChosen;

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

        private void OnReliableOpened() => ReliableChannelOpened?.Invoke();
        private void OnReliableReceived(byte[] data) => ReliableReceived?.Invoke(data);
        private void OnReliableError(string error) => ReliableReceivingError?.Invoke(error);
        private void OnReliableEnded() => ReliableReceivingEnded?.Invoke();

        private void OnUnreliableOpened() => UnreliableChannelOpened?.Invoke();
        private void OnUnreliableReceived(byte[] data) => UnreliableReceived?.Invoke(data);
        private void OnUnreliableError(string error) => UnreliableReceivingError?.Invoke(error);
        private void OnUnreliableEnded() => UnreliableReceivingEnded?.Invoke();

        private void OnIceConnectionStateChanged(string newState) => IceConnectionStateChanged?.Invoke(newState);

        private void OnConnectionStateChanged(string newState) => ConnectionStateChanged?.Invoke(newState);

        private void OnIceCandidate(string candidateJson) => IceCandidateCreated?.Invoke(candidateJson);

        private void OnLog(string methodName, string logMessage) => _logger.WithMethodName(methodName).Log(logMessage);
        private void OnLogWarning(string methodName, string logMessage) => _logger.WithMethodName(methodName).Warning(logMessage);
        private void OnLogError(string methodName, string logMessage) => _logger.WithMethodName(methodName).Error(logMessage);

        private void OnCandidatePairChosen(string localCandidateStatsJson, string remoteCandidateStatsJson)
        {
            var localCandidate = JsonUtility.FromJson<IceCandidateStats>(localCandidateStatsJson);
            var remoteCandidate = JsonUtility.FromJson<IceCandidateStats>(remoteCandidateStatsJson);
            if (localCandidate.candidateType == "relay" || localCandidate.HasTurnUrl())
                _logger.WebRtcContext.UsesTurn = true;
            CandidatePairChosen?.Invoke((localCandidate, remoteCandidate));
        }

        [DllImport("__Internal")] private static extern int WebRtcAllocate();

        [DllImport("__Internal")] private static extern void WebRtcFree(int instanceId);

        [DllImport("__Internal")] private static extern void WebRtcSetIceServers(int instanceId, string iceServersJson);

        [DllImport("__Internal")] private static extern void WebRtcSetOfferAnnouncingDelay(int delayMs);

        [DllImport("__Internal")] private static extern void WebRtcCreateOffer(int instanceId, bool restart);

        [DllImport("__Internal")] private static extern void WebRtcOnAnswer(int instanceId, string answer);

        [DllImport("__Internal")] private static extern int WebRtcSendReliable(int instanceId, byte[] dataPtr, int dataLength);

        [DllImport("__Internal")] private static extern int WebRtcSendUnreliable(int instanceId, byte[] dataPtr, int dataLength);

        [DllImport("__Internal")] private static extern int WebRtcClose(int instanceId);

        #region Callbacks

        public delegate void OnOpenedCallback(int instanceId);

        [DllImport("__Internal")] public static extern void WebRtcSetOnReliableOpened(OnOpenedCallback callback);

        [MonoPInvokeCallback(typeof(OnOpenedCallback))]
        public static void DelegateOnReliableOpened(int instanceId)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            instanceRef.OnReliableOpened();
        }

        [DllImport("__Internal")] public static extern void WebRtcSetOnUnreliableOpened(OnOpenedCallback callback);

        [MonoPInvokeCallback(typeof(OnOpenedCallback))]
        public static void DelegateOnUnreliableOpened(int instanceId)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            instanceRef.OnUnreliableOpened();
        }

        public delegate void OnReceivedCallback(int instanceId, IntPtr msgPtr, int msgSize);

        [DllImport("__Internal")] public static extern void WebRtcSetOnReliableReceived(OnReceivedCallback callback);

        [MonoPInvokeCallback(typeof(OnReceivedCallback))]
        public static void DelegateOnReliableReceived(int instanceId, IntPtr msgPtr, int msgSize)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var msg = new byte[msgSize];
            Marshal.Copy(msgPtr, msg, 0, msgSize);

            instanceRef.OnReliableReceived(msg);
        }

        [DllImport("__Internal")] public static extern void WebRtcSetOnUnreliableReceived(OnReceivedCallback callback);

        [MonoPInvokeCallback(typeof(OnReceivedCallback))]
        public static void DelegateOnUnreliableReceived(int instanceId, IntPtr msgPtr, int msgSize)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var msg = new byte[msgSize];
            Marshal.Copy(msgPtr, msg, 0, msgSize);

            instanceRef.OnUnreliableReceived(msg);
        }

        public delegate void OnReceivingErrorCallback(int instanceId, IntPtr errorPtr);

        [DllImport("__Internal")] public static extern void WebRtcSetOnReliableError(OnReceivingErrorCallback callback);

        [MonoPInvokeCallback(typeof(OnReceivingErrorCallback))]
        public static void DelegateOnReliableError(int instanceId, IntPtr errorPtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var errorMsg = Marshal.PtrToStringAuto(errorPtr);
            instanceRef.OnReliableError(errorMsg ?? "");
        }

        [DllImport("__Internal")] public static extern void WebRtcSetOnUnreliableError(OnReceivingErrorCallback callback);

        [MonoPInvokeCallback(typeof(OnReceivingErrorCallback))]
        public static void DelegateOnUnreliableError(int instanceId, IntPtr errorPtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var errorMsg = Marshal.PtrToStringAuto(errorPtr);
            instanceRef.OnUnreliableError(errorMsg ?? "");
        }

        public delegate void OnReceivingEndedCallback(int instanceId);

        [DllImport("__Internal")] public static extern void WebRtcSetOnReliableEnded(OnReceivingEndedCallback callback);

        [MonoPInvokeCallback(typeof(OnReceivingEndedCallback))]
        public static void DelegateOnReliableEnded(int instanceId)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            instanceRef.OnReliableEnded();
        }

        [DllImport("__Internal")] public static extern void WebRtcSetOnUnreliableEnded(OnReceivingEndedCallback callback);

        [MonoPInvokeCallback(typeof(OnReceivingEndedCallback))]
        public static void DelegateOnUnreliableEnded(int instanceId)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            instanceRef.OnUnreliableEnded();
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
