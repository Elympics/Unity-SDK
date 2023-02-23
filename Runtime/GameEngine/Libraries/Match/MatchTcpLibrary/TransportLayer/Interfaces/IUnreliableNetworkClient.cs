using System;
using System.Net;
using System.Threading.Tasks;

namespace MatchTcpLibrary.TransportLayer.Interfaces
{
	public interface IUnreliableNetworkClient : INetworkClient
	{
		event Action<byte[], IPEndPoint> DataReceivedWithSource;
		Task<bool>                       SendToAsync(byte[] payload, IPEndPoint destination);
	}
}
