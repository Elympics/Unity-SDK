using System;

namespace Proto.ProtoClient.NetworkClient
{
	public class ProtoNetworkDatagramClient : IProtoNetworkClient
	{
		public event Action<byte[]> Received;
		public event Action<string> ReceivingError;
		public event Action         ReceivingEnded;

		private readonly IDatagramCommunication _datagramCommunication;

		public ProtoNetworkDatagramClient(IDatagramCommunication datagramCommunication)
		{
			_datagramCommunication = datagramCommunication;
		}

		public void Send(byte[] data)
		{
			_datagramCommunication.Send(data);
		}

		public void Receive()
		{
			_datagramCommunication.Received += OnReceived;
			_datagramCommunication.ReceivingError += OnReceivingError;
			_datagramCommunication.ReceivingEnded += OnReceivingEnded;
		}

		private void OnReceived(byte[] data)        => Received?.Invoke(data);
		private void OnReceivingError(string error) => ReceivingError?.Invoke(error);
		private void OnReceivingEnded()             => ReceivingEnded?.Invoke();
	}
}
