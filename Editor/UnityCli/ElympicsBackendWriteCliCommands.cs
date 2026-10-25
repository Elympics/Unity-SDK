using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.Communication;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Elympics.Editor.UnityCli
{
    /// <summary>These change the Elympics backend right away and need --confirm true.</summary>
    public static class ElympicsBackendWriteCliCommands
    {
        private const string ConfirmDescription = "Must be true: the change is applied to the Elympics backend right away.";

        internal static readonly string[] MatchTypes = { "PAIR", "GROUP", "TEAM", "SOLO" };
        internal static readonly string[] MatchmakingStrategies = { "RANDOM", "HUNGARIAN_PAIR", "GALE_SHAPLEY_PAIR", "RANDOM_SAMPLER_PAIR", "WORST_GAME_ANNEALING", "IMBALANCE_ANNEALING" };
        internal static readonly string[] SamplingStrategies = { "ALL_COMBINATIONS", "ALL_UNIQUE_COMBINATIONS", "EACH_BEST_PARTITIONING", "MEAN_GROUP_PARTITIONING", "RANDOM", "TEAM_PARTITIONING" };
        internal static readonly string[] PredictionStrategies = { "RANDOM", "DUMMY", "ELO", "BAYESIAN" };
        internal static readonly string[] MatchmakingModes = { "ACCEPT_ALL", "FASTEST_MATCHING", "FAST_MATCHING", "BALANCED", "FAIR_MATCHING", "MOST_FAIR_MATCHING" };

        [CliCommand("elympics_game_create",
            "Create an Elympics game in the logged-in account's organization. With --create_config true, also create its ElympicsGameConfig in "
            + "Assets/Resources/Elympics, register it in ElympicsConfig and make it the active game. Needs --confirm true.",
            Tags = new[] { "elympics", "upload" })]
        public static Task<object> GameCreate(
            [CliArg("name", "Game name, 1-63 characters.", Required = true)] string name,
            [CliArg("fleet_id", "Server fleet ID from elympics_fleets_list. Defaults to the backend's default fleet.")] string fleetId = "",
            [CliArg("region", "Initial region name (deprecated by the backend; prefer leaving it empty).")] string region = "",
            [CliArg("create_config", "Create, register and activate an ElympicsGameConfig for the new game.")] bool createConfig = false,
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                name = ValidateGameName(name);
                CliArgs.RequireConfirm(confirm, "Creating an Elympics game");
                var config = createConfig ? CliGames.RequireConfig(true) : null;
                var created = await ElympicsEditorApi.CreateGameAsync(name, CliArgs.Optional(region), CliArgs.Optional(fleetId), ct);
                var gameId = (string)created?["Id"];
                await UniTask.SwitchToMainThread();
                string configAsset = null;
                if (createConfig)
                {
                    if (string.IsNullOrEmpty(gameId))
                        throw new ElympicsCliException(ElympicsCliErrors.RequestFailed, "The game was created, but the backend's response has no game ID, so no game config was created.",
                            "find the game with elympics_games_list and create its config in Tools > Elympics > Manage games in Elympics.");
                    configAsset = CreateAndSelectGameConfig(config, gameId, (string)created["Name"] ?? name);
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

        private static string CreateAndSelectGameConfig(ElympicsConfig config, string gameId, string name)
        {
            var gameConfig = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            gameConfig.gameId = gameId;
            gameConfig.gameName = name;
            ElympicsTools.EnsureElympicsResourcesDirectoryExists();
            var safeName = new string(name.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());
            var path = AssetDatabase.GenerateUniqueAssetPath($"{ElympicsConfig.ElympicsResourcesPath}/{safeName}GameConfig.asset");
            AssetDatabase.CreateAsset(gameConfig, path);
            _ = CliGames.RegisterAndActivate(config, gameConfig);
            return path;
        }

        [CliCommand("elympics_game_update",
            "Change an Elympics game's settings. Only the given fields change: the others are read from the backend and sent back. Needs --confirm true.",
            Tags = new[] { "elympics", "upload" })]
        public static Task<object> GameUpdate(
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("name", "New game name, 1-63 characters.")] string name = null,
            [CliArg("fleet_id", "New server fleet ID from elympics_fleets_list.")] string fleetId = null,
            [CliArg("external_backend", "External backend URL; an empty string clears it.")] string externalBackend = null,
            [CliArg("event_queue_join_url", "Event queue join URL; an empty string clears it.")] string eventQueueJoinUrl = null,
            [CliArg("event_queue_leave_url", "Event queue leave URL; an empty string clears it.")] string eventQueueLeaveUrl = null,
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                if (new[] { name, fleetId, externalBackend, eventQueueJoinUrl, eventQueueLeaveUrl }.All(value => value == null))
                    throw ElympicsCliErrors.InvalidArgumentException("Nothing to change.", "pass at least one of --name, --fleet_id, --external_backend, --event_queue_join_url, --event_queue_leave_url.");
                if (name != null)
                    name = ValidateGameName(name);
                CliArgs.RequireConfirm(confirm, "Changing an Elympics game");
                var current = await ElympicsEditorApi.GetGameAsync(resolvedGameId, ct) as JObject ?? new JObject();
                var body = new JObject
                {
                    ["GameName"] = name ?? (string)current["Name"],
                    ["FleetId"] = fleetId ?? (string)current["FleetId"],
                    ["ExternalBackend"] = NullIfEmpty(externalBackend ?? (string)current["ExternalBackend"]),
                    ["EventQueueJoinUrl"] = NullIfEmpty(eventQueueJoinUrl ?? (string)current["EventQueueJoinUrl"]),
                    ["EventQueueLeaveUrl"] = NullIfEmpty(eventQueueLeaveUrl ?? (string)current["EventQueueLeaveUrl"]),
                };
                var updated = await ElympicsEditorApi.UpdateGameAsync(resolvedGameId, body, ct);
                return new { status = "updated", gameId = resolvedGameId, game = updated ?? body };
            });

        private static string NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

        [CliCommand("elympics_game_version_update",
            "Block or unblock an uploaded server version, or change its debug flags. Only the given flags change. Needs --confirm true.",
            Tags = new[] { "elympics", "upload" })]
        public static Task<object> GameVersionUpdate(
            [CliArg("version", "Version name (as set by elympics_config_set_version) or version ID from elympics_game_versions.", Required = true)] string version,
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
                var versionId = await ResolveVersionIdAsync(resolvedGameId, version, ct);
                var body = new JObject { ["Blocked"] = blocked, ["DebugMode"] = debugMode, ["DebugModeWithBots"] = debugModeWithBots };
                var updated = await ElympicsEditorApi.UpdateGameVersionAsync(resolvedGameId, versionId, body, ct);
                return new { status = "updated", gameId = resolvedGameId, versionId, version = updated ?? body };
            });

        [CliCommand("elympics_game_version_delete",
            "Delete an uploaded server version. Its name stays burnt: versions are write-once. Needs --confirm true.",
            Tags = new[] { "elympics", "upload" })]
        public static Task<object> GameVersionDelete(
            [CliArg("version", "Version name or version ID from elympics_game_versions.", Required = true)] string version,
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                version = CliArgs.Require(version, "version");
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                CliArgs.RequireConfirm(confirm, "Deleting a game version");
                var versionId = await ResolveVersionIdAsync(resolvedGameId, version, ct);
                _ = await ElympicsEditorApi.DeleteGameVersionAsync(resolvedGameId, versionId, ct);
                return new { status = "deleted", gameId = resolvedGameId, versionId };
            });

        private static async UniTask<string> ResolveVersionIdAsync(string gameId, string version, CancellationToken ct)
        {
            var versions = await ElympicsEditorApi.GetGameVersionsAsync(gameId, ct);
            var match = versions.Versions?.FirstOrDefault(candidate => candidate.Id == version)
                ?? versions.Versions?.FirstOrDefault(candidate => candidate.Version == version);
            return match?.Id ?? throw new ElympicsCliException(ElympicsCliErrors.RequestFailed, $"The game {gameId} has no version {version}.",
                "list the versions with elympics_game_versions and pass a name or ID from it.");
        }

        [CliCommand("elympics_queue_create", "Create a matchmaking queue for an Elympics game. Enum values follow the backend (e.g. --match_type SOLO). Needs --confirm true.",
            Tags = new[] { "elympics", "upload" })]
        public static Task<object> QueueCreate(
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
                if (numberOfTeams < 1 || playersInTeam < 1)
                    throw ElympicsCliErrors.InvalidArgumentException("--number_of_teams and --players_in_team must be at least 1.", "pass e.g. --number_of_teams 1 --players_in_team 1 for solo.");
                var body = new JObject
                {
                    ["Name"] = CliArgs.ValidateVersion(name, "name"),
                    ["MatchType"] = ValidateEnum(matchType, "match_type", MatchTypes),
                    ["MatchmakingStrategy"] = ValidateEnum(matchmakingStrategy, "matchmaking_strategy", MatchmakingStrategies),
                    ["SamplingStrategy"] = ValidateEnum(samplingStrategy, "sampling_strategy", SamplingStrategies),
                    ["PredictionStrategy"] = ValidateEnum(predictionStrategy, "prediction_strategy", PredictionStrategies),
                    ["MatchmakingMode"] = ValidateEnum(matchmakingMode, "matchmaking_mode", MatchmakingModes),
                    ["NumberOfTeams"] = numberOfTeams,
                    ["PlayersInTeam"] = playersInTeam,
                    ["MatchWithBots"] = matchWithBots,
                    ["MaxWaitTime"] = maxWaitTime,
                    ["MinPlayersToMatch"] = minPlayersToMatch,
                };
                CliArgs.RequireConfirm(confirm, "Creating a queue");
                var created = await ElympicsEditorApi.CreateQueueAsync(resolvedGameId, body, ct);
                return new { status = "created", gameId = resolvedGameId, queue = created ?? body };
            });

        [CliCommand("elympics_queue_update",
            "Change a matchmaking queue. Only the given fields change: the others are read from the backend and sent back. Needs --confirm true.",
            Tags = new[] { "elympics", "upload" })]
        public static Task<object> QueueUpdate(
            [CliArg("queue", "Queue ID or name from elympics_queues_list.", Required = true)] string queue,
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("name", "New queue name.")] string name = null,
            [CliArg("match_type", "PAIR, GROUP, TEAM or SOLO.")] string matchType = null,
            [CliArg("number_of_teams", "Number of teams.")] int? numberOfTeams = null,
            [CliArg("players_in_team", "Players in each team.")] int? playersInTeam = null,
            [CliArg("matchmaking_strategy", "Matchmaking strategy, as in elympics_queue_create.")] string matchmakingStrategy = null,
            [CliArg("sampling_strategy", "Sampling strategy, as in elympics_queue_create.")] string samplingStrategy = null,
            [CliArg("prediction_strategy", "Prediction strategy, as in elympics_queue_create.")] string predictionStrategy = null,
            [CliArg("matchmaking_mode", "Matchmaking mode, as in elympics_queue_create.")] string matchmakingMode = null,
            [CliArg("match_with_bots", "Fill missing players with bots.")] bool? matchWithBots = null,
            [CliArg("max_wait_time", "Maximum wait in seconds; -1 for no limit.")] int? maxWaitTime = null,
            [CliArg("min_players_to_match", "Minimum players to start a match.")] int? minPlayersToMatch = null,
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                queue = CliArgs.Require(queue, "queue");
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                var fields = new (string Key, object Value)[]
                {
                    ("Name", name == null ? null : CliArgs.ValidateVersion(name, "name")),
                    ("MatchType", matchType == null ? null : ValidateEnum(matchType, "match_type", MatchTypes)),
                    ("MatchmakingStrategy", matchmakingStrategy == null ? null : ValidateEnum(matchmakingStrategy, "matchmaking_strategy", MatchmakingStrategies)),
                    ("SamplingStrategy", samplingStrategy == null ? null : ValidateEnum(samplingStrategy, "sampling_strategy", SamplingStrategies)),
                    ("PredictionStrategy", predictionStrategy == null ? null : ValidateEnum(predictionStrategy, "prediction_strategy", PredictionStrategies)),
                    ("MatchmakingMode", matchmakingMode == null ? null : ValidateEnum(matchmakingMode, "matchmaking_mode", MatchmakingModes)),
                    ("NumberOfTeams", numberOfTeams),
                    ("PlayersInTeam", playersInTeam),
                    ("MatchWithBots", matchWithBots),
                    ("MaxWaitTime", maxWaitTime),
                    ("MinPlayersToMatch", minPlayersToMatch),
                };
                var changes = new JObject();
                foreach (var (key, value) in fields.Where(field => field.Value != null))
                    changes[key] = JToken.FromObject(value);
                if (!changes.HasValues)
                    throw ElympicsCliErrors.InvalidArgumentException("Nothing to change.", "pass at least one field to change, e.g. --max_wait_time 30.");
                CliArgs.RequireConfirm(confirm, "Changing a queue");

                var current = await FindQueueAsync(resolvedGameId, queue, ct);
                var body = new JObject();
                foreach (var field in fields.Select(candidate => candidate.Key).Append("SmartContractDeploymentId"))
                    body[field] = changes[field] ?? current[field];
                var queueId = (string)current["Id"];
                var updated = await ElympicsEditorApi.UpdateQueueAsync(resolvedGameId, queueId, body, ct);
                return new { status = "updated", gameId = resolvedGameId, queueId, queue = updated ?? body };
            });

        [CliCommand("elympics_queue_delete", "Delete a matchmaking queue. Needs --confirm true.", Tags = new[] { "elympics", "upload" })]
        public static Task<object> QueueDelete(
            [CliArg("queue", "Queue ID or name from elympics_queues_list.", Required = true)] string queue,
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("confirm", ConfirmDescription)] bool confirm = false,
            [CliArg("timeout_seconds", CliWeb.TimeoutDescription)] int timeoutSeconds = CliWeb.DefaultTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                queue = CliArgs.Require(queue, "queue");
                var resolvedGameId = CliGames.ResolveGameId(gameId);
                CliArgs.RequireConfirm(confirm, "Deleting a queue");
                var queueId = (string)(await FindQueueAsync(resolvedGameId, queue, ct))["Id"];
                _ = await ElympicsEditorApi.DeleteQueueAsync(resolvedGameId, queueId, ct);
                return new { status = "deleted", gameId = resolvedGameId, queueId };
            });

        private static async UniTask<JObject> FindQueueAsync(string gameId, string queue, CancellationToken ct)
        {
            var queues = await ElympicsEditorApi.GetQueuesAsync(gameId, ct) as JArray ?? new JArray();
            return queues.OfType<JObject>().FirstOrDefault(candidate => (string)candidate["Id"] == queue)
                ?? queues.OfType<JObject>().FirstOrDefault(candidate => (string)candidate["Name"] == queue)
                ?? throw new ElympicsCliException(ElympicsCliErrors.RequestFailed, $"The game {gameId} has no queue {queue}.",
                    "list the queues with elympics_queues_list and pass an ID or name from it.");
        }

        internal static string ValidateEnum(string value, string argumentName, string[] allowed)
        {
            value = CliArgs.Require(value, argumentName).ToUpperInvariant();
            if (!allowed.Contains(value))
                throw ElympicsCliErrors.InvalidArgumentException($"Unknown --{argumentName} '{value}'.", $"use one of: {string.Join(", ", allowed)}.");
            return value;
        }
    }
}
