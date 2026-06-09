#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class GameState : IVisitableState
    {
        public readonly string GameId;
        public readonly string VersionName;
        public string? GameName;
        public string? GameMode;
        public string? GameVersionId;
        public string? FleetName;

        public GameState(string gameId, string versionName) => (GameId, VersionName) = (gameId, versionName);

        public void Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(GameId), GameId);
            visitor.ProcessProperty(nameof(VersionName), VersionName);
            if (!string.IsNullOrEmpty(GameName))
                visitor.ProcessProperty(nameof(GameName), GameName);
            if (!string.IsNullOrEmpty(GameMode))
                visitor.ProcessProperty(nameof(GameMode), GameMode);
            if (!string.IsNullOrEmpty(GameVersionId))
                visitor.ProcessProperty(nameof(GameVersionId), GameVersionId);
            if (!string.IsNullOrEmpty(FleetName))
                visitor.ProcessProperty(nameof(FleetName), FleetName);
        }
    }
}
