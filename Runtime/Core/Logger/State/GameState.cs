#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class GameState : IVisitableState
    {
        public string? GameId;
        public string? VersionName;
        public string? GameName;
        public string? GameMode;
        public string? GameVersionId;
        public string? FleetName;

        private static class Names
        {
            public const string GameId = "gameId";
            public const string VersionName = "versionName";
            public const string VersionNameLegacy = "version";
            public const string GameName = "gameName";
            public const string GameMode = "gameMode";
            public const string GameVersionId = "gameVersionId";
            public const string FleetName = "fleetName";
        }

        public bool Visit(IStateVisitor visitor)
        {
            var visited = false;
            visited |= visitor.ProcessOptionalProperty(nameof(GameId), GameId);
            visited |= visitor.ProcessOptionalProperty(nameof(VersionName), VersionName, legacyName: Names.VersionNameLegacy);
            visited |= visitor.ProcessOptionalProperty(nameof(GameName), GameName);
            visited |= visitor.ProcessOptionalProperty(nameof(GameMode), GameMode);
            visited |= visitor.ProcessOptionalProperty(nameof(GameVersionId), GameVersionId);
            visited |= visitor.ProcessOptionalProperty(nameof(FleetName), FleetName);
            return visited;
        }
    }
}
