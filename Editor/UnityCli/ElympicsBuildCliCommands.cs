using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Elympics.Editor.UnityCli
{
    /// <summary>Uploads are irreversible (versions are write-once) and need --confirm true.</summary>
    public static class ElympicsBuildCliCommands
    {
        [CliCommand("elympics_build_server",
            "Build the active game's Linux dedicated server into serverbuild/Engine (the Linux Dedicated Server module must be installed). "
            + "Takes minutes: run it with --detach, or raise the CLI's --timeout.",
            Tags = new[] { "elympics", "build" })]
        public static object BuildServer([CliArg("development", "Make a development build.")] bool development = false) => CliRun.Sync(() =>
        {
            RequireReadyToBuild();
            var game = CliGames.RequireActiveGame(CliGames.RequireConfig());
            var report = BuildTools.BuildServerLinux(development ? BuildOptions.Development : BuildOptions.None);
            ThrowIfFailed(report);
            return new
            {
                status = "built",
                result = report.summary.result.ToString(),
                report.summary.outputPath,
                totalSizeBytes = (long)report.summary.totalSize,
                durationSeconds = report.summary.totalTime.TotalSeconds,
                warnings = report.summary.totalWarnings,
                gameId = game.GameId,
                gameVersion = game.GameVersion,
            };
        });

        [CliCommand("elympics_upload_server",
            "Build the active game's Linux server and upload it as the game version set in its game config (elympics_config_set_version). "
            + "Irreversible: a version name can't be uploaded again. Needs --confirm true. Takes minutes: run it with --detach.",
            Tags = new[] { "elympics", "upload" })]
        public static Task<object> UploadServer(
            [CliArg("confirm", CliWeb.UploadConfirmDescription)] bool confirm = false,
            [CliArg("development", "Upload a development build.")] bool development = false,
            [CliArg("timeout_seconds", CliWeb.UploadTimeoutDescription)] int timeoutSeconds = CliWeb.DefaultUploadTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                CliArgs.RequireConfirm(confirm, "Uploading a server version");
                RequireReadyToBuild();
                var game = CliGames.RequireActiveGame(CliGames.RequireConfig());
                if (!Guid.TryParse(game.GameId, out _))
                    throw ElympicsCliErrors.InvalidArgumentException($"The active game config's GameId '{game.GameId}' is not a GUID.",
                        "set the active game's GameId in the Elympics config to an ID from elympics_games_list.");
                BuildReport report;
                try
                {
                    report = await ElympicsWebIntegration.BuildAndUploadServerUsingCurrentLoginAsync(development ? BuildOptions.Development : BuildOptions.None, ct);
                }
                catch (ElympicsPackFailedException e)
                {
                    throw new ElympicsCliException(ElympicsCliErrors.PackFailed, e.Message, "check that serverbuild/Engine isn't locked by another process and that the disk has space, then retry.");
                }
                ThrowIfFailed(report);
                return new { status = "uploaded", gameId = game.GameId, gameVersion = game.GameVersion, usedCurrentLogin = true, buildResult = report.summary.result.ToString() };
            }, ElympicsCliErrors.UploadFailed);

        [CliCommand("elympics_upload_client_build",
            "Upload a WebGL client build (its root or Build folder) to the Elympics cloud as a client version linked to a server version. "
            + "Irreversible: needs --confirm true. Large builds take minutes: run it with --detach.",
            Tags = new[] { "elympics", "upload" })]
        public static Task<object> UploadClientBuild(
            [CliArg("client_build_path", "The WebGL build's root or its Build folder, inside the project unless --allow_external_path true; absolute or relative to the project.", Required = true)] string clientBuildPath,
            [CliArg("client_game_version", "Client version name: 1-63 Latin letters, digits, dashes and dots.", Required = true)] string clientGameVersion,
            [CliArg("server_game_version", "Server version the client connects to. Defaults to the game config's version.")] string serverGameVersion = "",
            [CliArg("game_id", CliWeb.GameIdDescription)] string gameId = "",
            [CliArg("confirm", CliWeb.UploadConfirmDescription)] bool confirm = false,
            [CliArg("allow_external_path", "Must be true to upload a build outside the project.")] bool allowExternalPath = false,
            [CliArg("timeout_seconds", CliWeb.UploadTimeoutDescription)] int timeoutSeconds = CliWeb.DefaultUploadTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                var resolvedPath = CliArgs.ResolveProjectPath(CliArgs.Require(clientBuildPath, "client_build_path"), "client_build_path", allowExternalPath);
                clientGameVersion = CliArgs.ValidateVersion(clientGameVersion, "client_game_version");
                CliArgs.RequireConfirm(confirm, "Uploading a client build");
                var game = CliGames.ResolveGame(gameId);
                serverGameVersion = string.IsNullOrWhiteSpace(serverGameVersion) ? game.GameVersion : CliArgs.ValidateVersion(serverGameVersion, "server_game_version");
                if (!Directory.Exists(resolvedPath))
                    throw new ElympicsCliException(ElympicsCliErrors.BuildDirNotFound, $"The client build directory {resolvedPath} doesn't exist.",
                        "build the WebGL client first (build --target WebGL), then pass its output folder.");

                int fileCount;
                try
                {
                    fileCount = await ElympicsWebIntegration.UploadClientBuildUsingCurrentLoginAsync(game.GameId, resolvedPath, clientGameVersion, serverGameVersion, ct);
                }
                catch (ElympicsClientBuildFilesException e)
                {
                    throw new ElympicsCliException(ElympicsCliErrors.BuildDirNotFound, e.Message,
                        "pass the root of a complete WebGL build (or its Build folder) with the .data, .wasm, .framework.js, .loader.js and .elympicsmeta.json files.");
                }
                catch (ElympicsUploadInputException e)
                {
                    throw new ElympicsCliException(ElympicsCliErrors.InvalidArgument, e.Message, "fix the argument named in the message, then retry.");
                }
                return new { status = "uploaded", gameId = game.GameId, clientGameVersion, serverGameVersion, fileCount, usedCurrentLogin = true };
            }, ElympicsCliErrors.UploadFailed);

        private static void RequireReadyToBuild()
        {
            CliRun.RequireNotPlaying("Builds can't run in Play mode.");
            if (EditorUtility.scriptCompilationFailed)
                throw new ElympicsCliException(ElympicsCliErrors.CompileErrors, "Scripts have compile errors.", "fix the errors listed by console --level error, then retry.");
        }

        private static void ThrowIfFailed(BuildReport report)
        {
            if (report.summary.result == BuildResult.Succeeded)
                return;
            if (BuildTools.HasMissingModuleError(report))
                throw new ElympicsCliException(ElympicsCliErrors.MissingBuildModule, BuildTools.MissingModuleErrorMessage + ".",
                    "add the modules to this Editor in Unity Hub (Installs > ... > Add modules), restart the Editor, then retry.");
            var errors = report.steps.SelectMany(step => step.messages).Where(message => message.type is LogType.Error or LogType.Exception)
                .Select(message => message.content.Split('\n')[0]).Distinct().Take(5).ToList();
            throw new ElympicsCliException(ElympicsCliErrors.BuildFailed,
                $"The server build {report.summary.result}{(errors.Count > 0 ? ": " + string.Join(" | ", errors) : ".")}",
                "fix the build errors (console --level error has them all), then retry.");
        }
    }
}
