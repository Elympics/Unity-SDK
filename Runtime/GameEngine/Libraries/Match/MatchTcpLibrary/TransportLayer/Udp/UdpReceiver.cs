using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MatchTcpLibrary.TransportLayer.Udp
{
	public class UdpReceiver
	{
		private readonly UdpClient _udpClient;
		private readonly Thread    _readThread;

		public event Action                               OnReceivingStopped;
		public event Action<byte[], IPEndPoint, DateTime> DataReceived;

		public UdpReceiver(UdpClient udpClient)
		{
			_udpClient = udpClient;
			_readThread = new Thread(Receive);
		}

		public void StartReceiving()
		{
			_readThread.Start();
		}

		private void Receive()
		{
			while (true)
			{
				try
				{
					IPEndPoint remoteEndPoint = null;
					var buffer = _udpClient.Receive(ref remoteEndPoint);
					var receiveTime = DateTime.UtcNow;
					DataReceived?.Invoke(buffer, remoteEndPoint, receiveTime);
				}
				catch (Exception)
				{
					break;
				}
			}

			OnReceivingStopped?.Invoke();
		}
	}
}
