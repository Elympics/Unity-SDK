#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class MatchState : IVisitableState
    {
        public string? RoomId;
        public string? QueueName;
        public string? MatchId;

        private static class Names
        {
            public const string RoomId = "roomId";
            public const string QueueName = "queueName";
            public const string MatchId = "matchId";
        }

        public bool Visit(IStateVisitor visitor)
        {
            var visited = false;
            visited |= visitor.ProcessOptionalProperty(Names.RoomId, RoomId);
            visited |= visitor.ProcessOptionalProperty(Names.QueueName, QueueName);
            visited |= visitor.ProcessOptionalProperty(Names.MatchId, MatchId);
            return visited;
        }
    }
}
