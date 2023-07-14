using System.Net;

namespace MatchTcpLibrary.TransportLayer.Interfaces
{
    public interface IUnreliableNetworkServerClient : IUnreliableNetworkClient
    {
        void UpdateDestination(IPEndPoint destination);
        void OnDataReceived(byte[] data);
    }
}
