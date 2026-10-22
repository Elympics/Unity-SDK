using System.Linq;

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

        /// <param name="gameId">Empty for the active game.</param>
        public static ElympicsGameConfig ResolveGame(string gameId)
        {
            var config = RequireConfig();
            if (string.IsNullOrWhiteSpace(gameId))
                return RequireActiveGame(config);
            gameId = gameId.Trim();
            return config.AvailableGames.FirstOrDefault(game => game != null && game.GameId == gameId)
                ?? throw new ElympicsCliException(ElympicsCliErrors.GameNotFound,
                    $"No game config in ElympicsConfig has the game ID {gameId}.",
                    "list the configured games with elympics_status, or register one with elympics_config_select_game --config_asset <path>.");
        }

        /// <param name="gameId">Empty for the active game's ID.</param>
        public static string ResolveGameId(string gameId) =>
            string.IsNullOrWhiteSpace(gameId) ? RequireActiveGame(RequireConfig()).GameId : CliArgs.ValidateGuid(gameId, "game_id");

        public static void RequireLogin()
        {
            if (!ElympicsConfig.IsLogin)
                throw new ElympicsCliException(ElympicsCliErrors.NotLoggedIn, "The Editor isn't logged in to Elympics.", LoginFix);
        }
    }
}
