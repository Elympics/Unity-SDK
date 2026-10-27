using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Elympics.Core.Logger;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor;
using Elympics.Editor.Communication;
using Elympics.Editor.Models.UsageStatistics;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using UsageStatisticsRoutes = Elympics.Editor.Models.UsageStatistics.Routes;

namespace Elympics.Editor
{
    internal sealed class ElympicsPackFailedException : ElympicsException
    {
        public ElympicsPackFailedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}

namespace Elympics
{
    public static class ElympicsWebIntegration
    {
        private const string PackFileName = "pack.zip";
        private const string DirectoryNameToUpload = "Games";
        private const string EngineSubdirectory = "Engine";
        private const int StopEventTimeoutSeconds = 1;

        internal static event Action GameUploadedToTheCloud;

        public static void Login() => RunAndLogErrors(() => LoginAsync(ElympicsConfig.Username, ElympicsConfig.Password));

        public static void Logout()
        {
            var loginStore = ElympicsEditorApi.LoginStore;
            loginStore.IsLogin = false;
            ElympicsConfig.Password = string.Empty;
            loginStore.RefreshToken = string.Empty;
            loginStore.AuthToken = string.Empty;
            ElympicsConfig.Username = string.Empty;
        }

        public static bool IsConnectedToElympics() => IsLoggedIn();

        private static bool IsLoggedIn()
        {
            if (ElympicsEditorApi.LoginStore.IsLogin)
                return true;
            ElympicsLogger.LogError("Not logged in to Elympics cloud. Check your Internet connection, configured credentials and Elympics endpoints.");
            return false;
        }

        [Serializable]
        public class RefreshedTokensResponseModel
        {
            // ReSharper disable InconsistentNaming
            public string AuthToken;
            public string RefreshToken;
            // ReSharper restore InconsistentNaming
        }

        [Serializable]
        public class ElympicsEndpointsModel
        {
            // ReSharper disable InconsistentNaming
            public string Lobby;
            public string GameServers;
            // ReSharper restore InconsistentNaming
        }

        [Serializable]
        public class GameResponseModel
        {
            // ReSharper disable InconsistentNaming
            public string Id;
            public string Name;
            // ReSharper restore InconsistentNaming
        }

        [Serializable]
        public class GameVersionResponseModel
        {
            // ReSharper disable InconsistentNaming
            public string Id;
            public string Version;
            public bool Uploaded;
            public string UploadedTime;
            public bool Blocked;
            public bool DebugMode;
            public bool DebugModeWithBots;
            // ReSharper restore InconsistentNaming
        }

        [Serializable]
        public class GameVersionsResponseModel
        {
            // ReSharper disable InconsistentNaming
            public string GameName;
            public List<GameVersionResponseModel> Versions;
            // ReSharper restore InconsistentNaming
        }

        private static async UniTask LoginAsync(string username, string password, CancellationToken ct = default)
        {
            ElympicsEditorApi.RequireTrustedApiEndpoint();
            ElympicsLogger.LogInfo($"Logging in as {username}");
            var userName = await ElympicsEditorApi.LoginAsync(username, password, ct: ct);
            ElympicsLogger.LogInfo($"Logged to ElympicsWeb as {userName}.");
        }

        internal static void GetAvailableRegionsForGameId(string gameId, Action<List<RegionResponseModel>> updateProperty, Action onFailure)
        {
            ElympicsLogger.LogInfo("Getting available regions...");
            RunAndLogErrors(async () =>
            {
                List<RegionResponseModel> regions;
                try
                {
                    regions = await ElympicsEditorApi.GetRegionsAsync(gameId);
                }
                catch
                {
                    onFailure?.Invoke();
                    throw;
                }
                updateProperty?.Invoke(regions);
            });
        }

        private static CancellationTokenSource gameVersionsCancellation;

        public static void GetGameVersions(Action<GameVersionsResponseModel> updateProperty, bool silent = false)
        {
            var gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
            if (gameConfig == null)
                return;
            GetGameVersionsForGameId(gameConfig.gameId, updateProperty, silent);
        }

        /// <remarks>A new call cancels the previous one.</remarks>
        public static void GetGameVersionsForGameId(string gameId, Action<GameVersionsResponseModel> updateProperty, bool silent = false)
        {
            gameVersionsCancellation?.Cancel();
            var cancellation = gameVersionsCancellation = new CancellationTokenSource();
            RunAndLogErrors(async () =>
            {
                var gameVersions = await ElympicsEditorApi.GetGameVersionsAsync(gameId, ct: cancellation.Token);
                updateProperty?.Invoke(gameVersions);
            }, silent);
        }

        public static void GetGames(Action<List<GameResponseModel>> updateProperty)
        {
            ElympicsLogger.LogInfo("Getting available games...");
            RunAndLogErrors(async () =>
            {
                var games = await ElympicsEditorApi.GetGamesAsync();
                ElympicsLogger.LogInfo("Received available games.");
                updateProperty.Invoke(games);
            });
        }

        private static void RunAndLogErrors(Func<UniTask> action, bool silent = false) => RunAndLogErrorsAsync(action, silent).Forget();

        private static async UniTask RunAndLogErrorsAsync(Func<UniTask> action, bool silent)
        {
            try
            {
                await action();
            }
            catch (OperationCanceledException)
            { }
            catch (Exception e)
            {
                if (!silent)
                    ElympicsLogger.LogError(e.Message);
            }
        }

        public static void GetElympicsEndpoints(Action<ElympicsEndpointsModel> updateProperty) =>
            RunAndLogErrors(async () =>
            {
                var endpoints = await ElympicsEditorApi.GetEndpointsAsync();
                updateProperty.Invoke(endpoints);
                ElympicsLogger.LogInfo($"Elympics endpoints have been set to: {endpoints.Lobby}, {endpoints.GameServers}.");
            });

        public static void PostStartEvent()
        {
            var gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
            PostTelemetryEvent(UsageStatisticsRoutes.Start, new StartRequest { gameId = gameConfig != null ? gameConfig.GameId : string.Empty });
        }

        public static void PostPlayEvent(string mode)
        {
            var gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
            PostTelemetryEvent(UsageStatisticsRoutes.Play,
                new PlayRequest
                {
                    gameId = gameConfig?.GameId ?? string.Empty,
                    mode = mode ?? string.Empty,
                });
        }

        /// <remarks>Blocks the Editor for up to a second, so that the request leaves before the Editor quits.</remarks>
        public static void PostStopEvent()
        {
            var gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
            var requestBody = new StopRequest { gameId = gameConfig != null ? gameConfig.GameId : string.Empty };
            ElympicsEditorApi.RunBlocking(ct => ElympicsEditorApi.PostUsageEventAsync(UsageStatisticsRoutes.Stop, requestBody, false, StopEventTimeoutSeconds, ct));
        }

        private static void PostTelemetryEvent(string route, object requestBody) =>
            RunAndLogErrors(() => ElympicsEditorApi.PostUsageEventAsync(route, requestBody, true), true);

        public static void BuildAndUploadGame(Action<bool> completed = null) => BuildAndUploadGameAsync(completed).Forget();

        private static async UniTask BuildAndUploadGameAsync(Action<bool> completed)
        {
            const string title = "Uploading to Elympics cloud";
            var succeeded = false;
            try
            {
                if (!ElympicsEditorApi.LoginStore.IsLogin)
                {
                    ElympicsLogger.LogError("You must be logged in Elympics to upload games!");
                    return;
                }

                try
                {
                    await ElympicsEditorApi.EnsureLoggedInAsync();
                }
                catch (ElympicsEditorApiException e)
                {
                    ElympicsLogger.LogError(e.Message);
                    _ = EditorUtility.DisplayDialog(title, $"Auth failed:\n{e.Message}", "OK");
                    return;
                }

                var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
                if (currentGameConfig is null)
                {
                    const string message = "Current game config is null";
                    _ = EditorUtility.DisplayDialog(title, $"Upload failed:\n{message}", "OK");
                    ElympicsLogger.LogError(message);
                    return;
                }

                var report = await BuildPackAndUploadServerAsync(currentGameConfig, BuildOptions.None, (step, progress) => EditorUtility.DisplayProgressBar(title, step, progress), default);
                succeeded = report.summary.result == BuildResult.Succeeded;
            }
            catch (OperationCanceledException)
            { }
            catch (Exception e)
            {
                _ = EditorUtility.DisplayDialog(title, $"Upload failed: \n{e.Message}", "OK");
                ElympicsLogger.LogError(e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                completed?.Invoke(succeeded);
            }
        }

        private static async UniTask<BuildReport> BuildPackAndUploadServerAsync(ElympicsGameConfig gameConfig, BuildOptions options, Action<string, float> progress, CancellationToken ct)
        {
            var buildReport = BuildTools.BuildServerLinux(options);
            if (buildReport.summary.result != BuildResult.Succeeded)
                return buildReport;

            await PackAndUploadServerAsync(gameConfig, progress, null, ct);
            return buildReport;
        }

        /// <summary>Packs and uploads the server already built into <see cref="BuildTools.EnginePath"/>, using the current login.</summary>
        internal static async UniTask PackAndUploadServerUsingCurrentLoginAsync(int? timeoutSeconds, CancellationToken ct)
        {
            var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig() ?? throw new ElympicsException("Current game config is null");
            await ElympicsEditorApi.EnsureLoggedInAsync(0, timeoutSeconds, ct);
            await PackAndUploadServerAsync(currentGameConfig, null, timeoutSeconds, ct);
        }

        private static async UniTask PackAndUploadServerAsync(ElympicsGameConfig gameConfig, Action<string, float> progress, int? timeoutSeconds, CancellationToken ct)
        {
            progress?.Invoke("Packing engine", 0.2f);
            var enginePath = Pack(gameConfig.GameId, gameConfig.GameVersion, BuildTools.EnginePath, EngineSubdirectory);

            progress?.Invoke("Uploading...", 0.8f);
            await ElympicsEditorApi.UploadServerAsync(gameConfig.GameId, gameConfig.GameVersion, enginePath, timeoutSeconds, ct);
            ElympicsLogger.LogInfo($"Uploaded game {gameConfig.GameName} with version {gameConfig.GameVersion}.");
            GameUploadedToTheCloud?.Invoke();
        }

        public static void UploadClientBuild(string clientBuildPath, string gameId, string clientGameVersion, string serverGameVersion)
        {
            const string title = "Uploading client build to Elympics cloud";
            RunUploadWithProgressBar(title, async progress =>
            {
                _ = await ElympicsEditorApi.UploadClientBuildAsync(gameId, clientBuildPath, clientGameVersion, serverGameVersion, progress);
                ElympicsLogger.LogInfo("Client build uploaded successfully.");
            });
        }

        public static void UploadStreamingAssets(string gameId, string streamingAssetsPath, string version, StreamingAssetsLayout layout)
        {
            const string title = "Uploading StreamingAssets to Elympics cloud";
            RunUploadWithProgressBar(title, async progress =>
                _ = await ElympicsEditorApi.UploadStreamingAssetsAsync(gameId, streamingAssetsPath, version, layout, progress));
        }

        private static void RunUploadWithProgressBar(string title, Func<Action<string, float>, UniTask> upload) => RunUploadWithProgressBarAsync(title, upload).Forget();

        private static async UniTask RunUploadWithProgressBarAsync(string title, Func<Action<string, float>, UniTask> upload)
        {
            EditorUtility.DisplayProgressBar(title, "Gathering files", 0f);
            try
            {
                await upload((fileName, progress) => EditorUtility.DisplayProgressBar(title, $"Uploading file '{fileName}'", 0.1f + progress * 0.9f));
            }
            catch (Exception e)
            {
                if (e is not (ElympicsException or IOException))
                    ElympicsLogger.LogException(e);
                FailUpload(title, e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // Both upload methods are async so an invalid game ID faults the task instead of throwing synchronously.
        [PublicAPI]
        public static async UniTask<int> UploadStreamingAssetsUsingCurrentLoginAsync(string gameId, string streamingAssetsPath, string version, StreamingAssetsLayout layout,
            int? timeoutSeconds = null, CancellationToken ct = default)
        {
            ValidateGameId(gameId);
            return await ElympicsEditorApi.UploadStreamingAssetsAsync(gameId, streamingAssetsPath, version, layout, null, timeoutSeconds, ct);
        }

        [PublicAPI]
        public static async UniTask<int> UploadClientBuildUsingCurrentLoginAsync(string gameId, string clientBuildPath, string clientGameVersion, string serverGameVersion,
            int? timeoutSeconds = null, CancellationToken ct = default)
        {
            ValidateGameId(gameId);
            return await ElympicsEditorApi.UploadClientBuildAsync(gameId, clientBuildPath, clientGameVersion, serverGameVersion, null, timeoutSeconds, ct);
        }

        /// <returns>The build report. A failed build is returned without uploading.</returns>
        [PublicAPI]
        public static async UniTask<BuildReport> BuildAndUploadServerUsingCurrentLoginAsync(BuildOptions additionalOptions = BuildOptions.None, CancellationToken ct = default)
        {
            var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig() ?? throw new ElympicsException("Current game config is null");
            await ElympicsEditorApi.EnsureLoggedInAsync(0, ct: ct);
            return await BuildPackAndUploadServerAsync(currentGameConfig, additionalOptions, null, ct);
        }

        [PublicAPI]
        public static void UploadStreamingAssetsInBatchmode(
            string username,
            string password,
            string gameId,
            string streamingAssetsPath,
            string version,
            StreamingAssetsLayout layout)
        {
            ValidateGameId(gameId);
            LoginAsDeveloper(username, password);
            UploadStreamingAssetsUsingCurrentLogin(gameId, streamingAssetsPath, version, layout);
        }

        [PublicAPI]
        public static void UploadStreamingAssetsUsingCurrentLogin(string gameId, string streamingAssetsPath, string version, StreamingAssetsLayout layout)
        {
            ValidateGameId(gameId);
            _ = ElympicsEditorApi.RunBlocking(ct => ElympicsEditorApi.UploadStreamingAssetsAsync(gameId, streamingAssetsPath, version, layout, null, ct: ct));
        }

        // Play-mode code (e.g. the PlayPad extension handshake) sends the stored token as is,
        // so it has to be valid, with a margin for the domain reload and scene load.
        private const long PlayModeTokenExpirationMarginSeconds = 60;

        [InitializeOnLoadMethod]
        private static void RefreshAuthTokenOnEnteringPlayMode() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode || !ElympicsEditorApi.LoginStore.IsLogin)
                return;
            try
            {
                ElympicsEditorApi.RunBlocking(ct => ElympicsEditorApi.EnsureLoggedInAsync(PlayModeTokenExpirationMarginSeconds, ct: ct));
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
        }

        private static void FailUpload(string title, string error)
        {
            EditorUtility.ClearProgressBar();
            ElympicsLogger.LogError(error);
            if (!Application.isBatchMode)
                _ = EditorUtility.DisplayDialog(title, error, "OK");
        }

        [PublicAPI]
        public static void UploadClientBuildInBatchmode(
            string username,
            string password,
            string clientBuildPath,
            string gameId,
            string clientGameVersion,
            string serverGameVersion)
        {
            ValidateGameId(gameId);
            LoginAsDeveloper(username, password);
            _ = ElympicsEditorApi.RunBlocking(ct => ElympicsEditorApi.UploadClientBuildAsync(gameId, clientBuildPath, clientGameVersion, serverGameVersion, null, ct: ct));
            ElympicsLogger.LogInfo("Client build uploaded successfully.");
        }

        [UsedImplicitly]
        public static void BuildAndUploadServerInBatchmode(string username, string password, BuildOptions additionalOptions = BuildOptions.None)
        {
            var result = BuildAndUploadServerInBatchmodeWithBuildReport(username, password, additionalOptions);
            if (result.summary.result != BuildResult.Succeeded)
            {
                foreach (var reportStep in result.steps)
                    foreach (var error in reportStep.messages.Where(m => m.type is LogType.Error or LogType.Exception))
                        Debug.Log($"Found an {error.type} in build step {reportStep.name}:\n {error.content}");

                throw new ElympicsException("Build failed");
            }
        }

        [PublicAPI]
        public static BuildReport BuildAndUploadServerInBatchmodeWithBuildReport(string username, string password, BuildOptions additionalOptions = BuildOptions.None)
        {
            LoginAsDeveloper(username, password);
            var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig() ?? throw new ElympicsException("Current game config is null");
            return ElympicsEditorApi.RunBlocking(ct => BuildPackAndUploadServerAsync(currentGameConfig, additionalOptions, null, ct));
        }

        private static void ValidateGameId(string gameId)
        {
            if (!Guid.TryParse(gameId, out _))
                throw new ArgumentException($"Game ID '{gameId}' is not a valid GUID. Pass the ID of an Elympics game.", nameof(gameId));
        }

        private static void LoginAsDeveloper(string username, string password)
        {
            _ = ElympicsConfig.LoadCurrentElympicsGameConfig() ?? throw new ElympicsException("No Elympics game config found. Configure your game before trying to build a server.");
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("An Elympics developer account username is required for batch-mode uploads.", nameof(username));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("An Elympics developer account password is required for batch-mode uploads.", nameof(password));

            ElympicsEditorApi.RunBlocking(ct => LoginAsync(username, password, ct));
        }

        private static string Pack(string gameId, string gameVersion, string buildPath, string targetSubdirectory)
        {
            var destinationDirectoryPath = Path.Combine(DirectoryNameToUpload, gameId, gameVersion, targetSubdirectory);
            var destinationFilePath = Path.Combine(destinationDirectoryPath, PackFileName);
            try
            {
                ElympicsLogger.LogInfo($"Trying to pack {targetSubdirectory}...");
                _ = Directory.CreateDirectory(destinationDirectoryPath);
                if (File.Exists(destinationFilePath))
                    File.Delete(destinationFilePath);
                _ = Directory.CreateDirectory(buildPath);
                CreateZipFromDirectory(buildPath, destinationFilePath);
            }
            catch (Exception e)
            {
                throw new ElympicsPackFailedException($"Packing {targetSubdirectory} into {destinationFilePath} failed: {e.Message}", e);
            }
            return destinationFilePath;
        }

        /// <summary>Like <c>ZipFile.CreateFromDirectory(source, destination, CompressionLevel.Optimal, false)</c>, which needs
        /// the System.IO.Compression.FileSystem facade (missing on CoreCLR and from generated csprojs).</summary>
        internal static void CreateZipFromDirectory(string sourceDirectory, string destinationFile)
        {
            var root = Path.GetFullPath(sourceDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            using var archive = new ZipArchive(File.Create(destinationFile), ZipArchiveMode.Create);
            foreach (var info in new DirectoryInfo(root).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            {
                var name = info.FullName[(root.Length + 1)..].Replace('\\', '/');
                if (info is DirectoryInfo directory)
                {
                    if (!directory.EnumerateFileSystemInfos().Any())
                        _ = archive.CreateEntry(name + "/");
                    continue;
                }

                var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                entry.LastWriteTime = ClampToZipTimestamp(info.LastWriteTime);
                using var input = ((FileInfo)info).OpenRead();
                using var output = entry.Open();
                input.CopyTo(output);
            }
        }

        private static readonly DateTime MinZipTimestamp = new(1980, 1, 1);
        private static readonly DateTime MaxZipTimestamp = new(2107, 12, 31, 23, 59, 58);

        // Zip entries store DOS dates (1980-2107), and ZipArchiveEntry.LastWriteTime throws outside of that range.
        private static DateTime ClampToZipTimestamp(DateTime time) =>
            time < MinZipTimestamp ? MinZipTimestamp : time > MaxZipTimestamp ? MaxZipTimestamp : time;
    }
}
