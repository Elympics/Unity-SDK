using System;
using HybridWebSocket;
using Proto.ProtoClient.NetworkClient;

namespace Elympics
{
	public class WebSocketCommunicationWrapper : IDatagramCommunication
	{
		private readonly WebSocket _webSocket;

		public WebSocketCommunicationWrapper(WebSocket webSocket)
		{
			_webSocket = webSocket;
			_webSocket.OnMessage += data => Received?.Invoke(data);
			_webSocket.OnError += error => ReceivingError?.Invoke(error);
			_webSocket.OnClose += _ => ReceivingEnded?.Invoke();
		}

		public void Send(byte[] data)
		{
			_webSocket.Send(data);
		}

		public event Action<byte[]> Received;
		public event Action<string> ReceivingError;
		public event Action         ReceivingEnded;
	}
}
