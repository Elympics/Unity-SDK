using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using Elympics.ElympicsSystems.Internal;
using UnityEngine;
using WebRtcWrapper;

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
            WebRtcSetOnReliableReceived(DelegateOnReliableReceived);
            WebRtcSetOnReliableError(DelegateOnReliableError);
            WebRtcSetOnReliableEnded(DelegateOnReliableEnded);
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

        public event Action<byte[]> ReliableReceived;
        public event Action<string> ReliableReceivingError;
        public event Action ReliableReceivingEnded;

        public event Action<byte[]> UnreliableReceived;
        public event Action<string> UnreliableReceivingError;
        public event Action UnreliableReceivingEnded;
        public event Action<string> IceConnectionStateChanged;
        public event Action<string> ConnectionStateChanged;

        public event Action<string> OfferCreated;
        public event Action<string> IceCandidateCreated;
        public event Action<(string LocalCandidate, string RemoteCandidate)> CandidatePairChosen;

        public void Dispose() => HandleInstanceDestroy(_instanceId);

        public void SetIceServers(string iceServersJson) => WebRtcSetIceServers(_instanceId, iceServersJson);

        public void CreateOffer(bool restart) => WebRtcCreateOffer(_instanceId, restart);

        public void OnAnswer(string answerJson) => WebRtcOnAnswer(_instanceId, answerJson);

        public void ReceiveWithThread()
        { }

        public bool ReceiveReliableOnce() => true;
        public bool ReceiveUnreliableOnce() => true;

        public void Close() => WebRtcClose(_instanceId);

        private void OnReliableReceived(byte[] data) => ReliableReceived?.Invoke(data);
        private void OnReliableError(string error) => ReliableReceivingError?.Invoke(error);
        private void OnReliableEnded() => ReliableReceivingEnded?.Invoke();

        private void OnUnreliableReceived(byte[] data) => UnreliableReceived?.Invoke(data);
        private void OnUnreliableError(string error) => UnreliableReceivingError?.Invoke(error);
        private void OnUnreliableEnded() => UnreliableReceivingEnded?.Invoke();

        private void OnIceConnectionStateChanged(string newState) => IceConnectionStateChanged?.Invoke(newState);

        private void OnConnectionStateChanged(string newState) => ConnectionStateChanged?.Invoke(newState);

        private void OnOffer(string offerJson) => OfferCreated?.Invoke(offerJson);

        private void OnIceCandidate(string candidateJson) => IceCandidateCreated?.Invoke(candidateJson);

        private void OnLog(string methodName, string logMessage) => _logger.WithMethodName(methodName).Log(logMessage);
        private void OnLogWarning(string methodName, string logMessage) => _logger.WithMethodName(methodName).Warning(logMessage);
        private void OnLogError(string methodName, string logMessage) => _logger.WithMethodName(methodName).Error(logMessage);

        private void OnCandidatePairChosen(string localCandidateStatsJson, string remoteCandidateStatsJson)
        {
            if (JsonUtility.FromJson<CandidateWithTypeOnly>(localCandidateStatsJson).candidateType == "relay")
                _logger.WebRtcContext.UsesTurn = true;
            CandidatePairChosen?.Invoke((localCandidateStatsJson, remoteCandidateStatsJson));
        }

        [Serializable]
        private struct CandidateWithTypeOnly
        {
            public string candidateType;
        }

        [DllImport("__Internal")] public static extern int WebRtcAllocate();

        [DllImport("__Internal")] public static extern void WebRtcFree(int instanceId);

        [DllImport("__Internal")] public static extern void WebRtcSetIceServers(int instanceId, string iceServersJson);

        [DllImport("__Internal")] public static extern void WebRtcSetOfferAnnouncingDelay(int delayMs);

        [DllImport("__Internal")] public static extern void WebRtcCreateOffer(int instanceId, bool restart);

        [DllImport("__Internal")] public static extern void WebRtcOnAnswer(int instanceId, string answer);

        [DllImport("__Internal")] public static extern int WebRtcSendReliable(int instanceId, byte[] dataPtr, int dataLength);

        [DllImport("__Internal")] public static extern int WebRtcSendUnreliable(int instanceId, byte[] dataPtr, int dataLength);

        [DllImport("__Internal")] public static extern int WebRtcClose(int instanceId);

        #region Callbacks

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
            instanceRef.OnReliableError(errorMsg);
        }

        [DllImport("__Internal")] public static extern void WebRtcSetOnUnreliableError(OnReceivingErrorCallback callback);

        [MonoPInvokeCallback(typeof(OnReceivingErrorCallback))]
        public static void DelegateOnUnreliableError(int instanceId, IntPtr errorPtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var errorMsg = Marshal.PtrToStringAuto(errorPtr);
            instanceRef.OnUnreliableError(errorMsg);
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
        public static void DelegateOnIceConnectionStateChanged(int instanceId, IntPtr newState)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var errorMsg = Marshal.PtrToStringAuto(newState);
            instanceRef.OnIceConnectionStateChanged(errorMsg);
        }

        public delegate void OnConnectionStateChangedCallback(int instanceId, IntPtr newState);

        [DllImport("__Internal")] public static extern void WebRtcSetOnConnectionStateChanged(OnConnectionStateChangedCallback callback);

        [MonoPInvokeCallback(typeof(OnConnectionStateChangedCallback))]
        public static void DelegateOnConnectionStateChanged(int instanceId, IntPtr newState)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var errorMsg = Marshal.PtrToStringAuto(newState);
            instanceRef.OnConnectionStateChanged(errorMsg);
        }

        public delegate void OnOfferCallback(int instanceId, IntPtr offer);

        [DllImport("__Internal")] public static extern void WebRtcSetOnOffer(OnOfferCallback callback);

        [MonoPInvokeCallback(typeof(OnOfferCallback))]
        public static void DelegateOnOffer(int instanceId, IntPtr offerPtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var offerJson = Marshal.PtrToStringAuto(offerPtr);
            instanceRef.OnOffer(offerJson);
        }

        public delegate void OnIceCandidateCallback(int instanceId, IntPtr iceCandidate);

        [DllImport("__Internal")] public static extern void WebRtcSetOnIceCandidate(OnIceCandidateCallback callback);

        [MonoPInvokeCallback(typeof(OnIceCandidateCallback))]
        public static void DelegateOnIceCandidate(int instanceId, IntPtr candidatePtr)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var candidateJson = Marshal.PtrToStringAuto(candidatePtr);
            instanceRef.OnIceCandidate(candidateJson);
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
            instanceRef.OnCandidatePairChosen(localCandidateJson, remoteCandidateJson);
        }

        public delegate void OnLogCallback(int instanceId, IntPtr methodName, IntPtr logMessage);

        [DllImport("__Internal")] public static extern void WebRtcSetOnLog(OnLogCallback callback);

        [MonoPInvokeCallback(typeof(OnLogCallback))]
        public static void DelegateOnLog(int instanceId, IntPtr methodName, IntPtr logMessage)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var methodNameString = Marshal.PtrToStringAuto(methodName);
            var logMessageString = Marshal.PtrToStringAuto(logMessage);
            instanceRef.OnLog(methodNameString, logMessageString);
        }

        [DllImport("__Internal")] public static extern void WebRtcSetOnLogWarning(OnLogCallback callback);

        [MonoPInvokeCallback(typeof(OnLogCallback))]
        public static void DelegateOnLogWarning(int instanceId, IntPtr methodName, IntPtr logMessage)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var methodNameString = Marshal.PtrToStringAuto(methodName);
            var logMessageString = Marshal.PtrToStringAuto(logMessage);
            instanceRef.OnLogWarning(methodNameString, logMessageString);
        }

        [DllImport("__Internal")] public static extern void WebRtcSetOnLogError(OnLogCallback callback);

        [MonoPInvokeCallback(typeof(OnLogCallback))]
        public static void DelegateOnLogError(int instanceId, IntPtr methodName, IntPtr logMessage)
        {
            if (!Instances.TryGetValue(instanceId, out var instanceRef))
                return;

            var methodNameString = Marshal.PtrToStringAuto(methodName);
            var logMessageString = Marshal.PtrToStringAuto(logMessage);
            instanceRef.OnLogError(methodNameString, logMessageString);
        }

        #endregion
    }
}
