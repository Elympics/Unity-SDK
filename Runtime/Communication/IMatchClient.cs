using System;
using System.Threading.Tasks;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
    public interface IMatchClient
    {
        event Action<TimeSynchronizationData> Synchronized;
        event Action<ElympicsSnapshot> SnapshotReceived;
        event Action<ElympicsRpcMessageList> RpcMessageListReceived;

        Task SendInput(ElympicsInput input);
        Task SendRpcMessageList(ElympicsRpcMessageList rpcMessageList);
    }
}
