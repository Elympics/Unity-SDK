#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.Communication;
using Unity.Pipeline.Commands;

namespace Elympics.Editor.UnityCli
{
    /// <summary>These change the Elympics backend right away and need --confirm true.</summary>
    public static class ElympicsBackendWriteCliCommands
    {
        private const string ConfirmDescription = "Must be true: the change is applied to the Elympics backend right away.";

        private static readonly string[] MatchTypes = { "PAIR", "GROUP", "TEAM", "SOLO" };
        private static readonly string[] MatchmakingStrategies = { "RANDOM", "HUNGARIAN_PAIR", "GALE_SHAPLEY_PAIR", "RANDOM_SAMPLER_PAIR", "WORST_GAME_ANNEALING", "IMBALANCE_ANNEALING" };
        private static readonly string[] SamplingStrategies = { "ALL_COMBINATIONS", "ALL_UNIQUE_COMBINATIONS", "EACH_BEST_PARTITIONING", "MEAN_GROUP_PARTITIONING", "RANDOM", "TEAM_PARTITIONING" };
        private static readonly string[] PredictionStrategies = { "RANDOM", "DUMMY", "ELO", "BAYESIAN" };
        private static readonly string[] MatchmakingModes = { "ACCEPT_ALL", "FASTEST_MATCHING", "FAST_MATCHING", "BALANCED", "FAIR_MATCHING", "MOST_FAIR_MATCHING" };

        [CliCommand("elympics_create_game",
            "Create an Elympics game in the logged-in account's organization. With --create_config true, also create its ElympicsGameConfig in "
            + "Assets/Resources/Elympics, register it in ElympicsConfig and make it the active game. Needs --confirm true.",
            Tags = new[] { "elympics/backend" })]
        public static Task<object> CreateGame(
            [CliArg("name", "Game name, 1-63 characters.", Required = true)] string name,
            [CliArg("fleet_id", "Server fleet ID from elympics_list_fleets. Defaults to the backend's default fleet.")] string fleetId = "",
            [CliArg("region", "Initial region name (deprecated by the backend; prefer leaving it empty).")] string region = "",
            [CliArg("create_config", "Create, register and activate an ElympicsGameConfig for the new game.")] bool createConfig = false,
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                name = ValidateGameName(name);
                CliArgs.RequireConfirm(confirm, "Creating an Elympics game");
                var config = createConfig ? CliGames.RequireConfig(true) : null;
                var created = await ElympicsEditorApi.CreateGameAsync(name, CliArgs.TrimToNull(region), CliArgs.TrimToNull(fleetId), timeoutSeconds, ct);
                var gameId = created?.Id;
                await UniTask.SwitchToMainThread();
                string? configAsset = null;
                if (createConfig)
                {
                    if (string.IsNullOrEmpty(gameId))
                        throw new ElympicsCliException(ElympicsCliErrors.RequestFailed, "The game was created, but the backend's response has no game ID, so no game config was created.",
                            "find the game with elympics_list_games and create its config in Tools > Elympics > Manage games in Elympics.");
                    try
                    {
                        configAsset = ElympicsTools.CreateAndActivateGameConfig(config!, gameId!, created!.Name ?? name);
                    }
                    catch (Exception exception) when (exception is not ElympicsCliException)
                    {
                        throw new ElympicsCliException(ElympicsCliErrors.RequestFailed, $"The game {gameId} was created, but creating its game config failed: {exception.Message}",
                            $"do not retry elympics_create_game (it would create a duplicate game); create the config for game ID {gameId} in Tools > Elympics > Manage games in Elympics.");
                    }
                }
                return new { status = "created", gameId, game = created, configAsset };
            });

        private static string ValidateGameName(string name)
        {
            name = CliArgs.Require(name, "name");
            if (name.Length > 63)
                throw ElympicsCliErrors.InvalidArgumentException("--name is longer than 63 characters.", "pass a shorter name.");
            return name;
        }

        [CliCommand("elympics_update_game",
            "Change an Elympics game's settings. Only the given fields are sent; the backend keeps the others. Needs --confirm true.",
            Tags = new[] { "elympics/backend" })]
        public static Task<object> UpdateGame(
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("name", "New game name, 1-63 characters.")] string? name = null,
            [CliArg("fleet_id", "New server fleet ID from elympics_list_fleets.")] string? fleetId = null,
            [CliArg("external_backend", "External backend URL; an empty string clears it.")] string? externalBackend = null,
            [CliArg("event_queue_join_url", "Event queue join URL; an empty string clears it.")] string? eventQueueJoinUrl = null,
            [CliArg("event_queue_leave_url", "Event queue leave URL; an empty string clears it.")] string? eventQueueLeaveUrl = null,
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                if (new[] { name, fleetId, externalBackend, eventQueueJoinUrl, eventQueueLeaveUrl }.All(value => value == null))
                    throw ElympicsCliErrors.InvalidArgumentException("Nothing to change.", "pass at least one of --name, --fleet_id, --external_backend, --event_queue_join_url, --event_queue_leave_url.");
                if (name != null)
                    name = ValidateGameName(name);
                if (fleetId != null && string.IsNullOrWhiteSpace(fleetId))
                    throw ElympicsCliErrors.InvalidArgumentException("--fleet_id is empty.", "pass a fleet ID from elympics_list_fleets, or leave --fleet_id out to keep the current fleet.");
                CliArgs.RequireConfirm(confirm, "Changing an Elympics game");
                var body = new UpdateGameRequestModel
                {
                    GameName = name,
                    FleetId = fleetId?.Trim(),
                    ExternalBackend = externalBackend,
                    EventQueueJoinUrl = eventQueueJoinUrl,
                    EventQueueLeaveUrl = eventQueueLeaveUrl,
                };
                var updated = await ElympicsEditorApi.UpdateGameAsync(resolvedGameId, body, timeoutSeconds, ct);
                return new { status = "updated", gameId = resolvedGameId, game = (object?)updated ?? body };
            });

        [CliCommand("elympics_update_game_version",
            "Block or unblock an uploaded server version, or change its debug flags. Only the given flags change. Needs --confirm true.",
            Tags = new[] { "elympics/backend" })]
        public static Task<object> UpdateGameVersion(
            [CliArg("version", "Version name (as set by elympics_set_config_game_version) or version ID from elympics_list_game_versions.", Required = true)] string version,
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("blocked", "Block (true) or unblock (false) the version for new matches.")] bool? blocked = null,
            [CliArg("debug_mode", "Run matches of this version in debug mode.")] bool? debugMode = null,
            [CliArg("debug_mode_with_bots", "Run debug matches of this version with bots.")] bool? debugModeWithBots = null,
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                version = CliArgs.Require(version, "version");
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                if (blocked == null && debugMode == null && debugModeWithBots == null)
                    throw ElympicsCliErrors.InvalidArgumentException("Nothing to change.", "pass at least one of --blocked, --debug_mode, --debug_mode_with_bots.");
                CliArgs.RequireConfirm(confirm, "Changing a game version");
                var versionId = await ResolveVersionIdAsync(resolvedGameId, version, timeoutSeconds, ct);
                var body = new UpdateGameVersionRequestModel { Blocked = blocked, DebugMode = debugMode, DebugModeWithBots = debugModeWithBots };
                await ElympicsEditorApi.UpdateGameVersionAsync(resolvedGameId, versionId, body, timeoutSeconds, ct);
                var updated = await ElympicsEditorApi.GetGameVersionAsync(resolvedGameId, versionId, timeoutSeconds, ct);
                return new { status = "updated", gameId = resolvedGameId, versionId, version = updated };
            });

        [CliCommand("elympics_delete_game_version",
            "Delete an uploaded server version. Its name stays burnt: versions are write-once. Needs --confirm true.",
            Tags = new[] { "elympics/backend" })]
        public static Task<object> DeleteGameVersion(
            [CliArg("version", "Version name or version ID from elympics_list_game_versions.", Required = true)] string version,
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                version = CliArgs.Require(version, "version");
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                CliArgs.RequireConfirm(confirm, "Deleting a game version");
                var versionId = await ResolveVersionIdAsync(resolvedGameId, version, timeoutSeconds, ct);
                await ElympicsEditorApi.DeleteGameVersionAsync(resolvedGameId, versionId, timeoutSeconds, ct);
                return new { status = "deleted", gameId = resolvedGameId, versionId };
            });

        private static async UniTask<string> ResolveVersionIdAsync(string gameId, string version, int timeoutSeconds, CancellationToken ct)
        {
            var versions = await ElympicsEditorApi.GetGameVersionsAsync(gameId, timeoutSeconds, ct);
            var match = versions.Versions?.FirstOrDefault(candidate => candidate.Id == version)
                ?? versions.Versions?.FirstOrDefault(candidate => candidate.Version == version);
            return match?.Id ?? throw new ElympicsCliException(ElympicsCliErrors.RequestFailed, $"The game {gameId} has no version {version}.",
                "list the versions with elympics_list_game_versions and pass a name or ID from it.");
        }

        [CliCommand("elympics_create_queue", "Create a matchmaking queue for an Elympics game. Enum values follow the backend (e.g. --match_type SOLO). Needs --confirm true.",
            Tags = new[] { "elympics/backend" })]
        public static Task<object> CreateQueue(
            [CliArg("name", "Queue name: 1-63 Latin letters, digits, dashes and dots.", Required = true)] string name,
            [CliArg("match_type", "PAIR, GROUP, TEAM or SOLO.", Required = true)] string matchType,
            [CliArg("number_of_teams", "Number of teams.", Required = true)] int numberOfTeams,
            [CliArg("players_in_team", "Players in each team.", Required = true)] int playersInTeam,
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("matchmaking_strategy", "RANDOM (default), HUNGARIAN_PAIR, GALE_SHAPLEY_PAIR, RANDOM_SAMPLER_PAIR, WORST_GAME_ANNEALING or IMBALANCE_ANNEALING.")] string matchmakingStrategy = "RANDOM",
            [CliArg("sampling_strategy", "RANDOM (default), ALL_COMBINATIONS, ALL_UNIQUE_COMBINATIONS, EACH_BEST_PARTITIONING, MEAN_GROUP_PARTITIONING or TEAM_PARTITIONING.")] string samplingStrategy = "RANDOM",
            [CliArg("prediction_strategy", "RANDOM (default), DUMMY, ELO or BAYESIAN.")] string predictionStrategy = "RANDOM",
            [CliArg("matchmaking_mode", "BALANCED (default), ACCEPT_ALL, FASTEST_MATCHING, FAST_MATCHING, FAIR_MATCHING or MOST_FAIR_MATCHING.")] string matchmakingMode = "BALANCED",
            [CliArg("match_with_bots", "Fill missing players with bots.")] bool? matchWithBots = null,
            [CliArg("max_wait_time", "Maximum wait in seconds; -1 for no limit.")] int? maxWaitTime = null,
            [CliArg("min_players_to_match", "Minimum players to start a match.")] int? minPlayersToMatch = null,
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                ValidateTeamSizes(numberOfTeams, playersInTeam);
                var body = new QueueSettingsModel
                {
                    Name = CliArgs.ValidateName(name, "name"),
                    MatchType = ValidateEnum(matchType, "match_type", MatchTypes),
                    MatchmakingStrategy = ValidateEnum(matchmakingStrategy, "matchmaking_strategy", MatchmakingStrategies),
                    SamplingStrategy = ValidateEnum(samplingStrategy, "sampling_strategy", SamplingStrategies),
                    PredictionStrategy = ValidateEnum(predictionStrategy, "prediction_strategy", PredictionStrategies),
                    MatchmakingMode = ValidateEnum(matchmakingMode, "matchmaking_mode", MatchmakingModes),
                    NumberOfTeams = numberOfTeams,
                    PlayersInTeam = playersInTeam,
                    MatchWithBots = matchWithBots,
                    MaxWaitTime = maxWaitTime,
                    MinPlayersToMatch = minPlayersToMatch,
                };
                CliArgs.RequireConfirm(confirm, "Creating a queue");
                var created = await ElympicsEditorApi.CreateQueueAsync(resolvedGameId, body, timeoutSeconds, ct);
                return new { status = "created", gameId = resolvedGameId, queue = created ?? body };
            });

        private static void ValidateTeamSizes(int? numberOfTeams, int? playersInTeam)
        {
            if (numberOfTeams < 1 || playersInTeam < 1)
                throw ElympicsCliErrors.InvalidArgumentException("--number_of_teams and --players_in_team must be at least 1.", "pass e.g. --number_of_teams 1 --players_in_team 1 for solo.");
        }

        [CliCommand("elympics_update_queue",
            "Change a matchmaking queue. Only the given fields are sent; the backend keeps the others. Needs --confirm true.",
            Tags = new[] { "elympics/backend" })]
        public static Task<object> UpdateQueue(
            [CliArg("queue", "Queue ID or name from elympics_list_queues.", Required = true)] string queue,
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("name", "New queue name.")] string? name = null,
            [CliArg("match_type", "PAIR, GROUP, TEAM or SOLO.")] string? matchType = null,
            [CliArg("number_of_teams", "Number of teams.")] int? numberOfTeams = null,
            [CliArg("players_in_team", "Players in each team.")] int? playersInTeam = null,
            [CliArg("matchmaking_strategy", "Matchmaking strategy, as in elympics_create_queue.")] string? matchmakingStrategy = null,
            [CliArg("sampling_strategy", "Sampling strategy, as in elympics_create_queue.")] string? samplingStrategy = null,
            [CliArg("prediction_strategy", "Prediction strategy, as in elympics_create_queue.")] string? predictionStrategy = null,
            [CliArg("matchmaking_mode", "Matchmaking mode, as in elympics_create_queue.")] string? matchmakingMode = null,
            [CliArg("match_with_bots", "Fill missing players with bots. Without --min_players_to_match, the queue's current value is sent with it, as the backend requires.")] bool? matchWithBots = null,
            [CliArg("max_wait_time", "Maximum wait in seconds; -1 for no limit.")] int? maxWaitTime = null,
            [CliArg("min_players_to_match", "Minimum players to start a match.")] int? minPlayersToMatch = null,
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                queue = CliArgs.Require(queue, "queue");
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                ValidateTeamSizes(numberOfTeams, playersInTeam);
                if (new object?[] { name, matchType, numberOfTeams, playersInTeam, matchmakingStrategy, samplingStrategy, predictionStrategy, matchmakingMode, matchWithBots, maxWaitTime, minPlayersToMatch }
                    .All(value => value == null))
                    throw ElympicsCliErrors.InvalidArgumentException("Nothing to change.", "pass at least one field to change, e.g. --max_wait_time 30.");
                name = name == null ? null : CliArgs.ValidateName(name, "name");
                matchType = matchType == null ? null : ValidateEnum(matchType, "match_type", MatchTypes);
                matchmakingStrategy = matchmakingStrategy == null ? null : ValidateEnum(matchmakingStrategy, "matchmaking_strategy", MatchmakingStrategies);
                samplingStrategy = samplingStrategy == null ? null : ValidateEnum(samplingStrategy, "sampling_strategy", SamplingStrategies);
                predictionStrategy = predictionStrategy == null ? null : ValidateEnum(predictionStrategy, "prediction_strategy", PredictionStrategies);
                matchmakingMode = matchmakingMode == null ? null : ValidateEnum(matchmakingMode, "matchmaking_mode", MatchmakingModes);
                CliArgs.RequireConfirm(confirm, "Changing a queue");

                var current = await FindQueueAsync(resolvedGameId, queue, timeoutSeconds, ct);
                if (matchWithBots != null && minPlayersToMatch == null)
                    minPlayersToMatch = current.MinPlayersToMatch
                        ?? throw ElympicsCliErrors.InvalidArgumentException("The backend needs --min_players_to_match with --match_with_bots, and the queue has none.",
                            "pass --min_players_to_match too.");
                var body = new UpdateQueueRequestModel
                {
                    Name = name,
                    MatchType = matchType,
                    MatchmakingStrategy = matchmakingStrategy,
                    SamplingStrategy = samplingStrategy,
                    PredictionStrategy = predictionStrategy,
                    MatchmakingMode = matchmakingMode,
                    NumberOfTeams = numberOfTeams,
                    PlayersInTeam = playersInTeam,
                    MatchWithBots = matchWithBots,
                    MaxWaitTime = maxWaitTime,
                    MinPlayersToMatch = minPlayersToMatch,
                };
                var queueId = current.Id;
                var updated = await ElympicsEditorApi.UpdateQueueAsync(resolvedGameId, queueId, body, timeoutSeconds, ct);
                return new { status = "updated", gameId = resolvedGameId, queueId, queue = (object?)updated ?? body };
            });

        [CliCommand("elympics_delete_queue", "Delete a matchmaking queue. Needs --confirm true.", Tags = new[] { "elympics/backend" })]
        public static Task<object> DeleteQueue(
            [CliArg("queue", "Queue ID or name from elympics_list_queues.", Required = true)] string queue,
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                queue = CliArgs.Require(queue, "queue");
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                CliArgs.RequireConfirm(confirm, "Deleting a queue");
                var queueId = (await FindQueueAsync(resolvedGameId, queue, timeoutSeconds, ct)).Id;
                await ElympicsEditorApi.DeleteQueueAsync(resolvedGameId, queueId, timeoutSeconds, ct);
                return new { status = "deleted", gameId = resolvedGameId, queueId };
            });

        private static async UniTask<QueueModel> FindQueueAsync(string gameId, string queue, int timeoutSeconds, CancellationToken ct)
        {
            var queues = await ElympicsEditorApi.GetQueuesAsync(gameId, timeoutSeconds, ct);
            return queues.FirstOrDefault(candidate => candidate?.Id == queue)
                ?? queues.FirstOrDefault(candidate => candidate?.Name == queue)
                ?? throw new ElympicsCliException(ElympicsCliErrors.RequestFailed, $"The game {gameId} has no queue {queue}.",
                    "list the queues with elympics_list_queues and pass an ID or name from it.");
        }

        private static string ValidateEnum(string value, string argumentName, string[] allowed)
        {
            value = CliArgs.Require(value, argumentName).ToUpperInvariant();
            if (!allowed.Contains(value))
                throw ElympicsCliErrors.InvalidArgumentException($"Unknown --{argumentName} '{value}'.", $"use one of: {string.Join(", ", allowed)}.");
            return value;
        }
    }
}
