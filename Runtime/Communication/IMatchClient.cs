using System;
using Cysharp.Threading.Tasks;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
    public interface IMatchClient : IDisposable
    {
        event Action<TimeSynchronizationData> Synchronized;
        event Action<ElympicsSnapshot> SnapshotReceived;
        event Action<ElympicsRpcMessageList> RpcMessageListReceived;

        void AddInputToSendBuffer(ElympicsInput input);
        UniTask SendRpcMessageList(ElympicsRpcMessageList rpcMessageList, bool reliable);
        UniTask SendBufferInput(long tick);
        void SetLastReceivedSnapshot(long tick);
    }
}
