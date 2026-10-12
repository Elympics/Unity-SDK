#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class PlayPadState : IVisitableState
    {
        public string? ProtocolVersion;
        public string? Capabilities;
        public string? FeatureAccess;
        public string? TournamentId;

        private static class Names
        {
            public const string ProtocolVersion = "protocolVersion";
            public const string Capabilities = "capabilities";
            public const string FeatureAccess = "featureAccess";
            public const string TournamentId = "tournamentId";
        }

        public bool Visit(IStateVisitor visitor)
        {
            var visited = false;
            visited |= visitor.ProcessOptionalProperty(Names.ProtocolVersion, ProtocolVersion);
            visited |= visitor.ProcessOptionalProperty(Names.Capabilities, Capabilities);
            visited |= visitor.ProcessOptionalProperty(Names.FeatureAccess, FeatureAccess);
            visited |= visitor.ProcessOptionalProperty(Names.TournamentId, TournamentId);
            return visited;
        }
    }
}
