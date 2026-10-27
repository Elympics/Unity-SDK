using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elympics.Editor.Communication;
using Unity.Pipeline.Commands;

namespace Elympics.Editor.UnityCli
{
    public static class ElympicsBackendCliCommands
    {
        [CliCommand("elympics_list_games", "List the Elympics games the logged-in account can access, marking the ones in ElympicsConfig and the active one.",
            Tags = new[] { "elympics", "read" })]
        public static Task<object> ListGames([CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var games = await ElympicsEditorApi.GetGamesAsync(timeoutSeconds, ct);
                var config = ElympicsConfig.Load();
                var configured = config != null ? config.AvailableGames.Where(game => game != null).Select(game => game.GameId).ToList() : new List<string>();
                var active = config?.GetCurrentGameConfig()?.GameId;
                return new
                {
                    status = "ok",
                    games = games.Select(game => new { id = game.Id, name = game.Name, isConfigured = configured.Contains(game.Id), isActive = game.Id == active }).ToArray(),
                };
            });

        [CliCommand("elympics_list_game_versions", "List the uploaded server versions of an Elympics game, with the IDs that elympics_update_game_version and _delete take.",
            Tags = new[] { "elympics", "read" })]
        public static Task<object> ListGameVersions(
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                var response = await ElympicsEditorApi.GetGameVersionsAsync(resolvedGameId, timeoutSeconds, ct);
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

        [CliCommand("elympics_list_queues", "List the matchmaking queues of an Elympics game as the backend returns them.", Tags = new[] { "elympics", "read" })]
        public static Task<object> ListQueues(
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                return new { status = "ok", gameId = resolvedGameId, queues = await ElympicsEditorApi.GetQueuesAsync(resolvedGameId, timeoutSeconds, ct) };
            });

        [CliCommand("elympics_list_regions", "List the regions an Elympics game can run in, or every region of the Elympics cloud with --all true.",
            Tags = new[] { "elympics", "read" })]
        public static Task<object> ListRegions(
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("all", "List every region of the Elympics cloud instead of the game's.")] bool all = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var resolvedGameId = all ? null : CliGames.ResolveGameId(gameId);
                var regions = await ElympicsEditorApi.GetRegionsAsync(resolvedGameId, timeoutSeconds, ct);
                return new { status = "ok", gameId = resolvedGameId, regions = regions.Select(region => new { id = region.Id, name = region.Name }).ToArray() };
            });

        [CliCommand("elympics_list_fleets", "List the server fleets available to the logged-in account, for elympics_create_game --fleet_id.",
            Tags = new[] { "elympics", "read" })]
        public static Task<object> ListFleets([CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct => new { status = "ok", fleets = await ElympicsEditorApi.GetFleetsAsync(timeoutSeconds, ct) });
    }
}
