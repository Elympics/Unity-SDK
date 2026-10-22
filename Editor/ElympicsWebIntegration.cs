using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elympics.Core.Logger;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor;
using Elympics.Editor.Communication;
using Elympics.Editor.Models.UsageStatistics;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;
using AuthRoutes = ElympicsApiModels.ApiModels.Auth.Routes;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using GamesRoutes = ElympicsApiModels.ApiModels.Games.Routes;
using Routes = ElympicsApiModels.ApiModels.Users.Routes;
using UsageStatisticsRoutes = Elympics.Editor.Models.UsageStatistics.Routes;

namespace Elympics
{
    public static class ElympicsWebIntegration
    {
        private const string PackFileName = "pack.zip";
        private const string DirectoryNameToUpload = "Games";
        private const string EngineSubdirectory = "Engine";

        private static string ElympicsWebEndpoint => ElympicsConfig.Load()?.ElympicsApiEndpoint;

        private static ElympicsConfig Config => ElympicsConfig.Load();

        private static string RefreshEndpoint => GetCombinedUrl(ElympicsWebEndpoint, AuthRoutes.BaseRoute, AuthRoutes.RefreshRoute);

        internal static event Action GameUploadedToTheCloud;

        public static void Login() => _ = Login(ElympicsConfig.Username, ElympicsConfig.Password, LoginHandler);

        public static void Logout()
        {
            SetAsLoggedOut();
            ElympicsConfig.Password = string.Empty;
            ElympicsConfig.RefreshToken = string.Empty;
            ElympicsConfig.AuthToken = string.Empty;
            ElympicsConfig.Username = string.Empty;
        }

        private static void SetAsLoggedOut() => ElympicsConfig.IsLogin = false;

        public static bool IsConnectedToElympics() => IsLoggedIn();

        private static bool IsLoggedIn()
        {
            if (ElympicsConfig.IsLogin)
                return true;
            ElympicsLogger.LogError("Not logged in to Elympics cloud. " + "Check your Internet connection, configured credentials and Elympics endpoints.");
            return false;
        }

        [Serializable]
        private class LoginModel
        {
            // ReSharper disable InconsistentNaming
            public string UserName;
            public string Password;
            // ReSharper restore InconsistentNaming
        }

        [Serializable]
        private class LoggedInTokenResponseModel
        {
            // ReSharper disable InconsistentNaming
            public string UserName;
            public string AuthToken;
            public string RefreshToken;
            // ReSharper restore InconsistentNaming
        }

        [Serializable]
        private class TokenRefreshingRequestModel
        {
            // ReSharper disable InconsistentNaming
            public string RefreshToken;
            // ReSharper restore InconsistentNaming
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




        private static UnityWebRequestAsyncOperation Login(string username, string password, Action<UnityWebRequest> completed = null)
        {
            ElympicsLogger.LogInfo($"Logging in as {username}");

            var model = new LoginModel
            {
                UserName = username,
                Password = password,
            };

            var uri = GetCombinedUrl(ElympicsWebEndpoint, AuthRoutes.BaseRoute, AuthRoutes.LoginRoute);

            return ElympicsEditorWebClient.SendJsonPostRequestApi(uri, model, completed, false);
        }

        private static void LoginHandler(UnityWebRequest webRequest)
        {
            ElympicsLogger.LogInfo($"Received authentication token.\nResponse code: {webRequest.responseCode}.");
            if (TryDeserializeResponse(webRequest, "Login", out LoggedInTokenResponseModel responseModel))
            {
                try
                {
                    var authToken = responseModel.AuthToken;

                    ElympicsLogger.LogInfo($"Logged to ElympicsWeb as {responseModel.UserName}.");
                    ElympicsConfig.AuthToken = authToken;
                    ElympicsConfig.AuthTokenExp = ElympicsEditorApi.ReadTokenExpiration(authToken).ToString();
                    ElympicsConfig.RefreshToken = responseModel.RefreshToken;
                    ElympicsConfig.IsLogin = true;
                }
                catch (Exception e)
                {
                    ElympicsLogger.LogException(e);
                }
            }
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
                return; // TODO: error handling ~dsygocki 2025-07-10
            GetGameVersionsForGameId(gameConfig.gameId, updateProperty, silent);
        }

        /// <remarks>A new call cancels the previous one. Nothing is called back on failure.</remarks>
        public static void GetGameVersionsForGameId(string gameId, Action<GameVersionsResponseModel> updateProperty, bool silent = false)
        {
            gameVersionsCancellation?.Cancel();
            var cancellation = gameVersionsCancellation = new CancellationTokenSource();
            RunAndLogErrors(async () =>
            {
                var gameVersions = await ElympicsEditorApi.GetGameVersionsAsync(gameId, cancellation.Token);
                updateProperty?.Invoke(gameVersions);
            }, silent);
        }

        /// <remarks>Nothing is called back on failure.</remarks>
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

        public static void GetElympicsEndpoints(Action<ElympicsEndpointsModel> updateProperty)
        {
            CheckAuthTokenAndRefreshIfNeeded(OnContinuation);

            void OnContinuation(bool success)
            {
                if (!success)
                    return;

                var uri = GetCombinedUrl(ElympicsWebEndpoint, Routes.BaseRoute, Routes.EndpointRoutes);
                _ = ElympicsEditorWebClient.SendJsonGetRequestApi(uri, OnCompleted);

                void OnCompleted(UnityWebRequest webRequest)
                {
                    if (TryDeserializeResponse(webRequest, "Get Elympics Endpoints", out ElympicsEndpointsModel endpoints))
                    {
                        updateProperty.Invoke(endpoints);
                        ElympicsLogger.LogInfo($"Elympics endpoints have been set to: {endpoints.Lobby}, {endpoints.GameServers}.");
                    }
                }
            }
        }

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
                    gameId = gameConfig?.GameId,
                    mode = mode,
                });
        }

        public static void PostStopEvent()
        {
            var gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
            var requestBody = new StopRequest { gameId = gameConfig != null ? gameConfig.GameId : string.Empty };
            var uri = GetCombinedUrl(ElympicsWebEndpoint, UsageStatisticsRoutes.Base, UsageStatisticsRoutes.Stop);
            var asyncOperation = ElympicsEditorWebClient.SendJsonPostRequestApi(uri, requestBody, auth: ElympicsConfig.IsLogin);
            while (asyncOperation.isDone)
                ;
        }

        private static void PostTelemetryEvent(string pathSegment, object requestBody)
        {
            try
            {
                CheckAuthTokenAndRefreshIfNeeded(_ => OnContinuation());
            }
            catch (ElympicsException)
            {
                OnContinuation();
            }

            void OnContinuation()
            {
                var uri = GetCombinedUrl(ElympicsWebEndpoint, UsageStatisticsRoutes.Base, pathSegment);
                _ = ElympicsEditorWebClient.SendJsonPostRequestApi(uri, requestBody, null, ElympicsConfig.IsLogin);
            }
        }

        /// <summary>Build and upload the game server.</summary>
        public static void BuildAndUploadGame(Action<UnityWebRequest> completed = null)
        {
            CheckAuthTokenAndRefreshIfNeeded(OnContinuation);

            void OnContinuation(bool success)
            {
                const string title = "Uploading to Elympics cloud";

                if (!success)
                {
                    EditorUtility.ClearProgressBar();
                    _ = EditorUtility.DisplayDialog(title, "Auth failed", "Check login state");
                    return;
                }

                if (BuildTools.BuildServerLinux(BuildOptions.None).summary.result != BuildResult.Succeeded)
                    return;

                var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
                if (currentGameConfig is null)
                {
                    const string message = "Current game config is null";
                    _ = EditorUtility.DisplayDialog(title, $"Upload failed:\n{message}", "OK");
                    ElympicsLogger.LogError(message);
                    return;
                }

                string enginePath;
                var waitingForContinuation = false;
                try
                {
                    EditorUtility.DisplayProgressBar(title, "", 0f);
                    if (!ElympicsConfig.IsLogin)
                    {
                        ElympicsLogger.LogError("You must be logged in Elympics to upload games!");
                        return;
                    }

                    EditorUtility.DisplayProgressBar(title, "Packing engine", 0.2f);
                    if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.EnginePath, EngineSubdirectory, out enginePath))
                        return;

                    EditorUtility.DisplayProgressBar(title, "Uploading...", 0.8f);
                    waitingForContinuation = true;
                }
                finally
                {
                    if (!waitingForContinuation)
                        EditorUtility.ClearProgressBar();
                }

                var url = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute, currentGameConfig.GameId, GamesRoutes.GameVersionsRoute);
                _ = ElympicsEditorWebClient.SendEnginePostRequestApi(url,
                    currentGameConfig.GameVersion,
                    new[] { enginePath },
                    webRequest =>
                    {
                        try
                        {
                            HandleUploadResults(currentGameConfig, webRequest);
                        }
                        catch (ElympicsException e)
                        {
                            _ = EditorUtility.DisplayDialog(title, $"Upload failed: \n{e.Message}", "OK");
                            ElympicsLogger.LogError(e.Message);
                        }

                        EditorUtility.ClearProgressBar();
                        completed?.Invoke(webRequest);
                        GameUploadedToTheCloud?.Invoke();
                    });
            }
        }

        internal static List<(string name, string extension)> GetValidFiles(string[] fileNames, string[] knownCompoundExtensions)
            => WebGLUploader.GetValidFiles(fileNames, knownCompoundExtensions);

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
            catch (Exception e) when (e is ElympicsException or IOException)
            {
                FailUpload(title, e.Message);
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
                FailUpload(title, e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>Like <see cref="UploadStreamingAssetsUsingCurrentLogin"/>, without blocking the Editor.</summary>
        /// <returns>The number of uploaded files.</returns>
        [PublicAPI]
        public static Task<int> UploadStreamingAssetsUsingCurrentLoginAsync(string gameId, string streamingAssetsPath, string version, StreamingAssetsLayout layout,
            CancellationToken ct = default)
        {
            ValidateGameId(gameId);
            return ElympicsEditorApi.UploadStreamingAssetsAsync(gameId, streamingAssetsPath, version, layout, null, ct).AsTask();
        }

        /// <summary>Uploads a WebGL client build with the Editor's current login, without dialogs and without blocking the Editor.</summary>
        /// <param name="clientBuildPath">The WebGL build's root or its Build folder.</param>
        /// <returns>The number of uploaded files.</returns>
        [PublicAPI]
        public static Task<int> UploadClientBuildUsingCurrentLoginAsync(string gameId, string clientBuildPath, string clientGameVersion, string serverGameVersion,
            CancellationToken ct = default)
        {
            ValidateGameId(gameId);
            return ElympicsEditorApi.UploadClientBuildAsync(gameId, clientBuildPath, clientGameVersion, serverGameVersion, null, ct).AsTask();
        }

        /// <summary>
        /// Builds the Linux server of the active game and uploads it as the game's current version with the Editor's current login,
        /// without dialogs. The upload doesn't block the Editor.
        /// </summary>
        /// <returns>The build report. A failed build is returned without uploading.</returns>
        [PublicAPI]
        public static async Task<BuildReport> BuildAndUploadServerUsingCurrentLoginAsync(BuildOptions additionalOptions = BuildOptions.None, CancellationToken ct = default)
        {
            var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig() ?? throw new ElympicsException("Current game config is null");
            await ElympicsEditorApi.EnsureLoggedInAsync(0, ct);

            var buildReport = BuildTools.BuildServerLinux(additionalOptions);
            if (buildReport.summary.result != BuildResult.Succeeded)
                return buildReport;

            if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.EnginePath, EngineSubdirectory, out var enginePath))
                throw new ElympicsException("Problem with packing engine");

            await ElympicsEditorApi.UploadServerAsync(currentGameConfig.GameId, currentGameConfig.GameVersion, enginePath, ct);
            ElympicsLogger.LogInfo($"Uploaded game {currentGameConfig.GameName} with version {currentGameConfig.GameVersion}.");
            GameUploadedToTheCloud?.Invoke();
            return buildReport;
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

            WebGLUploader.PrepareStreamingAssetsFiles(streamingAssetsPath, version, layout, out var relativePaths, out var generatedFiles);

            var initRequest = WebGLUploader.CreateStreamingAssetsInitRequest(gameId, version, relativePaths);
            var initOp = WebGLUploader.SendStreamingAssetsInitRequest(ElympicsWebEndpoint, initRequest);
            while (!initOp.isDone)
            { }

            if (!TryDeserializeResponse<WebGLUploader.StreamingAssetsUploadInitResponse>(initOp.webRequest, StreamingAssetsInitAction, out var initResponse, out var initError))
                throw new ElympicsException(DescribeFailure(StreamingAssetsInitAction, initError));

            WebGLUploader.UploadStreamingAssetsToGcs(streamingAssetsPath, initResponse, generatedFiles);

            ElympicsLogger.LogInfo(DescribeUploadSuccess(relativePaths.Count, version));
        }

        [PublicAPI]
        public static void UploadStreamingAssetsUsingCurrentLogin(string gameId, string streamingAssetsPath, string version, StreamingAssetsLayout layout)
        {
            RefreshAuthTokenIfNeededSynchronously();

            WebGLUploader.PrepareStreamingAssetsFiles(streamingAssetsPath, version, layout, out var relativePaths, out var generatedFiles);

            var initRequest = WebGLUploader.CreateStreamingAssetsInitRequest(gameId, version, relativePaths);
            var initOp = WebGLUploader.SendStreamingAssetsInitRequest(ElympicsWebEndpoint, initRequest);
            while (!initOp.isDone)
            { }

            if (!TryDeserializeResponse<WebGLUploader.StreamingAssetsUploadInitResponse>(initOp.webRequest, StreamingAssetsInitAction, out var initResponse, out var initError))
                throw new ElympicsException(DescribeFailure(StreamingAssetsInitAction, initError));

            WebGLUploader.UploadStreamingAssetsToGcs(streamingAssetsPath, initResponse, generatedFiles);

            ElympicsLogger.LogInfo(DescribeUploadSuccess(relativePaths.Count, version));
        }

        /// <summary>
        /// Blocking counterpart of <see cref="CheckAuthTokenAndRefreshIfNeeded"/>.
        /// </summary>
        private static void RefreshAuthTokenIfNeededSynchronously(long expirationMarginSeconds = 0)
        {
            if (!ElympicsConfig.IsLogin)
                throw new ElympicsException("Not logged in to Elympics cloud. Log in before uploading.");

            var authTokenExpStr = ElympicsConfig.AuthTokenExp;
            if (string.IsNullOrEmpty(authTokenExpStr))
            {
                SetAsLoggedOut();
                throw new ElympicsException("Can't check auth token expiration time. Are you logged in?");
            }

            var authTokenExp = long.Parse(authTokenExpStr);
            var currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            if (currentTimestamp + expirationMarginSeconds <= authTokenExp)
                return;

            ElympicsLogger.LogInfo("Auth token expired. Refreshing using refresh token...");

            var refreshOp = ElympicsEditorWebClient.SendJsonPostRequestApi(RefreshEndpoint, new TokenRefreshingRequestModel { RefreshToken = ElympicsConfig.RefreshToken }, null, false);
            while (!refreshOp.isDone)
            { }

            if (!TryDeserializeResponse(refreshOp.webRequest, "Refresh auth token", out RefreshedTokensResponseModel responseModel))
            {
                SetAsLoggedOut();
                throw new ElympicsException("Refreshing the Elympics auth token failed. Log in to Elympics again.");
            }

            var authToken = responseModel.AuthToken;
            ElympicsConfig.AuthToken = authToken;
            ElympicsConfig.AuthTokenExp = ElympicsEditorApi.ReadTokenExpiration(authToken).ToString();
            ElympicsConfig.RefreshToken = responseModel.RefreshToken;
        }

        // Play-mode code (e.g. the PlayPad extension handshake) sends the stored token as is,
        // so it has to be valid, with a margin for the domain reload and scene load.
        private const long PlayModeTokenExpirationMarginSeconds = 60;

        [InitializeOnLoadMethod]
        private static void RefreshAuthTokenOnEnteringPlayMode() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode || !ElympicsConfig.IsLogin)
                return;
            try
            {
                RefreshAuthTokenIfNeededSynchronously(PlayModeTokenExpirationMarginSeconds);
            }
            catch (Exception e)
            {
                ElympicsLogger.LogException(e);
            }
        }

        private const string StreamingAssetsInitAction = "Initialize StreamingAssets upload";
        private const string ClientBuildInitAction = "Initialize client build upload";

        private static string DescribeFailure(string actionName, string error) =>
            string.IsNullOrEmpty(error) ? $"{actionName} failed." : $"{actionName} failed.{Environment.NewLine}{error}";

        private static string DescribeUploadSuccess(int fileCount, string version) =>
            $"StreamingAssets content uploaded successfully - {fileCount} file(s) as version {version}.";

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
            string serverGameVersion) =>
            UploadClientBuildInBatchmodeInternal(username, password, clientBuildPath, gameId, clientGameVersion, serverGameVersion);

        private static void UploadClientBuildInBatchmodeInternal(
            string username,
            string password,
            string clientBuildPath,
            string gameId,
            string clientGameVersion,
            string serverGameVersion)
        {
            ValidateGameId(gameId);
            LoginAsDeveloper(username, password);

            var (resolvedBuildPath, validFiles) = WebGLUploader.ResolveClientBuildFiles(clientBuildPath, clientGameVersion);
            clientBuildPath = resolvedBuildPath;

            var initRequest = WebGLUploader.CreateInitRequest(gameId, clientGameVersion, serverGameVersion, validFiles);
            var initOp = WebGLUploader.SendInitRequest(ElympicsWebEndpoint, initRequest);
            while (!initOp.isDone)
            { }

            if (!TryDeserializeResponse<WebGLUploader.UploadInitResponse>(initOp.webRequest, ClientBuildInitAction, out var initResponse, out var initError))
                throw new ElympicsException(DescribeFailure(ClientBuildInitAction, initError));

            try
            {
                WebGLUploader.UploadFilesToGcs(clientBuildPath, initResponse, validFiles);
            }
            catch (ElympicsException)
            {
                var failOp = WebGLUploader.SendCompleteRequest(ElympicsWebEndpoint, initResponse.UploadId, false);
                while (!failOp.isDone)
                { }
                throw;
            }

            var completeOp = WebGLUploader.SendCompleteRequest(ElympicsWebEndpoint, initResponse.UploadId, true);
            while (!completeOp.isDone)
            { }

            if (completeOp.webRequest.IsConnectionError() || completeOp.webRequest.IsProtocolError())
                throw new ElympicsException($"Failed to complete client build upload: {completeOp.webRequest.error}");

            ElympicsLogger.LogInfo("Client build uploaded successfully.");
        }

        private static void HandleUploadResults(ElympicsGameConfig currentGameConfig, UnityWebRequest webRequest)
        {
            if (webRequest.IsProtocolError()
                || webRequest.IsConnectionError())
            {
                var errorMessage = ParseResponseErrors(webRequest);
                ElympicsLogger.LogError($"Upload failed for game {currentGameConfig.GameName} " + $"with version: {currentGameConfig.GameVersion}.\nGame ID: {currentGameConfig.GameId}.");
                throw new ElympicsException(errorMessage);
            }

            ElympicsLogger.LogInfo($"Uploaded game {currentGameConfig.GameName} with version {currentGameConfig.GameVersion}.");
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
            var buildReport = BuildTools.BuildServerLinux(additionalOptions);
            if (buildReport.summary.result != BuildResult.Succeeded)
                return buildReport;

            var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig() ?? throw new ElympicsException("Current game config is null");

            if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.EnginePath, EngineSubdirectory, out var enginePath))
                throw new ElympicsException("Problem with packing engine");

            var url = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute, currentGameConfig.GameId, GamesRoutes.GameVersionsRoute);
            var uploadOp = ElympicsEditorWebClient.SendEnginePostRequestApi(url, currentGameConfig.GameVersion, new[] { enginePath });

            while (!uploadOp.isDone)
                ;

            HandleUploadResults(currentGameConfig, uploadOp.webRequest);
            return buildReport;
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

            var loginOp = Login(username, password);

            while (!loginOp.isDone)
                ;

            LoginHandler(loginOp.webRequest);

            if (!ElympicsConfig.IsLogin)
                throw new ElympicsException("Login operation failed. Check log for details");
        }

        private static bool TryPack(string gameId, string gameVersion, string buildPath, string targetSubdirectory, out string destinationFilePath)
        {
            var destinationDirectoryPath = Path.Combine(DirectoryNameToUpload, gameId, gameVersion, targetSubdirectory);
            destinationFilePath = Path.Combine(destinationDirectoryPath, PackFileName);

            _ = Directory.CreateDirectory(destinationDirectoryPath);
            try
            {
                ElympicsLogger.LogInfo($"Trying to pack {targetSubdirectory}...");
                if (File.Exists(destinationFilePath))
                    File.Delete(destinationFilePath);
                _ = Directory.CreateDirectory(buildPath);
                CreateZipFromDirectory(buildPath, destinationFilePath);
            }
            catch (Exception e)
            {
                ElympicsLogger.LogError(e.Message);
                return false;
            }

            return true;
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

        private static void CheckAuthTokenAndRefreshIfNeeded(Action<bool> continuation)
        {
            var authTokenExpStr = ElympicsConfig.AuthTokenExp;
            if (string.IsNullOrEmpty(authTokenExpStr))
            {
                SetAsLoggedOut();
                ElympicsLogger.LogException(new ElympicsException("Can't check auth token expiration time. Are you logged in?"));
                return;
            }

            var authTokenExp = long.Parse(authTokenExpStr);
            var currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            if (currentTimestamp <= authTokenExp)
            {
                continuation?.Invoke(true);
                return;
            }

            ElympicsLogger.LogInfo("Auth token expired. Refreshing using refresh token...");
            var refreshToken = ElympicsConfig.RefreshToken;
            _ = ElympicsEditorWebClient.SendJsonPostRequestApi(RefreshEndpoint, new TokenRefreshingRequestModel { RefreshToken = refreshToken }, OnCompleted, false);

            void OnCompleted(UnityWebRequest webRequest)
            {
                var deserialized = TryDeserializeResponse(webRequest, "Refresh auth token", out RefreshedTokensResponseModel responseModel);
                if (deserialized)
                {
                    var authToken = responseModel.AuthToken;
                    ElympicsConfig.AuthToken = authToken;
                    ElympicsConfig.AuthTokenExp = ElympicsEditorApi.ReadTokenExpiration(authToken).ToString();
                    ElympicsConfig.RefreshToken = responseModel.RefreshToken;
                }
                else
                    SetAsLoggedOut();

                continuation?.Invoke(deserialized);
            }
        }

        private static string GetCombinedUrl(params string[] urlParts) => string.Join("/", urlParts);

        private static bool TryDeserializeResponse<T>(UnityWebRequest webRequest, string actionName, out T deserializedResponse, bool silent = false) =>
            TryDeserializeResponse(webRequest, actionName, out deserializedResponse, out _, silent);

        private static bool TryDeserializeResponse<T>(UnityWebRequest webRequest, string actionName, out T deserializedResponse, out string error, bool silent = false)
        {
            deserializedResponse = default;
            error = null;
            if (webRequest.IsProtocolError()
                || webRequest.IsConnectionError())
            {
                error = ParseResponseErrors(webRequest, silent);
                if (!silent)
                    ElympicsLogger.LogError($"Error occurred for action '{actionName}': {error}");
                return false;
            }

            var jsonBody = webRequest.downloadHandler.text;
            if (typeof(T) == typeof(string))
                deserializedResponse = (T)(object)jsonBody;
            else
                try
                {
                    deserializedResponse = JsonConvert.DeserializeObject<T>(jsonBody);
                }
                catch (JsonException e)
                {
                    error = e.Message;
                    if (!silent)
                        ElympicsLogger.LogException(e);
                    return false;
                }

            return true;
        }

        private static string ParseResponseErrors(UnityWebRequest request, bool silent = false)
        {
            if (request.responseCode == 401)
                return "Not authenticated, please login to your ElympicsWeb account";
            if (request.responseCode == 403)
                return "This account is not authorized to access the requested resource";

            ErrorModel errorModel = null;
            try
            {
                errorModel = JsonConvert.DeserializeObject<ErrorModel>(request.downloadHandler.text);
            }
            catch (JsonException e)
            {
                if (!silent)
                    ElympicsLogger.LogException(e);
            }

            if (errorModel?.Errors == null
                || errorModel.Errors.Count == 0)
                return $"Received error response code {request.responseCode} with error:\n{request.downloadHandler.text}";

            var errors = string.Join("\n", errorModel.Errors.SelectMany(r => r.Value.Select(x => $"[{r.Key}] {x}")));
            return $"Received error response code {request.responseCode} with errors:\n{errors}";
        }

        [Serializable]
        private class ErrorModel
        {
            public Dictionary<string, string[]> Errors = new();
        }
    }
}
