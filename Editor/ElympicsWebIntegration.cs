using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Elympics.Editor.Models.UsageStatistics;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine.Networking;
using AuthRoutes = ElympicsApiModels.ApiModels.Auth.Routes;
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
        private const string BotSubdirectory = "Bot";

        private static string ElympicsWebEndpoint => ElympicsConfig.Load().ElympicsApiEndpoint;

        private static ElympicsConfig Config => ElympicsConfig.Load();

        private static string RefreshEndpoint => GetCombinedUrl(ElympicsWebEndpoint, AuthRoutes.BaseRoute, AuthRoutes.RefreshRoute);

        internal static event Action GameUploadedToTheCloud;

        public static void Login()
        {
            _ = Login(ElympicsConfig.Username, ElympicsConfig.Password, LoginHandler);
        }

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
            public string UserName;
            public string Password;
        }

        [Serializable]
        private class LoggedInTokenResponseModel
        {
            public string UserName = null;
            public string AuthToken = null;
            public string RefreshToken = null;
        }

        [Serializable]
        private class TokenRefreshingRequestModel
        {
            public string RefreshToken;
        }

        [Serializable]
        public class RefreshedTokensResponseModel
        {
            public string AuthToken;
            public string RefreshToken;
        }

        [Serializable]
        public class ElympicsEndpointsModel
        {
            public string Lobby;
            public string GameServers;
        }

        [Serializable]
        public class GameResponseModel
        {
            public string Id;
            public string Name;
        }

        [Serializable]
        public class GameVersionResponseModel
        {
            public string Id;
            public string Version;
            public bool Uploaded;
            public string UploadedTime;
            public bool Blocked;
            public bool DebugMode;
            public bool DebugModeWithBots;
        }

        [Serializable]
        public class GameVersionsResponseModel
        {
            public string GameName;
            public List<GameVersionResponseModel> Versions;
        }


        [Serializable]
        private class JwtMidPart
        {
            public long exp = 0;
        }

        private static UnityWebRequestAsyncOperation Login(string username, string password, Action<UnityWebRequest> completed = null)
        {
            ElympicsLogger.Log($"Logging in as {username}");

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
            ElympicsLogger.Log($"Received authentication token.\nResponse code: {webRequest.responseCode}.");
            if (TryDeserializeResponse(webRequest, "Login", out LoggedInTokenResponseModel responseModel))
            {
                try
                {
                    var authToken = responseModel.AuthToken;

                    ElympicsLogger.Log($"Logged to ElympicsWeb as {responseModel.UserName}.");
                    ElympicsConfig.AuthToken = authToken;
                    ElympicsConfig.AuthTokenExp = GetAuthTokenMid(authToken).exp.ToString();
                    ElympicsConfig.RefreshToken = responseModel.RefreshToken;
                    ElympicsConfig.IsLogin = true;
                }
                catch (Exception e)
                {
                    _ = ElympicsLogger.LogException(e);
                }
            }
        }

        private static JwtMidPart GetAuthTokenMid(string authToken)
        {
            var authTokenParts = authToken.Split('.');
            var authTokenMidPadded = authTokenParts[1].PadRight(4 * ((authTokenParts[1].Length + 3) / 4), '=');
            var authTokenMidStr =
                Encoding.ASCII.GetString(Convert.FromBase64String(authTokenMidPadded.Replace('-', '+').Replace('_', '/'))); // JWT is encoded as URL-safe base64, some characters have to be replaced
            var authTokenMid = JsonConvert.DeserializeObject<JwtMidPart>(authTokenMidStr);
            return authTokenMid;
        }

        internal static void GetAvailableRegionsForGameId(string gameId, Action<List<RegionResponseModel>> updateProperty, Action onFailure)
        {
            ElympicsLogger.Log("Getting available regions...");

            var uri = string.IsNullOrEmpty(gameId) ? Config.ElympicsAvailableRegionsUrl : Config.GameAvailableRegionsUrl(gameId);

            _ = ElympicsEditorWebClient.SendJsonGetRequestApi(uri, OnCompleted);

            void OnCompleted(UnityWebRequest webRequest)
            {
                GetAvailableRegionsHandler(updateProperty, webRequest, onFailure);
            }
        }

        private static UnityWebRequest gameVersionsWebRequest;

        public static void GetGameVersions(Action<GameVersionsResponseModel> updateProperty, bool silent = false)
        {
            CheckAuthTokenAndRefreshIfNeeded(OnContinuation);

            void OnContinuation(bool success)
            {
                if (!success)
                    return;  // TODO: error handling ~dsygocki 2025-07-10
                var gameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
                if (gameConfig == null)
                    return;  // TODO: error handling ~dsygocki 2025-07-10
                var uri = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute, gameConfig.gameId, GamesRoutes.GameVersionsRoute);

                var unityWebRequestAsyncOperation = ElympicsEditorWebClient.SendJsonGetRequestApi(uri, OnCompleted, silent);
                if (gameVersionsWebRequest is { isDone: false })
                    gameVersionsWebRequest.Abort();
                gameVersionsWebRequest = unityWebRequestAsyncOperation.webRequest;

                void OnCompleted(UnityWebRequest webRequest)
                {
                    gameVersionsWebRequest = null;
                    if (TryDeserializeResponse(webRequest, nameof(GetGameVersions), out GameVersionsResponseModel gameVersions, silent))
                        updateProperty?.Invoke(gameVersions);
                }
            }
        }

        public static void GetGameVersionsForGameId(string gameId, Action<GameVersionsResponseModel> updateProperty, bool silent = false)
        {
            CheckAuthTokenAndRefreshIfNeeded(OnContinuation);

            void OnContinuation(bool success)
            {
                if (!success)
                    return;  // TODO: error handling ~dsygocki 2025-07-10

                var uri = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute, gameId, GamesRoutes.GameVersionsRoute);

                var unityWebRequestAsyncOperation = ElympicsEditorWebClient.SendJsonGetRequestApi(uri, OnCompleted, silent);
                if (gameVersionsWebRequest is { isDone: false })
                    gameVersionsWebRequest.Abort();
                gameVersionsWebRequest = unityWebRequestAsyncOperation.webRequest;

                void OnCompleted(UnityWebRequest webRequest)
                {
                    gameVersionsWebRequest = null;
                    if (TryDeserializeResponse(webRequest, nameof(GetGameVersionsForGameId), out GameVersionsResponseModel gameVersions, silent))
                        updateProperty?.Invoke(gameVersions);
                }
            }
        }

        public static void GetGames(Action<List<GameResponseModel>> updateProperty)
        {
            ElympicsLogger.Log("Getting available games...");

            CheckAuthTokenAndRefreshIfNeeded(OnContinuation);

            void OnContinuation(bool success)
            {
                if (!success)
                    return;

                var uri = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute);
                _ = ElympicsEditorWebClient.SendJsonGetRequestApi(uri, OnCompleted);

                void OnCompleted(UnityWebRequest webRequest)
                {
                    GetAvailableGamesHandler(updateProperty, webRequest);
                }
            }
        }

        private static void GetAvailableGamesHandler(Action<List<GameResponseModel>> updateProperty, UnityWebRequest webRequest)
        {
            ElympicsLogger.Log($"Received available games.\nResponse code: {webRequest.responseCode}.");
            if (!TryDeserializeResponse(webRequest, "GetAvailableGames", out List<GameResponseModel> availableGames))
                return;

            updateProperty.Invoke(availableGames);
        }

        private static void GetAvailableRegionsHandler(Action<List<RegionResponseModel>> updateProperty, UnityWebRequest webRequest, Action onFailure)
        {
            ElympicsLogger.Log($"Received available regions.\nResponse code: {webRequest.responseCode}.");
            if (TryDeserializeResponse(webRequest, "GetAvailableRegions", out AvailableRegionsResponseModel availableRegions))
            {
                updateProperty?.Invoke(availableRegions.Regions.ToList());
                return;
            }
            onFailure?.Invoke();
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
                        ElympicsLogger.Log($"Elympics endpoints have been set to: {endpoints.Lobby}, {endpoints.GameServers}.");
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
                    gameId = gameConfig.GameId,
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

                if (!BuildTools.BuildElympicsServerLinux(BuildOptions.None))
                    return;

                var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
                string enginePath;
                string botPath;
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

                    EditorUtility.DisplayProgressBar(title, "Packing bot", 0.4f);
                    if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.BotPath, BotSubdirectory, out botPath))
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
                    new[] { enginePath, botPath },
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

        private static void HandleUploadResults(ElympicsGameConfig currentGameConfig, UnityWebRequest webRequest)
        {
            if (webRequest.IsProtocolError()
                || webRequest.IsConnectionError())
            {
                var errorMessage = ParseResponseErrors(webRequest);
                ElympicsLogger.LogError($"Upload failed for game {currentGameConfig.GameName} " + $"with version: {currentGameConfig.GameVersion}.\nGame ID: {currentGameConfig.GameId}.");
                throw new ElympicsException(errorMessage);
            }

            ElympicsLogger.Log($"Uploaded game {currentGameConfig.GameName} with version {currentGameConfig.GameVersion}.");
        }

        [UsedImplicitly]
        public static void BuildAndUploadServerInBatchmode(string username, string password, BuildOptions additionalOptions = BuildOptions.None)
        {
            _ = ElympicsConfig.LoadCurrentElympicsGameConfig() ?? throw new ElympicsException("No Elympics game config found. Configure your game before trying to build a server.");
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            var loginOp = Login(username, password);

            while (!loginOp.isDone)
                ;

            LoginHandler(loginOp.webRequest);

            if (!ElympicsConfig.IsLogin)
                throw new ElympicsException("Login operation failed. Check log for details");

            if (!BuildTools.BuildElympicsServerLinux(additionalOptions))
                throw new ElympicsException("Build failed");

            var currentGameConfig = ElympicsConfig.LoadCurrentElympicsGameConfig();
            if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.EnginePath, EngineSubdirectory, out var enginePath))
                throw new ElympicsException("Problem with packing engine");

            if (!TryPack(currentGameConfig.GameId, currentGameConfig.GameVersion, BuildTools.BotPath, BotSubdirectory, out var botPath))
                throw new ElympicsException("Problem with packing bot");

            var url = GetCombinedUrl(ElympicsWebEndpoint, GamesRoutes.BaseRoute, currentGameConfig.GameId, GamesRoutes.GameVersionsRoute);
            var uploadOp = ElympicsEditorWebClient.SendEnginePostRequestApi(url, currentGameConfig.GameVersion, new[] { enginePath, botPath });

            while (!uploadOp.isDone)
                ;

            HandleUploadResults(currentGameConfig, uploadOp.webRequest);
        }

        private static bool TryPack(string gameId, string gameVersion, string buildPath, string targetSubdirectory, out string destinationFilePath)
        {
            var destinationDirectoryPath = Path.Combine(DirectoryNameToUpload, gameId, gameVersion, targetSubdirectory);
            destinationFilePath = Path.Combine(destinationDirectoryPath, PackFileName);

            _ = Directory.CreateDirectory(destinationDirectoryPath);
            try
            {
                ElympicsLogger.Log($"Trying to pack {targetSubdirectory}...");
                if (File.Exists(destinationFilePath))
                    File.Delete(destinationFilePath);
                ZipFile.CreateFromDirectory(buildPath, destinationFilePath, CompressionLevel.Optimal, false);
            }
            catch (Exception e)
            {
                ElympicsLogger.LogError(e.Message);
                return false;
            }

            return true;
        }

        private static void CheckAuthTokenAndRefreshIfNeeded(Action<bool> continuation)
        {
            var authTokenExpStr = ElympicsConfig.AuthTokenExp;
            if (string.IsNullOrEmpty(authTokenExpStr))
            {
                SetAsLoggedOut();
                throw new ElympicsException("Can't check auth token expiration time. Are you logged in?");
            }

            var authTokenExp = long.Parse(authTokenExpStr);
            var currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            if (currentTimestamp <= authTokenExp)
            {
                continuation?.Invoke(true);
                return;
            }

            ElympicsLogger.Log("Auth token expired. Refreshing using refresh token...");
            var refreshToken = ElympicsConfig.RefreshToken;
            _ = ElympicsEditorWebClient.SendJsonPostRequestApi(RefreshEndpoint, new TokenRefreshingRequestModel { RefreshToken = refreshToken }, OnCompleted, false);

            void OnCompleted(UnityWebRequest webRequest)
            {
                var deserialized = TryDeserializeResponse(webRequest, "Refresh auth token", out RefreshedTokensResponseModel responseModel);
                if (deserialized)
                {
                    var authToken = responseModel.AuthToken;
                    ElympicsConfig.AuthToken = authToken;
                    ElympicsConfig.AuthTokenExp = GetAuthTokenMid(authToken).exp.ToString();
                    ElympicsConfig.RefreshToken = responseModel.RefreshToken;
                }
                else
                    SetAsLoggedOut();

                continuation?.Invoke(deserialized);
            }
        }

        private static string GetCombinedUrl(params string[] urlParts) => string.Join("/", urlParts);

        private static bool TryDeserializeResponse<T>(UnityWebRequest webRequest, string actionName, out T deserializedResponse, bool silent = false)
        {
            deserializedResponse = default;
            if (webRequest.IsProtocolError()
                || webRequest.IsConnectionError())
            {
                var errorMessage = ParseResponseErrors(webRequest, silent);
                if (!silent)
                    ElympicsLogger.LogError($"Error occurred for action {actionName}: {errorMessage}");
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
                    if (!silent)
                        _ = ElympicsLogger.LogException(e);
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
                    _ = ElympicsLogger.LogException(e);
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
