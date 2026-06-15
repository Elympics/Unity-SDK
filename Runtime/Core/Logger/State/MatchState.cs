#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class MatchState : IVisitableState
    {
        public string? RoomId;
        public string? QueueName;
        public string? MatchId;

        public bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessOptionalProperty(nameof(RoomId), RoomId);
            visitor.ProcessOptionalProperty(nameof(QueueName), QueueName);
            visitor.ProcessOptionalProperty(nameof(MatchId), MatchId);
            return true;
        }
    }
}
