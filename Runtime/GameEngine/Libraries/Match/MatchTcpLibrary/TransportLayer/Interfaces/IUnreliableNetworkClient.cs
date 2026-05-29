using System;
using System.Net;
using Cysharp.Threading.Tasks;

namespace MatchTcpLibrary.TransportLayer.Interfaces
{
    public interface IUnreliableNetworkClient : INetworkClient, IDisposable
    {
        event Action<byte[], IPEndPoint> DataReceivedWithSource;
        UniTask SendToAsync(byte[] payload, IPEndPoint destination);
    }
}
