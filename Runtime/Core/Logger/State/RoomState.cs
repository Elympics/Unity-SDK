#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class RoomState : IVisitableState
    {
        public readonly string RoomId;
        public string? QueueName;
        public string? MatchId;
        public string? TcpUdpServerAddress;
        public string? WebServerAddress;

        public RoomState(string roomId) => RoomId = roomId;

        public bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(RoomId), RoomId);
            visitor.ProcessOptionalProperty(nameof(QueueName), QueueName);
            visitor.ProcessOptionalProperty(nameof(MatchId), MatchId);
            visitor.ProcessOptionalProperty(nameof(TcpUdpServerAddress), TcpUdpServerAddress);
            visitor.ProcessOptionalProperty(nameof(WebServerAddress), WebServerAddress);
            return true;
        }
    }
}
