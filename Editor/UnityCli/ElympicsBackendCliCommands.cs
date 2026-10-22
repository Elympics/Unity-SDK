using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elympics.Editor.Communication;
using Unity.Pipeline.Commands;

namespace Elympics.Editor.UnityCli
{
    /// <summary>Elympics backend calls. They use the Editor's current login.</summary>
    public static class ElympicsBackendCliCommands
    {
        private const string TimeoutDescription = "Seconds to wait for the backend.";
        private const string GameIdDescription = "Elympics game ID. Defaults to the active game config's ID.";

        [CliCommand("elympics_games_list", "List the Elympics games the logged-in account can access, marking the ones in ElympicsConfig and the active one.",
            Tags = new[] { "elympics", "read" })]
        public static Task<object> GamesList([CliArg("timeout_seconds", TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var games = await ElympicsEditorApi.GetGamesAsync(ct);
                var config = ElympicsConfig.Load();
                var configured = config != null ? config.AvailableGames.Where(game => game != null).Select(game => game.GameId).ToList() : new List<string>();
                var active = config?.GetCurrentGameConfig()?.GameId;
                return new
                {
                    status = "ok",
                    games = games.Select(game => new { id = game.Id, name = game.Name, isConfigured = configured.Contains(game.Id), isActive = game.Id == active }).ToArray(),
                };
            });

        [CliCommand("elympics_game_versions", "List the uploaded server versions of an Elympics game, with the IDs that elympics_game_version_update and _delete take.",
            Tags = new[] { "elympics", "read" })]
        public static Task<object> GameVersions(
            [CliArg("game_id", GameIdDescription)] string gameId = "",
            [CliArg("timeout_seconds", TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                var response = await ElympicsEditorApi.GetGameVersionsAsync(resolvedGameId, ct);
                return new
                {
                    status = "ok",
                    gameId = resolvedGameId,
                    gameName = response.GameName,
                    versions = (response.Versions ?? new List<ElympicsWebIntegration.GameVersionResponseModel>()).Select(version => new
                    {
                        id = version.Id,
                        version = version.Version,
                        uploaded = version.Uploaded,
                        uploadedTime = version.UploadedTime,
                        blocked = version.Blocked,
                        debugMode = version.DebugMode,
                        debugModeWithBots = version.DebugModeWithBots,
                    }).ToArray(),
                };
            });

        [CliCommand("elympics_queues_list", "List the matchmaking queues of an Elympics game as the backend returns them.", Tags = new[] { "elympics", "read" })]
        public static Task<object> QueuesList(
            [CliArg("game_id", GameIdDescription)] string gameId = "",
            [CliArg("timeout_seconds", TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                return new { status = "ok", gameId = resolvedGameId, queues = await ElympicsEditorApi.GetQueuesAsync(resolvedGameId, ct) };
            });

        [CliCommand("elympics_regions_list", "List the regions an Elympics game can run in, or every region of the Elympics cloud with --all true.",
            Tags = new[] { "elympics", "read" })]
        public static Task<object> RegionsList(
            [CliArg("game_id", GameIdDescription)] string gameId = "",
            [CliArg("all", "List every region of the Elympics cloud instead of the game's.")] bool all = false,
            [CliArg("timeout_seconds", TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var resolvedGameId = all ? null : CliGames.ResolveGameId(gameId);
                var regions = await ElympicsEditorApi.GetRegionsAsync(resolvedGameId, ct);
                return new { status = "ok", gameId = resolvedGameId, regions = regions.Select(region => new { id = region.Id, name = region.Name }).ToArray() };
            });

        [CliCommand("elympics_fleets_list", "List the server fleets available to the logged-in account, for elympics_game_create --fleet_id.",
            Tags = new[] { "elympics", "read" })]
        public static Task<object> FleetsList([CliArg("timeout_seconds", TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct => new { status = "ok", fleets = await ElympicsEditorApi.GetFleetsAsync(ct) });
    }
}
