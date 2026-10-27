#nullable enable

using System;
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
            Tags = new[] { "elympics/build" })]
        public static object BuildServer([CliArg("development", "Make a development build.")] bool development = false) => CliRun.Run(() =>
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
            "Build the active game's Linux server and upload it as the game version set in its game config (elympics_set_config_game_version). "
            + "Irreversible: a version name can't be uploaded again. Needs --confirm true. Takes minutes: run it with --detach.",
            Tags = new[] { "elympics/build" })]
        public static Task<object> UploadServer(
            [CliArg("confirm", CliWeb.UploadConfirmDescription)] bool confirm = false,
            [CliArg("development", "Upload a development build.")] bool development = false,
            [CliArg("timeout_seconds", CliWeb.UploadTimeoutDescription)] int timeoutSeconds = CliWeb.DefaultUploadTimeoutSeconds) =>
            CliRun.RunAsync(async () =>
            {
                CliWeb.RequirePositiveTimeout(timeoutSeconds);
                CliArgs.RequireConfirm(confirm, "Uploading a server version");
                RequireReadyToBuild();
                var game = CliGames.RequireActiveGame(CliGames.RequireConfig());
                if (!Guid.TryParse(game.GameId, out _))
                    throw ElympicsCliErrors.InvalidArgumentException($"The active game config's GameId '{game.GameId}' is not a GUID.",
                        "set the active game's GameId in the Elympics config to an ID from elympics_list_games.");
                await CliWeb.EnsureLoggedInAsync(ElympicsCliErrors.UploadFailed);
                var report = BuildTools.BuildServerLinux(development ? BuildOptions.Development : BuildOptions.None);
                ThrowIfFailed(report);
                return await CliWeb.RunAsync(timeoutSeconds, async ct =>
                {
                    try
                    {
                        await ElympicsWebIntegration.PackAndUploadServerUsingCurrentLoginAsync(timeoutSeconds, ct);
                    }
                    catch (ElympicsPackFailedException e)
                    {
                        throw new ElympicsCliException(ElympicsCliErrors.PackFailed, e.Message, "check that serverbuild/Engine isn't locked by another process and that the disk has space, then retry.");
                    }
                    return new { status = "uploaded", gameId = game.GameId, gameVersion = game.GameVersion, usedCurrentLogin = true, buildResult = report.summary.result.ToString() };
                }, ElympicsCliErrors.UploadFailed);
            });

        private static void RequireReadyToBuild()
        {
            CliRun.RequireNotPlaying("Builds can't run in Play mode.");
            CliRun.RequireNoCompileErrors();
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
