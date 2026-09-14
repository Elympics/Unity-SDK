using System;
using System.IO;
using Unity.Pipeline.Commands;

namespace Elympics.Editor.UnityCli
{
    public static class ElympicsStreamingAssetsCliCommands
    {
        private const string CommandName = "elympics_upload_streaming_assets";

        /// <summary>
        /// Unity CLI equivalent of <see cref="ElympicsWebIntegration.UploadStreamingAssetsInBatchmode"/>.
        /// </summary>
        /// <remarks>
        /// A large bundle can outrun the default HTTP timeout. Raise the timeout
        /// or run the command as a detached job (<c>"job": true</c>) for big uploads.
        /// </remarks>
        [CliCommand(CommandName,
            "Upload a directory of StreamingAssets variants to the Elympics cloud as a new content version. "
            + "Uses the Editor's current login and active game config unless username/password/game_id are given. "
            + "Versions are write-once - a burnt version name cannot be reused, and the upload is not undoable. "
            + "Blocks the Editor main thread for the whole upload, so raise the client timeout "
            + "or run it as a detached job for large bundles.",
            Tags = new[] { "build" })]
        public static object UploadStreamingAssets(
            [CliArg("streaming_assets_path", "Local directory to upload. Absolute, or relative to the project root.", Required = true)] string streamingAssetsPath,
            [CliArg("version_name", "Content version to publish. Write-once - a version name that has already been used (or failed mid-upload) cannot be reused. Only Latin letters, digits, dashes, and dots are allowed.", Required = true)] string versionName,
            [CliArg("game_id", "Target game ID. Defaults to the active Elympics game config's ID.")] string gameId = "",
            [CliArg("username", "Elympics account username. Omit to upload as the currently logged in account. Must be paired with password.")] string username = "",
            [CliArg("password", "Elympics account password. Omit to upload as the currently logged in account. Must be paired with username.")] string password = "",
            [CliArg("layout", "Directory layout: AddressableVariants (default, one Addressables build per variant, generating a manifest file) "
                + "or UnstructuredAssets (every file below the path, unvalidated).")]
            string layout = nameof(StreamingAssetsLayout.AddressableVariants))
        {
            RequireArgument(streamingAssetsPath, "streaming_assets_path");
            RequireArgument(versionName, "version_name");

            var parsedLayout = ParseLayout(layout);
            var resolvedPath = Path.GetFullPath(streamingAssetsPath);
            if (!Directory.Exists(resolvedPath))
                throw new ArgumentException($"StreamingAssets directory not found: {resolvedPath}");

            var hasUsername = !string.IsNullOrWhiteSpace(username);
            var hasPassword = !string.IsNullOrWhiteSpace(password);

            if (hasUsername != hasPassword)
                throw new ArgumentException("--username and --password must be supplied together. Omit both to upload as the currently logged in account.");

            var usedActiveGameConfig = ResolveGameId(ref gameId);

            if (hasUsername)
                ElympicsWebIntegration.UploadStreamingAssetsInBatchmode(username, password, gameId, streamingAssetsPath, versionName, parsedLayout);
            else
                ElympicsWebIntegration.UploadStreamingAssetsUsingCurrentLogin(gameId, streamingAssetsPath, versionName, parsedLayout);

            return new
            {
                status = "uploaded",
                gameId,
                versionName,
                layout = parsedLayout.ToString(),
                streamingAssetsPath,
                usedCurrentLogin = !hasUsername,
                usedActiveGameConfig,
            };
        }

        /// <returns><c>true</c> if the fallback (the active Elympics game config's ID) was taken.</summary>
        private static bool ResolveGameId(ref string gameId)
        {
            if (!string.IsNullOrWhiteSpace(gameId))
                return false;

            var gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
            if (gameConfig == null || string.IsNullOrWhiteSpace(gameConfig.GameId))
                throw new ArgumentException("No active Elympics game config with a game ID was found. Select one in the Elympics config, or pass --game_id.");

            gameId = gameConfig.GameId;
            return true;
        }

        private static void RequireArgument(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Missing required argument --{argumentName}.");
        }

        private static StreamingAssetsLayout ParseLayout(string layout)
        {
            if (string.IsNullOrWhiteSpace(layout))
                return StreamingAssetsLayout.AddressableVariants;

            if (Enum.TryParse<StreamingAssetsLayout>(layout.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(typeof(StreamingAssetsLayout), parsed))
                return parsed;

            throw new ArgumentException($"Unknown layout '{layout}'. Valid values: {string.Join(", ", Enum.GetNames(typeof(StreamingAssetsLayout)))}.");
        }
    }
}
