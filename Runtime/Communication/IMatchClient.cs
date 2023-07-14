using System;
using System.Threading.Tasks;
using MatchTcpClients.Synchronizer;

namespace Elympics
{
    public interface IMatchClient
    {
        event Action<TimeSynchronizationData> Synchronized;
        event Action<ElympicsSnapshot> SnapshotReceived;

        Task SendInputReliable(ElympicsInput input);
        Task SendInputUnreliable(ElympicsInput input);
    }
}
