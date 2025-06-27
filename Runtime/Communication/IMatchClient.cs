using System;
using System.Threading.Tasks;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
    public interface IMatchClient : IDisposable
    {
        event Action<TimeSynchronizationData> Synchronized;
        event Action<ElympicsSnapshot> SnapshotReceived;
        event Action<ElympicsRpcMessageList> RpcMessageListReceived;

        void AddInputToSendBuffer(ElympicsInput input);
        Task SendRpcMessageList(ElympicsRpcMessageList rpcMessageList);
        Task SendBufferInput(long tick);
        void SetLastReceivedSnapshot(long tick);
    }
}
