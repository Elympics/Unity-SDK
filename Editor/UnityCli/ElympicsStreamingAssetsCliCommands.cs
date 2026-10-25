using System.IO;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace Elympics.Editor.UnityCli
{
    public static class ElympicsStreamingAssetsCliCommands
    {
        private const string CommandName = "elympics_upload_streaming_assets";

        [CliCommand(CommandName,
            "Upload a directory of StreamingAssets variants to the Elympics cloud as a new content version, with the Editor's current login and "
            + "the active game config unless --game_id is given. Versions are write-once - a burnt version name cannot be reused, and the upload "
            + "is not undoable, so it needs --confirm true. Large bundles take minutes: run it with --detach.",
            Tags = new[] { "elympics", "upload" })]
        public static Task<object> UploadStreamingAssets(
            [CliArg("streaming_assets_path", "Local directory to upload, inside the project unless --allow_external_path true. Absolute, or relative to the project root.", Required = true)] string streamingAssetsPath,
            [CliArg("version_name", "Content version to publish. Write-once - a version name that has already been used (or failed mid-upload) cannot be reused. "
                + "Only Latin letters, digits, dashes, and dots are allowed.", Required = true)]
            string versionName,
            [CliArg("game_id", "Target game ID. Defaults to the active Elympics game config's ID.")] string gameId = "",
            [CliArg("layout", "Directory layout: AddressableVariants (default, one Addressables build per variant, generating a manifest file) "
                + "or UnstructuredAssets (every file below the path, unvalidated).")]
            string layout = nameof(StreamingAssetsLayout.AddressableVariants),
            [CliArg("confirm", CliWeb.UploadConfirmDescription)] bool confirm = false,
            [CliArg("allow_external_path", "Must be true to upload a directory outside the project.")] bool allowExternalPath = false,
            [CliArg("username", "Removed: credentials are never passed on the command line. Log in in the Editor instead.")] string username = "",
            [CliArg("password", "Removed: credentials are never passed on the command line. Log in in the Editor instead.")] string password = "",
            [CliArg("timeout_seconds", CliWeb.UploadTimeoutDescription)] int timeoutSeconds = CliWeb.DefaultUploadTimeoutSeconds) =>
            CliWeb.RunAsync(timeoutSeconds, async ct =>
            {
                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                    throw new ElympicsCliException(ElympicsCliErrors.CredentialsArgsRemoved,
                        "--username and --password were removed: a password in arguments leaks into shell history, process lists and logs.",
                        "log in once via Tools > Elympics > Manage games in Elympics, then call the command without credentials.");
                streamingAssetsPath = CliArgs.Require(streamingAssetsPath, "streaming_assets_path");
                versionName = CliArgs.ValidateVersion(versionName, "version_name");
                var parsedLayout = ParseLayout(layout);
                var resolvedPath = CliArgs.ResolveProjectPath(streamingAssetsPath, "streaming_assets_path", allowExternalPath);
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
                catch (ElympicsUploadInputException e)
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

        internal static StreamingAssetsLayout ParseLayout(string layout) =>
            string.IsNullOrWhiteSpace(layout) ? StreamingAssetsLayout.AddressableVariants : CliArgs.ParseEnum<StreamingAssetsLayout>(layout, "layout");
    }
}
