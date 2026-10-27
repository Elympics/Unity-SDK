using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Elympics.Core.Logger;
using Elympics.Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;
using AuthRoutes = ElympicsApiModels.ApiModels.Auth.Routes;
using GamesRoutes = ElympicsApiModels.ApiModels.Games.Routes;
using RegionsRoutes = ElympicsApiModels.ApiModels.Regions.Routes;
using UsageStatisticsRoutes = Elympics.Editor.Models.UsageStatistics.Routes;
using UsersRoutes = ElympicsApiModels.ApiModels.Users.Routes;

namespace Elympics.Editor.Communication
{
    internal enum EditorApiErrorKind
    {
        NotLoggedIn,
        Unauthorized,
        Forbidden,
        NotFound,
        Network,
        Timeout,
        RequestFailed,
        InvalidResponse,
        UntrustedHost,
    }

    internal sealed class ElympicsEditorApiException : ElympicsException
    {
        public EditorApiErrorKind Kind { get; }
        public long StatusCode { get; }
        public string Problem { get; }
        public string Fix { get; }

        public ElympicsEditorApiException(EditorApiErrorKind kind, string message, long statusCode = 0, string fix = null)
            : base(fix == null ? message : $"{message} Fix: {fix}")
        {
            Kind = kind;
            StatusCode = statusCode;
            Problem = message;
            Fix = fix;
        }
    }

    /// <summary>The Editor's Elympics login. Tests replace it so that they never touch the machine-global EditorPrefs login.</summary>
    internal interface IEditorLoginStore
    {
        bool IsLogin { get; set; }
        string AuthToken { get; set; }
        string AuthTokenExp { get; set; }
        string RefreshToken { get; set; }
    }

    internal sealed class EditorPrefsLoginStore : IEditorLoginStore
    {
        public bool IsLogin { get => ElympicsConfig.IsLogin; set => ElympicsConfig.IsLogin = value; }
        public string AuthToken { get => ElympicsConfig.AuthToken; set => ElympicsConfig.AuthToken = value; }
        public string AuthTokenExp { get => ElympicsConfig.AuthTokenExp; set => ElympicsConfig.AuthTokenExp = value; }
        public string RefreshToken { get => ElympicsConfig.RefreshToken; set => ElympicsConfig.RefreshToken = value; }
    }

    /// <summary>
    /// Async core of the Editor's Elympics web API calls. Every method throws <see cref="ElympicsEditorApiException"/> (or
    /// <see cref="ElympicsException"/> for invalid local input) instead of logging and skipping its callback, and none blocks the main thread.
    /// </summary>
    internal static class ElympicsEditorApi
    {
        private const string QueuesRoute = "queues";
        private const string FleetsRoute = "fleets";
        private const string ClientBuildsRoute = "client-builds";
        private const string ClientBuildInitRoute = "init";
        private const string ClientBuildCompleteRoute = "complete";
        private const string StreamingAssetsRoute = "streaming-assets";
        // Defaults for calls without timeoutSeconds. A caller's timeoutSeconds applies to every request of the call instead.
        private const int JsonTimeoutSeconds = 30;
        internal const int FileUploadTimeoutSeconds = 1800;

        internal static IEditorApiTransport Transport = new UnityWebRequestEditorApiTransport();
        internal static Func<IEditorApiTransport> CreateBlockingTransport = () => new BusyWaitEditorApiTransport();
        internal static IEditorLoginStore LoginStore = new EditorPrefsLoginStore();
        internal static Func<long> GetUtcNowSeconds = () => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        internal static Func<string> GetApiEndpointOverride;

        private static string ApiEndpoint =>
            GetApiEndpointOverride?.Invoke()
            ?? ElympicsConfig.Load()?.ElympicsApiEndpoint
            ?? throw new ElympicsException($"No ElympicsConfig found in Resources/{ElympicsConfig.PathInResources}.");

        internal static void RequireTrustedApiEndpoint() => ElympicsApiHostPolicy.Require(new Uri(ApiEndpoint));

        private static string BuildUrl(params string[] segments) => string.Join("/", new[] { ApiEndpoint.TrimEnd('/') }.Concat(segments.Select(Uri.EscapeDataString)));

        #region Blocking calls

        /// <summary>
        /// Runs <paramref name="call"/> to completion on the calling thread, with a transport that polls instead of waiting for the player loop.
        /// Blocks the Editor: for batch mode methods and for the few places that can't yield.
        /// </summary>
        /// <exception cref="InvalidOperationException">The call is still pending when it returns, because something in it awaits the player loop.</exception>
        internal static T RunBlocking<T>(Func<CancellationToken, UniTask<T>> call)
        {
            using var scope = new BlockingScope();
            var task = call(scope.Token);
            // A UniTask, like a ValueTask, allows GetResult only once and only after it completes; GetResult also returns pooled sources,
            // so Status must be read before it and the task must not be touched afterwards.
            scope.ThrowIfPending(task.Status);
            return task.GetAwaiter().GetResult();
        }

        internal static void RunBlocking(Func<CancellationToken, UniTask> call) => _ = RunBlocking(ct => call(ct).AsAsyncUnitUniTask());

        private sealed class BlockingScope : IDisposable
        {
            private readonly IEditorApiTransport _previousTransport = Transport;
            private readonly CancellationTokenSource _cancellation = new();

            public CancellationToken Token => _cancellation.Token;

            public BlockingScope() => Transport = CreateBlockingTransport();

            public void ThrowIfPending(UniTaskStatus status)
            {
                if (status != UniTaskStatus.Pending)
                    return;
                _cancellation.Cancel();
                throw new InvalidOperationException("A blocking Elympics Editor API call didn't complete synchronously: it awaits the player loop, which can't tick while the call blocks the main thread.");
            }

            public void Dispose()
            {
                Transport = _previousTransport;
                _cancellation.Dispose();
            }
        }

        #endregion

        #region Login

        [Serializable]
        private class LoginRequestModel
        {
            // ReSharper disable InconsistentNaming
            public string UserName;
            public string Password;
            // ReSharper restore InconsistentNaming
        }

        [Serializable]
        private class LoginResponseModel
        {
            // ReSharper disable InconsistentNaming
            public string UserName;
            public string AuthToken;
            public string RefreshToken;
            // ReSharper restore InconsistentNaming
        }

        public static async UniTask<string> LoginAsync(string username, string password, int? timeoutSeconds = null, CancellationToken ct = default)
        {
            var response = await SendAsync("Login", new EditorApiRequest
            {
                Method = UnityWebRequest.kHttpVerbPOST,
                Url = BuildUrl(AuthRoutes.BaseRoute, AuthRoutes.LoginRoute),
                JsonBody = JsonConvert.SerializeObject(new LoginRequestModel { UserName = username, Password = password }),
                Auth = false,
                TimeoutSeconds = JsonTimeoutSeconds,
            }, false, timeoutSeconds, ct);

            var tokens = Deserialize<LoginResponseModel>("Login", response);
            var expiration = ReadTokenExpiration("Login", tokens.AuthToken, response.StatusCode);

            LoginStore.AuthToken = tokens.AuthToken;
            LoginStore.AuthTokenExp = expiration.ToString();
            LoginStore.RefreshToken = tokens.RefreshToken;
            LoginStore.IsLogin = true;
            return tokens.UserName;
        }

        [Serializable]
        private class TokenRefreshingRequestModel
        {
            // ReSharper disable once InconsistentNaming
            public string RefreshToken;
        }

        [Serializable]
        private class JwtMidPart
        {
            // ReSharper disable once InconsistentNaming
            public long exp;
        }

        /// <remarks>Logs out only when the backend rejects the refresh token (400, 401 or 403), not when it can't be reached or fails (5xx, 429).</remarks>
        public static async UniTask EnsureLoggedInAsync(long expirationMarginSeconds = 0, int? timeoutSeconds = null, CancellationToken ct = default)
        {
            if (!LoginStore.IsLogin)
                throw new ElympicsEditorApiException(EditorApiErrorKind.NotLoggedIn, "Not logged in to Elympics cloud.");

            if (!long.TryParse(LoginStore.AuthTokenExp, out var expiration))
            {
                LoginStore.IsLogin = false;
                throw new ElympicsEditorApiException(EditorApiErrorKind.NotLoggedIn, "Can't check the auth token's expiration time. Log in to Elympics again.");
            }

            if (GetUtcNowSeconds() + expirationMarginSeconds <= expiration)
                return;

            ElympicsLogger.LogInfo("Auth token expired. Refreshing using refresh token...");
            var response = await Transport.SendAsync(new EditorApiRequest
            {
                Method = UnityWebRequest.kHttpVerbPOST,
                Url = BuildUrl(AuthRoutes.BaseRoute, AuthRoutes.RefreshRoute),
                JsonBody = JsonConvert.SerializeObject(new TokenRefreshingRequestModel { RefreshToken = LoginStore.RefreshToken }),
                Auth = false,
                TimeoutSeconds = timeoutSeconds ?? JsonTimeoutSeconds,
            }, ct);

            if (!response.Succeeded)
            {
                var error = ToException("Refresh auth token", response);
                if (response.StatusCode is not (400 or 401 or 403))
                    throw error;
                LoginStore.IsLogin = false;
                throw new ElympicsEditorApiException(EditorApiErrorKind.NotLoggedIn, $"{error.Message} Log in to Elympics again.", response.StatusCode);
            }

            var tokens = Deserialize<ElympicsWebIntegration.RefreshedTokensResponseModel>("Refresh auth token", response);
            var newExpiration = ReadTokenExpiration("Refresh auth token", tokens.AuthToken, response.StatusCode);
            LoginStore.AuthToken = tokens.AuthToken;
            LoginStore.AuthTokenExp = newExpiration.ToString();
            LoginStore.RefreshToken = tokens.RefreshToken;
        }

        private static long ReadTokenExpiration(string action, string authToken, long statusCode)
        {
            try
            {
                return ReadTokenExpiration(authToken);
            }
            catch (Exception e) when (e is FormatException or JsonException or IndexOutOfRangeException or NullReferenceException)
            {
                throw new ElympicsEditorApiException(EditorApiErrorKind.InvalidResponse, $"{action} failed: the auth token is malformed.", statusCode);
            }
        }

        internal static long ReadTokenExpiration(string authToken)
        {
            var parts = authToken.Split('.');
            var payload = parts[1].PadRight(4 * ((parts[1].Length + 3) / 4), '=');
            // JWT is encoded as URL-safe base64.
            var json = Encoding.ASCII.GetString(Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/')));
            return JsonConvert.DeserializeObject<JwtMidPart>(json).exp;
        }

        #endregion

        #region Games, versions, regions, fleets, queues

        public static UniTask<List<ElympicsWebIntegration.GameResponseModel>> GetGamesAsync(int? timeoutSeconds = null, CancellationToken ct = default) =>
            SendJsonAsync<List<ElympicsWebIntegration.GameResponseModel>>("Get games", UnityWebRequest.kHttpVerbGET, BuildUrl(GamesRoutes.BaseRoute), null, timeoutSeconds, ct);

        public static UniTask<ElympicsWebIntegration.GameVersionsResponseModel> GetGameVersionsAsync(string gameId, int? timeoutSeconds = null, CancellationToken ct = default) =>
            SendJsonAsync<ElympicsWebIntegration.GameVersionsResponseModel>("Get game versions", UnityWebRequest.kHttpVerbGET,
                BuildUrl(GamesRoutes.BaseRoute, gameId, GamesRoutes.GameVersionsRoute), null, timeoutSeconds, ct);

        /// <param name="gameId">Empty for every region of the Elympics cloud.</param>
        public static async UniTask<List<RegionResponseModel>> GetRegionsAsync(string gameId, int? timeoutSeconds = null, CancellationToken ct = default)
        {
            var route = string.IsNullOrEmpty(gameId) ? RegionsRoutes.AllRegionsRouteUnityFormat : RegionsRoutes.RegionForGameIdRoute;
            var segments = route.Split('/').Select(segment => segment == "{0}" ? gameId : segment).ToArray();
            var response = await SendAsync("Get regions", new EditorApiRequest { Url = BuildUrl(segments), TimeoutSeconds = JsonTimeoutSeconds }, false, timeoutSeconds, ct);
            return Deserialize<AvailableRegionsResponseModel>("Get regions", response).Regions?.ToList() ?? new List<RegionResponseModel>();
        }

        public static UniTask<ElympicsWebIntegration.ElympicsEndpointsModel> GetEndpointsAsync(int? timeoutSeconds = null, CancellationToken ct = default) =>
            SendJsonAsync<ElympicsWebIntegration.ElympicsEndpointsModel>("Get Elympics Endpoints", UnityWebRequest.kHttpVerbGET, BuildUrl(UsersRoutes.BaseRoute, UsersRoutes.EndpointRoutes), null, timeoutSeconds, ct);

        /// <summary>Posts a usage statistics event. Failures of the request are ignored, since losing an event must never disturb the Editor.</summary>
        public static async UniTask PostUsageEventAsync(string route, object body, bool requireLogin, int? timeoutSeconds = null, CancellationToken ct = default)
        {
            if (requireLogin && !LoginStore.IsLogin)
                return;
            try
            {
                if (requireLogin)
                {
                    try
                    {
                        await EnsureLoggedInAsync(0, timeoutSeconds, ct);
                    }
                    catch (ElympicsEditorApiException)
                    { }
                }

                var response = await Transport.SendAsync(new EditorApiRequest
                {
                    Method = UnityWebRequest.kHttpVerbPOST,
                    Url = BuildUrl(UsageStatisticsRoutes.Base, route),
                    JsonBody = JsonConvert.SerializeObject(body),
                    Auth = LoginStore.IsLogin && !IsAuthTokenExpired(),
                    TimeoutSeconds = timeoutSeconds ?? JsonTimeoutSeconds,
                }, ct);
                if (!response.Succeeded)
                    ElympicsLogger.LogDebug($"Usage statistics event '{route}' failed: {response.Error}");
            }
            catch (ElympicsEditorApiException e)
            {
                ElympicsLogger.LogDebug($"Usage statistics event '{route}' failed: {e.Message}");
            }
        }

        private static bool IsAuthTokenExpired() => !long.TryParse(LoginStore.AuthTokenExp, out var expiration) || GetUtcNowSeconds() > expiration;

        public static UniTask<List<FleetModel>> GetFleetsAsync(int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallAsync<List<FleetModel>>("Get fleets", UnityWebRequest.kHttpVerbGET, null, timeoutSeconds, ct, GamesRoutes.BaseRoute, FleetsRoute);

        /// <returns>The created game, or null if the backend's response was empty.</returns>
        public static UniTask<GameModel> CreateGameAsync(string name, string initialRegionName, string fleetId, int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallWithOptionalResponseAsync<GameModel>("Create game", UnityWebRequest.kHttpVerbPOST,
                new CreateGameRequestModel { GameName = name, InitialRegionName = initialRegionName, FleetId = string.IsNullOrEmpty(fleetId) ? null : fleetId },
                timeoutSeconds, ct, GamesRoutes.BaseRoute);

        public static UniTask<GameModel> GetGameAsync(string gameId, int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallAsync<GameModel>("Get game", UnityWebRequest.kHttpVerbGET, null, timeoutSeconds, ct, GamesRoutes.BaseRoute, gameId);

        /// <returns>The updated game, or null if the backend's response was empty.</returns>
        public static UniTask<GameModel> UpdateGameAsync(string gameId, UpdateGameRequestModel body, int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallWithOptionalResponseAsync<GameModel>("Update game", UnityWebRequest.kHttpVerbPUT, body, timeoutSeconds, ct, GamesRoutes.BaseRoute, gameId);

        public static UniTask<ElympicsWebIntegration.GameVersionResponseModel> GetGameVersionAsync(string gameId, string versionId, int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallAsync<ElympicsWebIntegration.GameVersionResponseModel>("Get game version", UnityWebRequest.kHttpVerbGET, null, timeoutSeconds, ct,
                GamesRoutes.BaseRoute, gameId, GamesRoutes.GameVersionsRoute, versionId);

        /// <remarks>The backend replies with the game, not the version, so the reply is ignored. Use <see cref="GetGameVersionAsync"/> to read the result.</remarks>
        public static UniTask UpdateGameVersionAsync(string gameId, string versionId, UpdateGameVersionRequestModel body, int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallAsync("Update game version", UnityWebRequest.kHttpVerbPUT, body, timeoutSeconds, ct, GamesRoutes.BaseRoute, gameId, GamesRoutes.GameVersionsRoute, versionId);

        public static UniTask DeleteGameVersionAsync(string gameId, string versionId, int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallAsync("Delete game version", UnityWebRequest.kHttpVerbDELETE, null, timeoutSeconds, ct, GamesRoutes.BaseRoute, gameId, GamesRoutes.GameVersionsRoute, versionId);

        public static UniTask<List<QueueModel>> GetQueuesAsync(string gameId, int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallAsync<List<QueueModel>>("Get queues", UnityWebRequest.kHttpVerbGET, null, timeoutSeconds, ct, GamesRoutes.BaseRoute, gameId, QueuesRoute);

        /// <returns>The created queue, or null if the backend's response was empty.</returns>
        public static UniTask<QueueModel> CreateQueueAsync(string gameId, QueueSettingsModel body, int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallWithOptionalResponseAsync<QueueModel>("Create queue", UnityWebRequest.kHttpVerbPOST, body, timeoutSeconds, ct, GamesRoutes.BaseRoute, gameId, QueuesRoute);

        /// <returns>The updated queue, or null if the backend's response was empty.</returns>
        public static UniTask<QueueModel> UpdateQueueAsync(string gameId, string queueId, QueueSettingsModel body, int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallWithOptionalResponseAsync<QueueModel>("Update queue", UnityWebRequest.kHttpVerbPUT, body, timeoutSeconds, ct, GamesRoutes.BaseRoute, gameId, QueuesRoute, queueId);

        public static UniTask DeleteQueueAsync(string gameId, string queueId, int? timeoutSeconds = null, CancellationToken ct = default) =>
            CallAsync("Delete queue", UnityWebRequest.kHttpVerbDELETE, null, timeoutSeconds, ct, GamesRoutes.BaseRoute, gameId, QueuesRoute, queueId);

        private static async UniTask CallAsync(string action, string method, object body, int? timeoutSeconds, CancellationToken ct, params string[] route) =>
            _ = await SendJsonAsync(action, method, BuildUrl(route), body, timeoutSeconds, ct);

        private static UniTask<T> CallAsync<T>(string action, string method, object body, int? timeoutSeconds, CancellationToken ct, params string[] route) =>
            SendJsonAsync<T>(action, method, BuildUrl(route), body, timeoutSeconds, ct);

        private static async UniTask<T> CallWithOptionalResponseAsync<T>(string action, string method, object body, int? timeoutSeconds, CancellationToken ct, params string[] route)
            where T : class
        {
            var response = await SendJsonAsync(action, method, BuildUrl(route), body, timeoutSeconds, ct);
            return string.IsNullOrWhiteSpace(response.Text) ? null : Deserialize<T>(action, response);
        }

        #endregion

        #region Uploads

        public static async UniTask UploadServerAsync(string gameId, string gameVersion, string packPath, int? timeoutSeconds = null, CancellationToken ct = default)
        {
            var form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection(GamesRoutes.GamesUploadRequestFilesFieldName, File.ReadAllBytes(packPath), "pack.zip", "multipart/form-data"),
                new MultipartFormDataSection("gameVersion", gameVersion),
            };
            _ = await SendAsync("Upload server", new EditorApiRequest
            {
                Method = UnityWebRequest.kHttpVerbPOST,
                Url = BuildUrl(GamesRoutes.BaseRoute, gameId, GamesRoutes.GameVersionsRoute),
                Form = form,
                TimeoutSeconds = FileUploadTimeoutSeconds,
            }, true, timeoutSeconds, ct);
        }

        public static async UniTask<int> UploadClientBuildAsync(
            string gameId,
            string clientBuildPath,
            string clientGameVersion,
            string serverGameVersion,
            Action<string, float> progress = null,
            int? timeoutSeconds = null, CancellationToken ct = default)
        {
            var (buildPath, validFiles) = WebGLUploader.ResolveClientBuildFiles(clientBuildPath, clientGameVersion);
            var initRequest = WebGLUploader.CreateInitRequest(gameId, clientGameVersion, serverGameVersion, validFiles);
            var initResponse = await CallAsync<WebGLUploader.UploadInitResponse>("Initialize client build upload", UnityWebRequest.kHttpVerbPOST, initRequest, timeoutSeconds, ct,
                ClientBuildsRoute, ClientBuildInitRoute);

            try
            {
                if (initResponse.Files == null || initResponse.Files.Length != validFiles.Count)
                    throw new ElympicsEditorApiException(EditorApiErrorKind.InvalidResponse,
                        $"Elympics cloud returned {initResponse.Files?.Length ?? 0} upload URLs for {validFiles.Count} client build files.");
                for (var index = 0; index < initResponse.Files.Length; index++)
                {
                    var fileUploadInfo = initResponse.Files[index];
                    var expectedFile = validFiles[index];
                    if (!WebGLUploader.DoesFileHaveGivenCompoundExtension(fileUploadInfo.FilePath, expectedFile.extension))
                        throw new ElympicsException($"Uploaded file '{Sanitize(fileUploadInfo.FilePath)}' does not match expected extension '{expectedFile.extension}'.");

                    var localFile = expectedFile.name + expectedFile.extension;
                    progress?.Invoke(localFile, (float)index / initResponse.Files.Length);
                    var payload = File.ReadAllBytes(Path.Combine(buildPath, localFile));
                    await UploadFileAsync(localFile, fileUploadInfo.SignedUrl, payload, WebGLUploader.FetchContentType(expectedFile.extension), expectedFile.extension, timeoutSeconds, ct);
                }
            }
            catch
            {
                try
                {
                    _ = await CompleteClientBuildUploadAsync(initResponse.UploadId, false, timeoutSeconds, CancellationToken.None);
                }
                catch (Exception e)
                {
                    ElympicsLogger.LogWarning($"Couldn't mark the failed client build upload as failed: {e.Message}");
                }
                throw;
            }

            progress?.Invoke(string.Empty, 1f);
            _ = await CompleteClientBuildUploadAsync(initResponse.UploadId, true, timeoutSeconds, ct);
            return initResponse.Files.Length;
        }

        private static UniTask<EditorApiResponse> CompleteClientBuildUploadAsync(string uploadId, bool success, int? timeoutSeconds, CancellationToken ct) =>
            SendAsync("Complete client build upload", new EditorApiRequest
            {
                Method = UnityWebRequest.kHttpVerbPOST,
                Url = BuildUrl(ClientBuildsRoute, ClientBuildCompleteRoute),
                JsonBody = JsonConvert.SerializeObject(new WebGLUploader.UploadCompleteRequest { uploadId = uploadId, success = success }),
                TimeoutSeconds = JsonTimeoutSeconds,
            }, true, timeoutSeconds, ct);

        public static async UniTask<int> UploadStreamingAssetsAsync(
            string gameId,
            string streamingAssetsPath,
            string version,
            StreamingAssetsLayout layout,
            Action<string, float> progress = null,
            int? timeoutSeconds = null, CancellationToken ct = default)
        {
            WebGLUploader.PrepareStreamingAssetsFiles(streamingAssetsPath, version, layout, out var relativePaths, out var generatedFiles);
            var initRequest = WebGLUploader.CreateStreamingAssetsInitRequest(gameId, version, relativePaths);
            var initResponse = await CallAsync<WebGLUploader.StreamingAssetsUploadInitResponse>("Initialize StreamingAssets upload", UnityWebRequest.kHttpVerbPOST, initRequest, timeoutSeconds, ct,
                ClientBuildsRoute, StreamingAssetsRoute);

            if (initResponse.Files == null || initResponse.Files.Length == 0)
                throw new ElympicsException("Elympics cloud returned no files to upload.");

            var localPaths = new HashSet<string>(relativePaths, StringComparer.Ordinal);
            foreach (var file in initResponse.Files)
                if (file.FilePath == null || !localPaths.Remove(file.FilePath))
                    throw new ElympicsEditorApiException(EditorApiErrorKind.InvalidResponse,
                        $"Elympics cloud requested '{Sanitize(file.FilePath)}', which isn't one of the files to upload or was requested twice.");

            var files = WebGLUploader.SortStreamingAssetsFilesForUpload(initResponse.Files);
            for (var index = 0; index < files.Count; index++)
            {
                var fileUploadInfo = files[index];
                progress?.Invoke(fileUploadInfo.FilePath, (float)index / files.Count);
                if (!generatedFiles.TryGetValue(fileUploadInfo.FilePath, out var payload))
                {
                    var filePath = Path.Combine(streamingAssetsPath, fileUploadInfo.FilePath);
                    if (!File.Exists(filePath))
                        throw new ElympicsException($"File '{fileUploadInfo.FilePath}' requested by Elympics cloud was not found at '{filePath}'.");
                    payload = File.ReadAllBytes(filePath);
                }

                var extension = Path.GetExtension(fileUploadInfo.FilePath);
                await UploadFileAsync(fileUploadInfo.FilePath, fileUploadInfo.SignedUrl, payload, WebGLUploader.FetchStreamingAssetsContentType(extension), extension, timeoutSeconds, ct);
            }

            progress?.Invoke(string.Empty, 1f);
            ElympicsLogger.LogInfo($"StreamingAssets content uploaded successfully - {relativePaths.Count} file(s) as version {version}.");
            return relativePaths.Count;
        }

        private static async UniTask UploadFileAsync(string fileName, string signedUrl, byte[] payload, string contentType, string extension, int? timeoutSeconds, CancellationToken ct)
        {
            if (!Uri.TryCreate(signedUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ElympicsEditorApiException(EditorApiErrorKind.InvalidResponse, $"Elympics cloud returned a non-https upload URL for '{fileName}'.");
            var response = await Transport.SendAsync(new EditorApiRequest
            {
                Method = UnityWebRequest.kHttpVerbPUT,
                Url = signedUrl,
                RawBody = payload,
                ContentType = contentType,
                ContentEncoding = WebGLUploader.FetchEncoding(extension),
                ElympicsApi = false,
                TimeoutSeconds = timeoutSeconds ?? FileUploadTimeoutSeconds,
            }, ct);
            if (!response.Succeeded)
                throw new ElympicsEditorApiException(response.Result == UnityWebRequest.Result.ConnectionError ? EditorApiErrorKind.Network : EditorApiErrorKind.RequestFailed,
                    $"Failed to upload file '{fileName}': {response.Error}\n{Sanitize(response.Text)}", response.StatusCode);
        }

        #endregion

        #region Sending requests and reading responses

        private static async UniTask<T> SendJsonAsync<T>(string action, string method, string url, object body, int? timeoutSeconds, CancellationToken ct) =>
            Deserialize<T>(action, await SendJsonAsync(action, method, url, body, timeoutSeconds, ct));

        private static UniTask<EditorApiResponse> SendJsonAsync(string action, string method, string url, object body, int? timeoutSeconds, CancellationToken ct) =>
            SendAsync(action, new EditorApiRequest
            {
                Method = method,
                Url = url,
                JsonBody = body == null ? null : JsonConvert.SerializeObject(body),
                TimeoutSeconds = JsonTimeoutSeconds,
            }, true, timeoutSeconds, ct);

        private static async UniTask<EditorApiResponse> SendAsync(string action, EditorApiRequest request, bool requiresLogin, int? timeoutSeconds, CancellationToken ct)
        {
            if (requiresLogin)
                await EnsureLoggedInAsync(0, timeoutSeconds, ct);
            if (timeoutSeconds.HasValue)
                request.TimeoutSeconds = timeoutSeconds.Value;
            var response = await Transport.SendAsync(request, ct);
            if (!response.Succeeded)
                throw ToException(action, response);
            return response;
        }

        internal static T Deserialize<T>(string action, EditorApiResponse response)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(response.Text ?? string.Empty)
                    ?? throw new ElympicsEditorApiException(EditorApiErrorKind.InvalidResponse, $"{action} failed: the response was empty.", response.StatusCode);
            }
            catch (JsonException e)
            {
                throw new ElympicsEditorApiException(EditorApiErrorKind.InvalidResponse, $"{action} failed: invalid response ({e.Message}).", response.StatusCode);
            }
        }

        internal static ElympicsEditorApiException ToException(string action, EditorApiResponse response)
        {
            if (response.Result == UnityWebRequest.Result.ConnectionError)
            {
                var kind = response.Error?.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ? EditorApiErrorKind.Timeout : EditorApiErrorKind.Network;
                return new ElympicsEditorApiException(kind, $"{action} failed: {response.Error}", response.StatusCode);
            }

            var errorKind = response.StatusCode switch
            {
                401 => EditorApiErrorKind.Unauthorized,
                403 => EditorApiErrorKind.Forbidden,
                404 => EditorApiErrorKind.NotFound,
                _ => EditorApiErrorKind.RequestFailed,
            };
            return new ElympicsEditorApiException(errorKind, $"{action} failed: {DescribeErrorResponse(response.StatusCode, response.Text)}", response.StatusCode);
        }

        [Serializable]
        private class ErrorModel
        {
            // ReSharper disable InconsistentNaming
            public Dictionary<string, string[]> Errors = new();
            public string Title;
            public string Detail;
            // ReSharper restore InconsistentNaming
        }

        internal static string DescribeErrorResponse(long statusCode, string text)
        {
            if (statusCode == 401)
                return "Not authenticated, please login to your ElympicsWeb account";
            if (statusCode == 403)
                return "This account is not authorized to access the requested resource";

            ErrorModel errorModel = null;
            try
            {
                errorModel = JsonConvert.DeserializeObject<ErrorModel>(text ?? string.Empty);
            }
            catch (JsonException)
            { }

            if (errorModel?.Errors is { Count: > 0 })
            {
                var errors = errorModel.Errors.SelectMany(r => (r.Value ?? Array.Empty<string>()).Select(x => $"[{Sanitize(r.Key)}] {Sanitize(x)}")).ToList();
                var shown = errors.Take(MaxShownErrors);
                if (errors.Count > MaxShownErrors)
                    shown = shown.Append($"(and {errors.Count - MaxShownErrors} more)");
                return $"Received error response code {statusCode} with errors:\n{string.Join("\n", shown)}";
            }
            if (!string.IsNullOrEmpty(errorModel?.Detail ?? errorModel?.Title))
                return $"Received error response code {statusCode}: {Sanitize(errorModel.Detail ?? errorModel.Title)}";
            return $"Received error response code {statusCode} with error:\n{Sanitize(text)}";
        }

        private const int MaxBackendTextLength = 500;
        private const int MaxShownErrors = 10;

        /// <summary>Makes backend text safe to show in logs and CLI results: no control characters (e.g. terminal escapes) and a bounded length.</summary>
        internal static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            var builder = new StringBuilder(Math.Min(text.Length, MaxBackendTextLength));
            foreach (var c in text)
            {
                if (builder.Length == MaxBackendTextLength)
                    return builder.Append("… (truncated)").ToString();
                _ = builder.Append(char.IsControl(c) ? ' ' : c);
            }
            return builder.ToString();
        }

        #endregion
    }
}
