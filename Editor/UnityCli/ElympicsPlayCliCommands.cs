#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Editor.Launcher;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Elympics.Editor.UnityCli
{
    public static class ElympicsPlayCliCommands
    {
        internal enum LaunchMode
        {
            SinglePlayer,
            HalfRemoteServer,
            HalfRemoteClient,
        }

        private const string MppmPackage = "com.unity.multiplayer.playmode";
        private const string NotLaunched = "NotLaunched";
        private const string Launched = "Launched";
        private const string TagsFix = "pass e.g. --tags '{\"2\":[\"Elympics:halfRemoteServer\"],\"3\":[]}' (an empty list removes the Elympics: tags).";

        [CliCommand("elympics_launch",
            "Launch a match in the Editor like the Match Launcher window: single_player, half_remote_server (virtual players from elympics_set_virtual_players "
            + "join as clients) or half_remote_client. Returns before Play mode starts (the domain reload drops the connection): "
            + "poll elympics_get_match_state until lastLaunch.result is running, failed or timed_out.",
            Tags = new[] { "elympics/play" })]
        public static object Launch(
            [CliArg("mode", "single_player, half_remote_server or half_remote_client.", Required = true)] string mode,
            [CliArg("game_id", CliWeb.ConfigGameIdDescription)] string gameId = "",
            [CliArg("launch_timeout_seconds", "Seconds the launch may take before it fails with a timeout; -1 for none.")] int launchTimeoutSeconds = ElympicsLaunchDefaults.LaunchTimeoutSeconds,
            [CliArg("post_match_timeout_seconds", "Seconds to wait after the match ends before returning to boot; -1 for none.")] int postMatchTimeoutSeconds = ElympicsLaunchDefaults.PostMatchTimeoutSeconds,
            [CliArg("save_scenes", "Save modified open scenes instead of refusing (Play mode replaces the open scenes with the boot scene).")] bool saveScenes = false) =>
            CliRun.Run(() =>
            {
                if (launchTimeoutSeconds < -1 || postMatchTimeoutSeconds < -1)
                    throw ElympicsCliErrors.InvalidArgumentException("--launch_timeout_seconds and --post_match_timeout_seconds must be -1 or more.",
                        "pass a number of seconds, or -1 for no timeout.");
                var launchMode = CliArgs.ParseEnum<LaunchMode>(mode, "mode");
                var kind = launchMode switch
                {
                    LaunchMode.HalfRemoteServer => ElympicsHostLaunchKind.HalfRemoteServer,
                    LaunchMode.HalfRemoteClient => ElympicsHostLaunchKind.HalfRemoteClient,
                    _ => ElympicsHostLaunchKind.SinglePlayer,
                };
                var playing = EditorApplication.isPlaying;
                if (playing)
                {
                    var launcher = ElympicsMatchLauncher.Current;
                    if (launcher != null && launcher.State != MatchLauncherState.Idle)
                        throw new ElympicsCliException(ElympicsCliErrors.LauncherBusy, $"The match launcher is {launcher.State}, not Idle.",
                            "wait for the match to end and elympics_get_match_state to report an Idle launcher, or stop Play mode (editor_stop).");
                }
                else
                {
                    var dirty = EditorMatchLaunchQueue.GetDirtyScenes();
                    if (dirty.Length > 0 && !saveScenes)
                        throw new ElympicsCliException(ElympicsCliErrors.UnsavedScenes,
                            $"These open scenes have unsaved changes: {string.Join(", ", dirty.Select(EditorMatchLaunchQueue.DescribeScene))}.",
                            "save them (save_all), or repeat with --save_scenes true to save them before launching.");
                    if (EditorApplication.isCompiling)
                        throw new ElympicsCliException(ElympicsCliErrors.Compiling, "Scripts are compiling.", "wait until recompile_status reports completion, then retry.");
                    CliRun.RequireNoCompileErrors();
                }

                var game = CliGames.ResolveGame(gameId);
                var requestId = "cli-" + Guid.NewGuid().ToString("N")[..12];
                var dataJson = JsonUtility.ToJson(ElympicsLaunchDtos.Create(kind));
                var optionsJson = JsonUtility.ToJson(new ElympicsLaunchOptionsDto
                {
                    requestId = requestId,
                    launchTimeoutSeconds = launchTimeoutSeconds,
                    postMatchTimeoutSeconds = postMatchTimeoutSeconds,
                });
                var modeName = ToSnakeCase(launchMode.ToString());
                EditorLaunchRecord.Start(requestId, modeName, game.GameId);
                if (EditorMatchLaunchQueue.Launch(kind, game.GameId, dataJson, optionsJson, EditorMatchLaunchQueue.UnsavedScenePolicy.Save) is { } refusal)
                {
                    EditorLaunchRecord.Fail(requestId, $"The launch was refused: {refusal}");
                    throw new ElympicsCliException(ElympicsCliErrors.LaunchRefused, $"The launch was refused: {refusal}",
                        "resolve the reason above (compile errors, unsaved untitled scenes), then retry.");
                }

                return new { status = "launch_queued", requestId, mode = modeName, gameId = game.GameId, enteringPlayMode = !playing };
            });

        private static string ToSnakeCase(string name) => string.Concat(name.Select((character, index) =>
            char.IsUpper(character) ? (index > 0 ? "_" : "") + char.ToLowerInvariant(character) : character.ToString()));

        [CliCommand("elympics_get_match_state",
            "Report Play mode, the match launcher, the last elympics_launch outcome (survives the Play mode reload), the live match "
            + "(tick, role, player) and client connection diagnostics (RTT, prediction, reconciliations). Never fails; poll it after elympics_launch.",
            Tags = new[] { "elympics/play" })]
        public static object GetMatchState() => CliRun.Run(() =>
        {
            var launcher = ElympicsMatchLauncher.Current;
            var record = EditorLaunchRecord.Last;
            var elympics = EditorApplication.isPlaying
                ? UnityEngine.Object.FindObjectsByType<ElympicsBase>(FindObjectsSortMode.None).FirstOrDefault(candidate => candidate.isActiveAndEnabled && candidate.Initialized)
                : null;
            var client = elympics as ElympicsClient;
            return new
            {
                status = "ok",
                inPlayMode = EditorApplication.isPlaying,
                willChangePlayMode = EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying,
                EditorApplication.isCompiling,
                launcher = launcher == null
                    ? (object)new { exists = false }
                    : new { exists = true, state = launcher.State.ToString(), isInMatch = launcher.IsCurrentlyInMatch },
                lastLaunch = record == null
                    ? null
                    : new
                    {
                        record.requestId,
                        record.mode,
                        record.gameId,
                        record.result,
                        error = FormatLaunchError(record),
                        message = record.result == EditorLaunchRecord.Ended && !string.IsNullOrEmpty(record.message) ? record.message : null,
                        matchId = string.IsNullOrEmpty(record.matchId) ? null : record.matchId,
                        record.updatedAt,
                    },
                match = elympics == null
                    ? null
                    : new
                    {
                        tick = elympics.Tick,
                        ticksPerSecond = elympics.TicksPerSecond,
                        tickDuration = elympics.TickDuration,
                        isServer = elympics.IsServer,
                        isClient = elympics.IsClient,
                        isBot = elympics.IsBot,
                        player = (int)elympics.Player,
                        scene = elympics.gameObject.scene.path,
                    },
                connection = client == null ? null : DescribeDiagnostics(client.Diagnostics),
                virtualPlayers = TryDescribeVirtualPlayers(),
            };
        });

        private static string? FormatLaunchError(EditorLaunchRecord.Record record)
        {
            if (record.result is not (EditorLaunchRecord.Failed or EditorLaunchRecord.TimedOut)
                || (string.IsNullOrEmpty(record.code) && string.IsNullOrEmpty(record.message)))
                return null;
            var code = record.result == EditorLaunchRecord.TimedOut ? ElympicsCliErrors.LaunchTimeout : ElympicsCliErrors.LaunchFailed;
            return ElympicsCliErrors.Format(code, $"{record.code} {record.message}".Trim(), LaunchFailureFix(record.code));
        }

        private static string LaunchFailureFix(string code) => code switch
        {
            nameof(ElympicsLaunchErrorCode.Timeout) => "check the Console (console --level error) for why the gameplay scene didn't start the match, "
                + "then launch again, with a larger --launch_timeout_seconds if it is just slow.",
            nameof(ElympicsLaunchErrorCode.UnknownGame) => "check the game config and its gameplay scene with elympics_status and elympics_validate_scene.",
            nameof(ElympicsLaunchErrorCode.SceneLoadFailed) => "add the gameplay scene to Build Settings and fix the errors elympics_validate_scene reports.",
            nameof(ElympicsLaunchErrorCode.ConnectionFailed) => "for half-remote, start the half_remote_server first and check the half-remote IP and port in the game config.",
            _ => "read the message and the Console (console --level error), fix the cause and launch again.",
        };

        private static object DescribeDiagnostics(ClientDiagnostics diagnostics) => new
        {
            rttTicks = diagnostics.RttTicks,
            rttMs = diagnostics.RttMs,
            canPredict = diagnostics.CanPredict,
            lastSnapshotTick = diagnostics.LastSnapshotTick,
            ticksAheadOfServer = diagnostics.TicksAheadOfServer,
            reanchoredReconciliations = diagnostics.ReanchoredCount,
            replayedReconciliations = diagnostics.ReplayedCount,
        };

        private static object? TryDescribeVirtualPlayers()
        {
            if (MppmReflection.TryResolve(out _) == null || PackageInfo.FindForPackageName(MppmPackage) == null)
                return null;
            try
            {
                var players = MppmReflection.GetPlayers().Where(player => player.number > 1).ToList();
                return new { active = players.Count(player => player.state != NotLaunched), launched = players.Count(player => player.state == Launched) };
            }
            catch (Exception)
            {
                return null;
            }
        }

        [CliCommand("elympics_get_virtual_players",
            "List Multiplayer Play Mode's players (1 is the main Editor) with their state and tags, and the defines that turn off their automatic half-remote launch.",
            Tags = new[] { "elympics/play" })]
        public static object GetVirtualPlayers() => CliRun.Run(() => new
        {
            status = "ok",
            mppmPackageVersion = PackageInfo.FindForPackageName(MppmPackage)?.version,
            players = MppmReflection.GetPlayers(),
            autoLaunch = new
            {
                defaultHalfRemoteClient = !ScriptingSymbols.MppmAutoLaunchDisabled,
                gameplaySceneLoad = !ScriptingSymbols.LaunchModeSceneLoadDisabled,
            },
            defines = new
            {
                ELYMPICS_DISABLE_MPPM_AUTO_LAUNCH = ScriptingSymbols.MppmAutoLaunchDisabled,
                ELYMPICS_DISABLE_LAUNCH_MODE_SCENE_LOAD = ScriptingSymbols.LaunchModeSceneLoadDisabled,
            },
        });

        [CliCommand("elympics_set_virtual_players",
            "Activate the first --count Multiplayer Play Mode virtual players (0-3) and deactivate the others, optionally replacing their Elympics: tags "
            + "(e.g. Elympics:halfRemoteServer, Elympics:PlayerIndex=1, Elympics:manual). By default virtual players join a half_remote_server launch as clients.",
            Tags = new[] { "elympics/play" })]
        public static object SetVirtualPlayers(
            [CliArg("count", "How many virtual players (Player 2, 3, ...) to activate.", Required = true)] int count,
            [CliArg("tags", "JSON object from player number to its Elympics: tags, e.g. '{\"2\":[\"Elympics:PlayerIndex=1\"]}'. "
                + "Replaces only the Elympics: tags of the listed players.")]
            string tags = "") => CliRun.Run(() =>
        {
            CliRun.RequireNotPlaying("Virtual players can't be changed in Play mode.");
            var tagsByPlayer = ParseTags(tags);
            var players = MppmReflection.GetPlayers();
            var virtualCount = players.Count - 1;
            if (count < 0 || count > virtualCount)
                throw ElympicsCliErrors.InvalidArgumentException($"--count must be from 0 to {virtualCount}.", "pass e.g. --count 1.");
            var highestTagged = tagsByPlayer.Select(entry => entry.number).DefaultIfEmpty(0).Max();
            if (highestTagged > players.Count)
                throw ElympicsCliErrors.InvalidArgumentException($"--tags lists player {highestTagged}, but there are only {players.Count} players.", TagsFix);

            foreach (var (number, playerTags) in tagsByPlayer)
                MppmReflection.ReplaceTags(number, PlayModeArguments.TagPrefix, playerTags);
            for (var number = 2; number <= players.Count; number++)
            {
                var wanted = number - 1 <= count;
                if (wanted != (players[number - 1].state != NotLaunched))
                    MppmReflection.SetActive(number, wanted);
            }

            return new { status = "updated", count, players = MppmReflection.GetPlayers() };
        });

        private static List<(int number, List<string> tags)> ParseTags(string json)
        {
            var parsed = CliArgs.ParseJsonObject(json, "tags", "a JSON object", TagsFix, JObject.Parse);
            if (parsed == null)
                return new List<(int, List<string>)>();

            var result = new List<(int, List<string>)>();
            foreach (var property in parsed.Properties())
            {
                if (!int.TryParse(property.Name, out var number) || number < 2 || property.Value is not JArray array)
                    throw ElympicsCliErrors.InvalidArgumentException($"--tags has an invalid entry \"{property.Name}\".", TagsFix);
                var foreign = array.FirstOrDefault(tag => tag.Type != JTokenType.String || !((string)tag!).StartsWith(PlayModeArguments.TagPrefix, StringComparison.OrdinalIgnoreCase));
                if (foreign != null)
                    throw ElympicsCliErrors.InvalidArgumentException($"The tag {foreign.ToString(Formatting.None)} isn't a string that starts with {PlayModeArguments.TagPrefix}.", TagsFix);
                result.Add((number, array.Select(tag => (string)tag!).ToList()));
            }
            return result;
        }
    }
}
