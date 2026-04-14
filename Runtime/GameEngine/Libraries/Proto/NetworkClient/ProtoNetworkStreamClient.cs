using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Proto.ProtoClient.NetworkClient
{
	public class ProtoNetworkStreamClient : IProtoNetworkClient
	{
		public event Action<byte[]> Received;
		public event Action<string> ReceivingError;
		public event Action         ReceivingEnded;

		private readonly NetworkStream _stream;

		private readonly byte[] _sendSizeBuffer = new byte[sizeof(int)];
		private readonly byte[] _recvSizeBuffer = new byte[sizeof(int)];

		private readonly object _sendLock = new object();

		public ProtoNetworkStreamClient(NetworkStream stream)
		{
			_stream = stream;
		}

		public void Send(byte[] data)
		{
			lock (_sendLock)
			{
				Array.Copy(BitConverter.GetBytes(data.Length), _sendSizeBuffer, _sendSizeBuffer.Length);
				try
				{
					_stream.Write(_sendSizeBuffer, 0, _sendSizeBuffer.Length);
					_stream.Write(data, 0, data.Length);
					_stream.Flush();
				}
				catch (IOException)
				{
					_stream.Close();
				}
				catch (ObjectDisposedException)
				{
					// Already closed
				}
			}
		}

		public void Receive()
		{
			Task.Run(() =>
			{
				try
				{
					while (true)
					{
						var data = ReadFromClient();
						if (data == null)
							return;

						Received?.Invoke(data);
					}
				}
				catch (IOException)
				{
					_stream.Close();
				}
				catch (ObjectDisposedException)
				{
					// Already closed
				}
				catch (Exception e)
				{
					ReceivingError?.Invoke(e.ToString());
				}
				finally
				{
					ReceivingEnded?.Invoke();
				}
			});
		}

		private byte[] ReadFromClient()
		{
			ReadFromClientToFillBuffer(_recvSizeBuffer);
			var size = BitConverter.ToInt32(_recvSizeBuffer, 0);
			var message = new byte[size];

			ReadFromClientToFillBuffer(message);
			return message;
		}


		private void ReadFromClientToFillBuffer(byte[] buffer)
		{
			var offset = 0;
			var remaining = buffer.Length;
			while (remaining > 0)
			{
				var read = _stream.Read(buffer, offset, remaining);
				if (read <= 0)
					throw new EndOfStreamException($"End of stream reached with {remaining} bytes left to read");
				remaining -= read;
				offset += read;
			}
		}
	}
}
