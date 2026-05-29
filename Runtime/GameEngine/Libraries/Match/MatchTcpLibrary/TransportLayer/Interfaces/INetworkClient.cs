using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;

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
        UniTask ConnectAsync(IPEndPoint remoteEndPoint, CancellationToken ct = default);
        UniTask SendAsync(byte[] payload);
        void Disconnect();
    }
}
