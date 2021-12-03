using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HybridWebSocket;
using MatchTcpLibrary.TransportLayer.Interfaces;

namespace Elympics
{
	// TODO It's just copied and adjusted code from MatchTcpLibrary with changes to work with IWebSocket and WebSocketFactory - make it same code ~pprzestrzelski 12.11.2021
	public class WebSocketNetworkClient : IReliableNetworkClient
	{
		private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

		public bool IsConnected { get; private set; }

		public event Action<byte[]> DataReceived;
		public event Action         Disconnected;

		private IWebSocket _webSocket;

		public WebSocketNetworkClient(IWebSocket webSocket = null)
		{
			_webSocket = webSocket;
		}

		public IPEndPoint LocalEndPoint => throw new NotImplementedException();

		public IPEndPoint RemoteEndpoint => throw new NotImplementedException();

		public void CreateAndBind()
		{
		}

		public void CreateAndBind(int port) => throw new NotImplementedException();

		public void CreateAndBind(IPEndPoint localEndPoint) => throw new NotImplementedException();

		public async Task<bool> ConnectAsync(IPEndPoint remoteEndPoint)
		{
			if (_webSocket != null)
				return false;
			_webSocket = WebSocketFactory.CreateInstance($"ws://{remoteEndPoint.Address.MapToIPv4()}:{remoteEndPoint.Port}");

			var tcs = new TaskCompletionSource<bool>();
			var timeoutTask = Task.Delay(ConnectTimeout);

			void OnWebSocketOnOpen()
			{
				tcs.SetResult(true);
			}

			_webSocket.OnOpen += OnWebSocketOnOpen;
			_webSocket.Connect();

			var finishedTask = await Task.WhenAny(tcs.Task, timeoutTask);
			_webSocket.OnOpen -= OnWebSocketOnOpen;

			if (finishedTask == timeoutTask)
			{
				_webSocket = null;
				return false;
			}

			IsConnected = true;
			SetupCallbacks();
			return true;
		}

		public void ConnectAsync(IPEndPoint remoteEndPoint, Action<bool> connectedCallback, CancellationToken ct = default)
		{
			if (_webSocket != null)
			{
				connectedCallback?.Invoke(false);
				return;
			}

			_webSocket = WebSocketFactory.CreateInstance($"ws://{remoteEndPoint.Address.MapToIPv4()}:{remoteEndPoint.Port}");

			_webSocket.OnOpen += OnWebSocketOnOpen;
			ct.Register(() => _webSocket.OnOpen -= OnWebSocketOnOpen);

			void OnWebSocketOnOpen()
			{
				if (ct.IsCancellationRequested)
					return;
				SetupCallbacks();
				IsConnected = true;
				_webSocket.OnOpen -= OnWebSocketOnOpen;
				connectedCallback.Invoke(true);
			}

			if (ct.IsCancellationRequested)
				return;
			_webSocket.Connect();

			var state = _webSocket.GetState();
			if (state == WebSocketState.Closed || state == WebSocketState.Closing)
			{
				if (ct.IsCancellationRequested)
					return;
				_webSocket.OnOpen -= OnWebSocketOnOpen;
				connectedCallback.Invoke(false);
			}
		}

		private void SetupCallbacks()
		{
			_webSocket.OnMessage += OnMessage;
			_webSocket.OnClose += OnClose;
		}

		private void UnsetCallbacks()
		{
			_webSocket.OnMessage -= OnMessage;
			_webSocket.OnClose -= OnClose;
		}

		private void OnMessage(byte[] data)                => DataReceived?.Invoke(data);
		private void OnClose(WebSocketCloseCode closeCode) => Disconnected?.Invoke();

		public Task<bool> SendAsync(byte[] payload)
		{
			if (!IsConnected)
				return Task.FromResult(false);
			_webSocket.Send(payload);
			return Task.FromResult(true);
		}

		public void Disconnect()
		{
			_webSocket?.Close();
			if (_webSocket != null)
				UnsetCallbacks();
			_webSocket = null;
		}
	}

	public static class WebSocketNetworkClientFactory
	{
		public static IReliableNetworkClient CreateInstance() => new WebSocketNetworkClient();
	}
}
