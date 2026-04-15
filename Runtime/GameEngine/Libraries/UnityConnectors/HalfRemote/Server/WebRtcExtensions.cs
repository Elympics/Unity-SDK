using System;
using Proto.ProtoClient.NetworkClient;
using WebRtcWrapper;

namespace UnityConnectors.HalfRemote.Server
{
	public static class WebRtcExtensions
	{
		public static IDatagramCommunication AsReliableDatagramCommunication(this IWebRtcCommunication webRtcCommunication)
		{
			return new WebRtcReliableCommunicationAdapter(webRtcCommunication);
		}

		public static IDatagramCommunication AsUnreliableDatagramCommunication(this IWebRtcCommunication webRtcCommunication)
		{
			return new WebRtcUnreliableCommunicationAdapter(webRtcCommunication);
		}
	}

	public class WebRtcReliableCommunicationAdapter : IDatagramCommunication
	{
		private readonly IWebRtcCommunication _webRtcCommunication;

		public WebRtcReliableCommunicationAdapter(IWebRtcCommunication webRtcCommunication)
		{
			_webRtcCommunication = webRtcCommunication;
		}

		public event Action<byte[]> Received
		{
			add => _webRtcCommunication.ReliableReceived += value;
			remove => _webRtcCommunication.ReliableReceived -= value;
		}

		public event Action<string> ReceivingError
		{
			add => _webRtcCommunication.ReliableReceivingError += value;
			remove => _webRtcCommunication.ReliableReceivingError -= value;
		}

		public event Action ReceivingEnded
		{
			add => _webRtcCommunication.ReliableReceivingEnded += value;
			remove => _webRtcCommunication.ReliableReceivingEnded -= value;
		}

		public void Send(byte[] data)
		{
			_webRtcCommunication.SendReliable(data);
		}
	}

	public class WebRtcUnreliableCommunicationAdapter : IDatagramCommunication
	{
		private readonly IWebRtcCommunication _webRtcCommunication;

		public WebRtcUnreliableCommunicationAdapter(IWebRtcCommunication webRtcCommunication)
		{
			_webRtcCommunication = webRtcCommunication;
		}

		public event Action<byte[]> Received
		{
			add => _webRtcCommunication.UnreliableReceived += value;
			remove => _webRtcCommunication.UnreliableReceived -= value;
		}

		public event Action<string> ReceivingError
		{
			add => _webRtcCommunication.UnreliableReceivingError += value;
			remove => _webRtcCommunication.UnreliableReceivingError -= value;
		}

		public event Action ReceivingEnded
		{
			add => _webRtcCommunication.UnreliableReceivingEnded += value;
			remove => _webRtcCommunication.UnreliableReceivingEnded -= value;
		}

		public void Send(byte[] data)
		{
			_webRtcCommunication.SendUnreliable(data);
		}
	}
}
