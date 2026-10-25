using System.Linq;
using UnityEditor;

namespace Elympics.Editor.UnityCli
{
    internal static class CliGames
    {
        public const string LoginFix = "log in via Tools > Elympics > Manage games in Elympics, then retry.";
        private const string CreateConfigFix = "repeat the call with --create_if_missing true, or use Tools > Elympics > Select or Create Config.";
        private const string AddGameFix = "import a sample (elympics_import_sample) or create a game (elympics_game_create --create_config true), "
            + "then select it with elympics_config_select_game.";

        public static ElympicsConfig RequireConfig(bool createIfMissing = false)
        {
            var config = ElympicsConfig.Load();
            if (config != null)
                return config;
            if (createIfMissing)
                return ElympicsTools.CreateConfigAsset();
            throw new ElympicsCliException(ElympicsCliErrors.NoConfig,
                $"The project has no ElympicsConfig (Resources/{ElympicsConfig.PathInResources}).", CreateConfigFix);
        }

        public static ElympicsGameConfig RequireActiveGame(ElympicsConfig config) =>
            config.GetCurrentGameConfig()
            ?? throw new ElympicsCliException(ElympicsCliErrors.NoActiveGame, "ElympicsConfig lists no game configs.", AddGameFix);

        public static ElympicsGameConfig ResolveGame(string gameId)
        {
            var explicitId = ExplicitGameId(gameId);
            var config = RequireConfig();
            if (explicitId == null)
                return RequireActiveGame(config);
            return config.AvailableGames.FirstOrDefault(game => game != null && game.GameId == explicitId)
                ?? throw new ElympicsCliException(ElympicsCliErrors.GameNotFound,
                    $"No game config in ElympicsConfig has the game ID {explicitId}.",
                    "list the configured games with elympics_status, or register one with elympics_config_select_game --config_asset <path>.");
        }

        public static string ResolveGameId(string gameId) =>
            ExplicitGameId(gameId) ?? RequireActiveGame(RequireConfig()).GameId;

        /// <returns>True if the game config wasn't in <paramref name="config"/> yet.</returns>
        public static bool RegisterAndActivate(ElympicsConfig config, ElympicsGameConfig gameConfig)
        {
            Undo.RecordObject(config, "Select Elympics game");
            var registered = !config.availableGames.Contains(gameConfig);
            if (registered)
                config.availableGames.Add(gameConfig);
            config.SwitchGame(config.availableGames.IndexOf(gameConfig));
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return registered;
        }

        /// <returns>Null for a blank <paramref name="gameId"/> (the active game).</returns>
        private static string ExplicitGameId(string gameId) => string.IsNullOrWhiteSpace(gameId) ? null : CliArgs.ValidateGuid(gameId, "game_id");
    }
}
