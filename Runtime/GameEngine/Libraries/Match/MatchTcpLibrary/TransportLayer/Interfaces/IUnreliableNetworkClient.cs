using System;
using System.Net;
using Cysharp.Threading.Tasks;

namespace MatchTcpLibrary.TransportLayer.Interfaces
{
    public interface IUnreliableNetworkClient : INetworkClient, IDisposable
    {
        event Action<byte[], IPEndPoint> DataReceivedWithSource;
        UniTask<bool> SendToAsync(byte[] payload, IPEndPoint destination);
    }
}
