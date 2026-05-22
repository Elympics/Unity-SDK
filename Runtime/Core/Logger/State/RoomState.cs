#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class RoomState : IVisitableState
    {
        public string RoomId;
        public string? QueueName;
        public string? MatchId;
        public string? TcpUdpServerAddress;
        public string? WebServerAddress;

        public RoomState(string roomId) => RoomId = roomId;

        public void Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(RoomId), RoomId);
            if (!string.IsNullOrEmpty(QueueName))
                visitor.ProcessProperty(nameof(QueueName), QueueName);
            if (!string.IsNullOrEmpty(MatchId))
                visitor.ProcessProperty(nameof(MatchId), MatchId);
            if (!string.IsNullOrEmpty(TcpUdpServerAddress))
                visitor.ProcessProperty(nameof(TcpUdpServerAddress), TcpUdpServerAddress);
            if (!string.IsNullOrEmpty(WebServerAddress))
                visitor.ProcessProperty(nameof(WebServerAddress), WebServerAddress);
        }
    }
}
