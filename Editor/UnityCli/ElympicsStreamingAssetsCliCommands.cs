using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace Elympics.Editor.UnityCli
{
    public static class ElympicsStreamingAssetsCliCommands
    {
        private const string CommandName = "elympics_upload_streaming_assets";

        /// <summary>
        /// Unity CLI equivalent of <see cref="ElympicsWebIntegration.UploadStreamingAssetsUsingCurrentLoginAsync"/>.
        /// </summary>
        [CliCommand(CommandName,
            "Upload a directory of StreamingAssets variants to the Elympics cloud as a new content version, with the Editor's current login and "
            + "the active game config unless --game_id is given. Versions are write-once - a burnt version name cannot be reused, and the upload "
            + "is not undoable, so it needs --confirm true. Large bundles take minutes: run it with --detach.",
            Tags = new[] { "elympics", "upload" })]
        public static Task<object> UploadStreamingAssets(
            [CliArg("streaming_assets_path", "Local directory to upload. Absolute, or relative to the project root.", Required = true)] string streamingAssetsPath,
            [CliArg("version_name", "Content version to publish. Write-once - a version name that has already been used (or failed mid-upload) cannot be reused. "
                + "Only Latin letters, digits, dashes, and dots are allowed.", Required = true)]
            string versionName,
            [CliArg("game_id", "Target game ID. Defaults to the active Elympics game config's ID.")] string gameId = "",
            [CliArg("layout", "Directory layout: AddressableVariants (default, one Addressables build per variant, generating a manifest file) "
                + "or UnstructuredAssets (every file below the path, unvalidated).")]
            string layout = nameof(StreamingAssetsLayout.AddressableVariants),
            [CliArg("confirm", "Must be true: the upload can't be undone.")] bool confirm = false,
            [CliArg("username", "Removed: credentials are never passed on the command line. Log in in the Editor instead.")] string username = "",
            [CliArg("password", "Removed: credentials are never passed on the command line. Log in in the Editor instead.")] string password = "",
            [CliArg("timeout_seconds", "Seconds to wait for the whole upload.")] int timeoutSeconds = 1800) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                    throw new ElympicsCliException(ElympicsCliErrors.CredentialsArgsRemoved,
                        "--username and --password were removed: a password in arguments leaks into shell history, process lists and logs.",
                        "log in once via Tools > Elympics > Manage games in Elympics, then call the command without credentials.");
                streamingAssetsPath = CliArgs.Require(streamingAssetsPath, "streaming_assets_path");
                versionName = CliArgs.ValidateVersion(versionName, "version_name");
                var parsedLayout = ParseLayout(layout);
                var resolvedPath = Path.GetFullPath(streamingAssetsPath);
                if (!Directory.Exists(resolvedPath))
                    throw new ElympicsCliException(ElympicsCliErrors.BuildDirNotFound, $"The StreamingAssets directory {resolvedPath} doesn't exist.",
                        "build the Addressables content first, then pass its output folder.");
                CliArgs.RequireConfirm(confirm, "Uploading a StreamingAssets version");
                var usedActiveGameConfig = string.IsNullOrWhiteSpace(gameId);
                var resolvedGameId = CliGames.ResolveGameId(gameId);

                int fileCount;
                try
                {
                    fileCount = await ElympicsWebIntegration.UploadStreamingAssetsUsingCurrentLoginAsync(resolvedGameId, resolvedPath, versionName, parsedLayout, ct);
                }
                catch (ElympicsException e) when (e is not Communication.ElympicsEditorApiException)
                {
                    throw new ElympicsCliException(ElympicsCliErrors.InvalidArgument, e.Message,
                        "fix the directory layout described above (or pass --layout UnstructuredAssets to upload it as-is), then retry with a new --version_name.");
                }

                return new
                {
                    status = "uploaded",
                    gameId = resolvedGameId,
                    versionName,
                    layout = parsedLayout.ToString(),
                    streamingAssetsPath = resolvedPath,
                    fileCount,
                    usedActiveGameConfig,
                };
            }, ElympicsCliErrors.UploadFailed);

        private static StreamingAssetsLayout ParseLayout(string layout)
        {
            if (string.IsNullOrWhiteSpace(layout))
                return StreamingAssetsLayout.AddressableVariants;

            if (Enum.TryParse<StreamingAssetsLayout>(layout.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(typeof(StreamingAssetsLayout), parsed))
                return parsed;

            throw ElympicsCliErrors.InvalidArgumentException($"Unknown --layout '{layout}'.",
                $"use one of: {string.Join(", ", Enum.GetNames(typeof(StreamingAssetsLayout)))}.");
        }
    }
}
