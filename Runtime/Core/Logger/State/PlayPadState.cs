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
            visitor.ProcessOptionalProperty(nameof(Capabilities), Capabilities);
            visitor.ProcessOptionalProperty(nameof(FeatureAccess), FeatureAccess);
            visitor.ProcessOptionalProperty(nameof(TournamentId), TournamentId);
            return true;
        }
    }
}
