#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class PlayPadState : IVisitableState
    {
        public string ProtocolVersion;
        public string? Capabilities;
        public string? FeatureAccess;
        public string? TournamentId;

        public PlayPadState(string protocolVersion) => ProtocolVersion = protocolVersion;

        public bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(ProtocolVersion), ProtocolVersion);
            if (!string.IsNullOrEmpty(Capabilities))
                visitor.ProcessProperty(nameof(Capabilities), Capabilities);
            if (!string.IsNullOrEmpty(FeatureAccess))
                visitor.ProcessProperty(nameof(FeatureAccess), FeatureAccess);
            if (!string.IsNullOrEmpty(TournamentId))
                visitor.ProcessProperty(nameof(TournamentId), TournamentId);
            return true;
        }
    }
}
