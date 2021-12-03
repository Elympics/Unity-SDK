// #undef UNITY_EDITOR
// #define UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
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

			public void Send(byte[] data)
			{
				WebRtcSend(_instanceId, data, data.Length);
			}

			public event Action<byte[]> Received;
			public event Action<string> ReceivingError;
			public event Action         ReceivingEnded;
			public event Action<string> Offer;

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

			public void Receive()
			{
			}

			public void Close()
			{
				WebRtcClose(_instanceId);
			}

			public void OnReceived(byte[] data)        => Received?.Invoke(data);
			public void OnReceivingError(string error) => ReceivingError?.Invoke(error);
			public void OnReceivingEnded()             => ReceivingEnded?.Invoke();
			public void OnOffer(string offerJson)      => Offer?.Invoke(offerJson);
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
		public static extern void WebRtcSetOnReceived(OnReceivedCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcSetOnReceivingError(OnReceivingErrorCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcSetOnReceivingEnded(OnReceivingEndedCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcSetOnOffer(OnOfferCallback callback);

		[DllImport("__Internal")]
		public static extern void WebRtcCreateOffer(int instanceId);

		[DllImport("__Internal")]
		public static extern void WebRtcOnAnswer(int instanceId, string answer);

		[DllImport("__Internal")]
		public static extern int WebRtcSend(int instanceId, byte[] dataPtr, int dataLength);

		[DllImport("__Internal")]
		public static extern int WebRtcClose(int instanceId);

		private static bool _isInitialized;

		private static void Initialize()
		{
			WebRtcSetOnReceived(DelegateOnReceived);
			WebRtcSetOnReceivingError(DelegateOnReceivingError);
			WebRtcSetOnReceivingEnded(DelegateOnReceivingEnded);
			WebRtcSetOnOffer(DelegateOnOffer);

			_isInitialized = true;
		}

		public static void HandleInstanceDestroy(int instanceId)
		{
			Instances.Remove(instanceId);
			WebRtcFree(instanceId);
		}

		[MonoPInvokeCallback(typeof(OnReceivedCallback))]
		public static void DelegateOnReceived(int instanceId, IntPtr msgPtr, int msgSize)
		{
			if (!Instances.TryGetValue(instanceId, out var instanceRef))
				return;

			var msg = new byte[msgSize];
			Marshal.Copy(msgPtr, msg, 0, msgSize);

			instanceRef.OnReceived(msg);
		}

		[MonoPInvokeCallback(typeof(OnReceivingErrorCallback))]
		public static void DelegateOnReceivingError(int instanceId, IntPtr errorPtr)
		{
			if (!Instances.TryGetValue(instanceId, out var instanceRef))
				return;

			var errorMsg = Marshal.PtrToStringAuto(errorPtr);
			instanceRef.OnReceivingError(errorMsg);
		}

		[MonoPInvokeCallback(typeof(OnReceivingEndedCallback))]
		public static void DelegateOnReceivingEnded(int instanceId)
		{
			if (!Instances.TryGetValue(instanceId, out var instanceRef))
				return;

			instanceRef.OnReceivingEnded();
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
