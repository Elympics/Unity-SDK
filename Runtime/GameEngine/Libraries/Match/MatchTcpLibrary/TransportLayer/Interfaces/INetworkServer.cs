using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTcpLibrary.TransportLayer.Interfaces
{
    public interface INetworkServer<out TClient>
        where TClient : INetworkClient
    {
        event Action<TClient> OnAccepted;

        Task ListenAsync(IPEndPoint endPoint = null, CancellationToken ct = default);
    }
}
