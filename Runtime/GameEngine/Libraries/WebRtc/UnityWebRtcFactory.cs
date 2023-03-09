// Should stay commented - uncomment for static analysis, and comment Compile Remove line from csproj
// #undef UNITY_EDITOR
// #define UNITY_WEBGL

#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
#endif
using UnityEngine;
using WebRtcWrapper;

namespace Elympics.Libraries
{
	public static class WebRtcFactory
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		private class WebRtcClientAdapter : IWebRtcClient
		{
			private readonly int _instanceId;

			public WebRtcClientAdapter(int instanceId)
			{
				_instanceId = instanceId;
			}

			public void SendReliable(byte[] data)
			{
				WebRtcSendReliable(_instanceId, data, data.Length);
			}

			public void SendUnreliable(byte[] data)
			{
				WebRtcSendUnreliable(_instanceId, data, data.Length);
			}

			public event Action<byte[]> ReliableReceived;
			public event Action<string> ReliableReceivingError;
			public event Action         ReliableReceivingEnded;

			public event Action<byte[]> UnreliableReceived;
			public event Action<string> UnreliableReceivingError;
			public event Action         UnreliableReceivingEnded;

			public event Action<string> OfferCreated;

			public void Dispose()
			{
				HandleInstanceDestroy(_instanceId);
			}

			public void CreateOffer()
			{
				WebRtcCreateOffer(_instanceId);
			}

			public void OnAnswer(string answerJson)
			{
				WebRtcOnAnswer(_instanceId, answerJson);
			}

			public void ReceiveWithThread()
			{
			}

			public bool ReceiveReliableOnce()   => true;
			public bool ReceiveUnreliableOnce() => true;

			public void Close()
			{
				WebRtcClose(_instanceId);
			}

			public void OnReliableReceived(byte[] data) => ReliableReceived?.Invoke(data);
			public void OnReliableError(string error)   => ReliableReceivingError?.Invoke(error);
			public void OnReliableEnded()               => ReliableReceivingEnded?.Invoke();

			public void OnUnreliableReceived(byte[] data) => UnreliableReceived?.Invoke(data);
			public void OnUnreliableError(string error)   => UnreliableReceivingError?.Invoke(error);
			public void OnUnreliableEnded()               => UnreliableReceivingEnded?.Invoke();

			public void OnOffer(string offerJson) => OfferCreated?.Invoke(offerJson);
		}

		private static readonly Dictionary<int, WebRtcClientAdapter> Instances = new Dictionary<int, WebRtcClientAdapter>();

		public delegate void OnReceivedCallback(int instanceId, IntPtr msgPtr, int msgSize);

		public delegate void OnReceivingErrorCallback(int instanceId, IntPtr errorPtr);

		public delegate void OnReceivingEndedCallback(int instanceId);

		public delegate void OnOfferCallback(int instanceId, IntPtr offer);

		[DllImport("__Internal")]
		public static extern int WebRtcAllocate();

		[DllImport("__Internal")]
		public static extern void WebRtcFree(int instanceId);

		[DllImport("__Internal")]
		public static extern void WebRtcSetOnReliableReceived(OnReceivedCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcSetOnReliableError(OnReceivingErrorCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcSetOnReliableEnded(OnReceivingEndedCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcSetOnUnreliableReceived(OnReceivedCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcSetOnUnreliableError(OnReceivingErrorCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcSetOnUnreliableEnded(OnReceivingEndedCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcSetOnOffer(OnOfferCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcCreateOffer(int instanceId);

		[DllImport("__Internal")]
		public static extern void WebRtcOnAnswer(int instanceId, string answer);

		[DllImport("__Internal")]
		public static extern int WebRtcSendReliable(int instanceId, byte[] dataPtr, int dataLength);

		[DllImport("__Internal")]
		public static extern int WebRtcSendUnreliable(int instanceId, byte[] dataPtr, int dataLength);

		[DllImport("__Internal")]
		public static extern int WebRtcClose(int instanceId);

		private static bool _isInitialized;

		private static void Initialize()
		{
			WebRtcSetOnReliableReceived(DelegateOnReliableReceived);
			WebRtcSetOnReliableError(DelegateOnReliableError);
			WebRtcSetOnReliableEnded(DelegateOnReliableEnded);
			WebRtcSetOnUnreliableReceived(DelegateOnUnreliableReceived);
			WebRtcSetOnUnreliableError(DelegateOnUnreliableError);
			WebRtcSetOnUnreliableEnded(DelegateOnUnreliableEnded);
			WebRtcSetOnOffer(DelegateOnOffer);

			_isInitialized = true;
		}

		public static void HandleInstanceDestroy(int instanceId)
		{
			Instances.Remove(instanceId);
			WebRtcFree(instanceId);
		}

		[MonoPInvokeCallback(typeof(OnReceivedCallback))]
		public static void DelegateOnReliableReceived(int instanceId, IntPtr msgPtr, int msgSize)
		{
			if (!Instances.TryGetValue(instanceId, out var instanceRef))
				return;

			var msg = new byte[msgSize];
			Marshal.Copy(msgPtr, msg, 0, msgSize);

			instanceRef.OnReliableReceived(msg);
		}

		[MonoPInvokeCallback(typeof(OnReceivingErrorCallback))]
		public static void DelegateOnReliableError(int instanceId, IntPtr errorPtr)
		{
			if (!Instances.TryGetValue(instanceId, out var instanceRef))
				return;

			var errorMsg = Marshal.PtrToStringAuto(errorPtr);
			instanceRef.OnReliableError(errorMsg);
		}

		[MonoPInvokeCallback(typeof(OnReceivingEndedCallback))]
		public static void DelegateOnReliableEnded(int instanceId)
		{
			if (!Instances.TryGetValue(instanceId, out var instanceRef))
				return;

			instanceRef.OnReliableEnded();
		}

		[MonoPInvokeCallback(typeof(OnReceivedCallback))]
		public static void DelegateOnUnreliableReceived(int instanceId, IntPtr msgPtr, int msgSize)
		{
			if (!Instances.TryGetValue(instanceId, out var instanceRef))
				return;

			var msg = new byte[msgSize];
			Marshal.Copy(msgPtr, msg, 0, msgSize);

			instanceRef.OnUnreliableReceived(msg);
		}

		[MonoPInvokeCallback(typeof(OnReceivingErrorCallback))]
		public static void DelegateOnUnreliableError(int instanceId, IntPtr errorPtr)
		{
			if (!Instances.TryGetValue(instanceId, out var instanceRef))
				return;

			var errorMsg = Marshal.PtrToStringAuto(errorPtr);
			instanceRef.OnUnreliableError(errorMsg);
		}

		[MonoPInvokeCallback(typeof(OnReceivingEndedCallback))]
		public static void DelegateOnUnreliableEnded(int instanceId)
		{
			if (!Instances.TryGetValue(instanceId, out var instanceRef))
				return;

			instanceRef.OnUnreliableEnded();
		}

		[MonoPInvokeCallback(typeof(OnOfferCallback))]
		public static void DelegateOnOffer(int instanceId, IntPtr offerPtr)
		{
			if (!Instances.TryGetValue(instanceId, out var instanceRef))
				return;

			var offerJson = Marshal.PtrToStringAuto(offerPtr);
			instanceRef.OnOffer(offerJson);
		}
#endif

		public static IWebRtcClient CreateInstance()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			if (!_isInitialized)
				Initialize();

			var instanceId = WebRtcAllocate();
			var wrapper = new WebRtcClientAdapter(instanceId);
			Instances.Add(instanceId, wrapper);

			return wrapper;
#else
			return new WebRtcClient();
#endif
		}
	}
}
