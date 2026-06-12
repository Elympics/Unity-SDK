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

        public bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(GameId), GameId);
            visitor.ProcessProperty(nameof(VersionName), VersionName);
            visitor.ProcessOptionalProperty(nameof(GameName), GameName);
            visitor.ProcessOptionalProperty(nameof(GameMode), GameMode);
            visitor.ProcessOptionalProperty(nameof(GameVersionId), GameVersionId);
            visitor.ProcessOptionalProperty(nameof(FleetName), FleetName);
            return true;
        }
    }
}
