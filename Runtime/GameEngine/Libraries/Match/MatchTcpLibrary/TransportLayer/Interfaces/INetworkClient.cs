using System;
using System.Net;
using System.Threading.Tasks;

namespace MatchTcpLibrary.TransportLayer.Interfaces
{
    public interface INetworkClient
    {
        bool IsConnected { get; }
        event Action Disconnected;
        event Action<byte[]> DataReceived;

        IPEndPoint LocalEndPoint { get; }
        IPEndPoint RemoteEndpoint { get; }

        void CreateAndBind();
        void CreateAndBind(int port);
        void CreateAndBind(IPEndPoint localEndPoint);
        Task<bool> ConnectAsync(IPEndPoint remoteEndPoint);
        Task<bool> SendAsync(byte[] payload);
        void Disconnect();
    }
}
