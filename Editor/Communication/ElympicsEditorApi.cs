using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Elympics.Core.Logger;
using Elympics.Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using AuthRoutes = ElympicsApiModels.ApiModels.Auth.Routes;
using GamesRoutes = ElympicsApiModels.ApiModels.Games.Routes;
using RegionsRoutes = ElympicsApiModels.ApiModels.Regions.Routes;

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
    }

    internal sealed class ElympicsEditorApiException : ElympicsException
    {
        public EditorApiErrorKind Kind { get; }
        public long StatusCode { get; }

        public ElympicsEditorApiException(EditorApiErrorKind kind, string message, long statusCode = 0) : base(message)
        {
            Kind = kind;
            StatusCode = statusCode;
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
        private const int JsonTimeoutSeconds = 30;

        internal static IEditorApiTransport Transport = new UnityWebRequestEditorApiTransport();
        internal static IEditorLoginStore LoginStore = new EditorPrefsLoginStore();
        internal static Func<long> UtcNowSeconds = () => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        internal static Func<string> ApiEndpointOverride;

        private static string ApiEndpoint =>
            ApiEndpointOverride?.Invoke()
            ?? ElympicsConfig.Load()?.ElympicsApiEndpoint
            ?? throw new ElympicsException($"No ElympicsConfig found in Resources/{ElympicsConfig.PathInResources}.");

        private static string Url(params string[] segments) => string.Join("/", new[] { ApiEndpoint.TrimEnd('/') }.Concat(segments.Select(Uri.EscapeDataString)));

        #region Login

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

        /// <summary>Refreshes the auth token when it expires within <paramref name="expirationMarginSeconds"/>.</summary>
        /// <remarks>Logs out only when the backend rejects the refresh token, not when it can't be reached.</remarks>
        public static async UniTask EnsureLoggedInAsync(long expirationMarginSeconds = 0, CancellationToken ct = default)
        {
            if (!LoginStore.IsLogin)
                throw new ElympicsEditorApiException(EditorApiErrorKind.NotLoggedIn, "Not logged in to Elympics cloud.");

            if (!long.TryParse(LoginStore.AuthTokenExp, out var expiration))
            {
                LoginStore.IsLogin = false;
                throw new ElympicsEditorApiException(EditorApiErrorKind.NotLoggedIn, "Can't check the auth token's expiration time. Log in to Elympics again.");
            }

            if (UtcNowSeconds() + expirationMarginSeconds <= expiration)
                return;

            ElympicsLogger.LogInfo("Auth token expired. Refreshing using refresh token...");
            var response = await Transport.SendAsync(new EditorApiRequest
            {
                Method = UnityWebRequest.kHttpVerbPOST,
                Url = Url(AuthRoutes.BaseRoute, AuthRoutes.RefreshRoute),
                JsonBody = JsonConvert.SerializeObject(new TokenRefreshingRequestModel { RefreshToken = LoginStore.RefreshToken }),
                Auth = false,
                TimeoutSeconds = JsonTimeoutSeconds,
            }, ct);

            if (!response.Succeeded)
            {
                if (response.IsProtocolError)
                    LoginStore.IsLogin = false;
                var error = ToException("Refresh auth token", response);
                throw response.IsProtocolError
                    ? new ElympicsEditorApiException(EditorApiErrorKind.NotLoggedIn, $"{error.Message} Log in to Elympics again.", response.StatusCode)
                    : error;
            }

            var tokens = Deserialize<ElympicsWebIntegration.RefreshedTokensResponseModel>("Refresh auth token", response);
            LoginStore.AuthToken = tokens.AuthToken;
            LoginStore.AuthTokenExp = ReadTokenExpiration(tokens.AuthToken).ToString();
            LoginStore.RefreshToken = tokens.RefreshToken;
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

        public static UniTask<List<ElympicsWebIntegration.GameResponseModel>> GetGamesAsync(CancellationToken ct = default) =>
            SendJsonAsync<List<ElympicsWebIntegration.GameResponseModel>>("Get games", UnityWebRequest.kHttpVerbGET, Url(GamesRoutes.BaseRoute), null, ct);

        public static UniTask<ElympicsWebIntegration.GameVersionsResponseModel> GetGameVersionsAsync(string gameId, CancellationToken ct = default) =>
            SendJsonAsync<ElympicsWebIntegration.GameVersionsResponseModel>("Get game versions", UnityWebRequest.kHttpVerbGET,
                Url(GamesRoutes.BaseRoute, gameId, GamesRoutes.GameVersionsRoute), null, ct);

        /// <param name="gameId">Empty for every region of the Elympics cloud.</param>
        public static async UniTask<List<RegionResponseModel>> GetRegionsAsync(string gameId, CancellationToken ct = default)
        {
            var route = string.IsNullOrEmpty(gameId) ? RegionsRoutes.AllRegionsRouteUnityFormat : string.Format(RegionsRoutes.RegionForGameIdRoute, gameId);
            var response = await SendAsync("Get regions", new EditorApiRequest { Url = $"{ApiEndpoint.TrimEnd('/')}/{route}", TimeoutSeconds = JsonTimeoutSeconds }, false, ct);
            return Deserialize<AvailableRegionsResponseModel>("Get regions", response).Regions?.ToList() ?? new List<RegionResponseModel>();
        }

        public static UniTask<JToken> GetFleetsAsync(CancellationToken ct = default) =>
            SendJsonAsync<JToken>("Get fleets", UnityWebRequest.kHttpVerbGET, Url(GamesRoutes.BaseRoute, FleetsRoute), null, ct);

        public static UniTask<JToken> CreateGameAsync(string name, string initialRegionName, string fleetId, CancellationToken ct = default) =>
            SendJsonAsync<JToken>("Create game", UnityWebRequest.kHttpVerbPOST, Url(GamesRoutes.BaseRoute),
                new JObject { ["GameName"] = name, ["InitialRegionName"] = initialRegionName, ["FleetId"] = string.IsNullOrEmpty(fleetId) ? null : fleetId }, ct);

        public static UniTask<JToken> GetGameAsync(string gameId, CancellationToken ct = default) =>
            SendJsonAsync<JToken>("Get game", UnityWebRequest.kHttpVerbGET, Url(GamesRoutes.BaseRoute, gameId), null, ct);

        public static UniTask<JToken> UpdateGameAsync(string gameId, JObject body, CancellationToken ct = default) =>
            SendJsonAsync<JToken>("Update game", UnityWebRequest.kHttpVerbPUT, Url(GamesRoutes.BaseRoute, gameId), body, ct);

        public static UniTask<JToken> UpdateGameVersionAsync(string gameId, string versionId, JObject body, CancellationToken ct = default) =>
            SendJsonAsync<JToken>("Update game version", UnityWebRequest.kHttpVerbPUT, Url(GamesRoutes.BaseRoute, gameId, GamesRoutes.GameVersionsRoute, versionId), body, ct);

        public static UniTask<JToken> DeleteGameVersionAsync(string gameId, string versionId, CancellationToken ct = default) =>
            SendJsonAsync<JToken>("Delete game version", UnityWebRequest.kHttpVerbDELETE, Url(GamesRoutes.BaseRoute, gameId, GamesRoutes.GameVersionsRoute, versionId), null, ct);

        public static UniTask<JToken> GetQueuesAsync(string gameId, CancellationToken ct = default) =>
            SendJsonAsync<JToken>("Get queues", UnityWebRequest.kHttpVerbGET, Url(GamesRoutes.BaseRoute, gameId, QueuesRoute), null, ct);

        public static UniTask<JToken> CreateQueueAsync(string gameId, JObject body, CancellationToken ct = default) =>
            SendJsonAsync<JToken>("Create queue", UnityWebRequest.kHttpVerbPOST, Url(GamesRoutes.BaseRoute, gameId, QueuesRoute), body, ct);

        public static UniTask<JToken> UpdateQueueAsync(string gameId, string queueId, JObject body, CancellationToken ct = default) =>
            SendJsonAsync<JToken>("Update queue", UnityWebRequest.kHttpVerbPUT, Url(GamesRoutes.BaseRoute, gameId, QueuesRoute, queueId), body, ct);

        public static UniTask<JToken> DeleteQueueAsync(string gameId, string queueId, CancellationToken ct = default) =>
            SendJsonAsync<JToken>("Delete queue", UnityWebRequest.kHttpVerbDELETE, Url(GamesRoutes.BaseRoute, gameId, QueuesRoute, queueId), null, ct);

        #endregion

        #region Uploads

        /// <summary>Uploads a packed server build as a new game version.</summary>
        public static async UniTask UploadServerAsync(string gameId, string gameVersion, string packPath, CancellationToken ct = default)
        {
            var form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection(GamesRoutes.GamesUploadRequestFilesFieldName, await File.ReadAllBytesAsync(packPath, ct), "pack.zip", "multipart/form-data"),
                new MultipartFormDataSection("gameVersion", gameVersion),
            };
            _ = await SendAsync("Upload server", new EditorApiRequest
            {
                Method = UnityWebRequest.kHttpVerbPOST,
                Url = Url(GamesRoutes.BaseRoute, gameId, GamesRoutes.GameVersionsRoute),
                Form = form,
            }, true, ct);
        }

        /// <returns>The number of uploaded files.</returns>
        public static async UniTask<int> UploadClientBuildAsync(
            string gameId,
            string clientBuildPath,
            string clientGameVersion,
            string serverGameVersion,
            Action<string, float> progress = null,
            CancellationToken ct = default)
        {
            var (buildPath, validFiles) = WebGLUploader.ResolveClientBuildFiles(clientBuildPath, clientGameVersion);
            var initRequest = WebGLUploader.CreateInitRequest(gameId, clientGameVersion, serverGameVersion, validFiles);
            var initResponse = await SendJsonAsync<WebGLUploader.UploadInitResponse>("Initialize client build upload", UnityWebRequest.kHttpVerbPOST,
                $"{ApiEndpoint.TrimEnd('/')}/{ClientBuildsRoute}/init", initRequest, ct);

            try
            {
                for (var index = 0; index < initResponse.Files.Length; index++)
                {
                    var fileUploadInfo = initResponse.Files[index];
                    var expectedFile = validFiles[index];
                    if (!WebGLUploader.DoesFileHaveGivenCompoundExtension(fileUploadInfo.FilePath, expectedFile.extension))
                        throw new ElympicsException($"Uploaded file '{fileUploadInfo.FilePath}' does not match expected extension '{expectedFile.extension}'.");

                    var localFile = expectedFile.name + expectedFile.extension;
                    progress?.Invoke(localFile, (float)index / initResponse.Files.Length);
                    var payload = await File.ReadAllBytesAsync(Path.Combine(buildPath, localFile), ct);
                    await PutToSignedUrlAsync(localFile, fileUploadInfo.SignedUrl, payload, WebGLUploader.FetchContentType(expectedFile.extension),
                        WebGLUploader.FetchEncoding(expectedFile.extension), ct);
                }
            }
            catch
            {
                try
                {
                    _ = await CompleteClientBuildUploadAsync(initResponse.UploadId, false, CancellationToken.None);
                }
                catch (Exception e)
                {
                    ElympicsLogger.LogWarning($"Couldn't mark the failed client build upload as failed: {e.Message}");
                }
                throw;
            }

            progress?.Invoke(string.Empty, 1f);
            _ = await CompleteClientBuildUploadAsync(initResponse.UploadId, true, ct);
            return initResponse.Files.Length;
        }

        private static UniTask<JToken> CompleteClientBuildUploadAsync(string uploadId, bool success, CancellationToken ct) =>
            SendJsonAsync<JToken>("Complete client build upload", UnityWebRequest.kHttpVerbPOST, $"{ApiEndpoint.TrimEnd('/')}/{ClientBuildsRoute}/complete",
                new WebGLUploader.UploadCompleteRequest { uploadId = uploadId, success = success }, ct);

        /// <returns>The number of uploaded files.</returns>
        public static async UniTask<int> UploadStreamingAssetsAsync(
            string gameId,
            string streamingAssetsPath,
            string version,
            StreamingAssetsLayout layout,
            Action<string, float> progress = null,
            CancellationToken ct = default)
        {
            WebGLUploader.PrepareStreamingAssetsFiles(streamingAssetsPath, version, layout, out var relativePaths, out var generatedFiles);
            var initRequest = WebGLUploader.CreateStreamingAssetsInitRequest(gameId, version, relativePaths);
            var initResponse = await SendJsonAsync<WebGLUploader.StreamingAssetsUploadInitResponse>("Initialize StreamingAssets upload", UnityWebRequest.kHttpVerbPOST,
                $"{ApiEndpoint.TrimEnd('/')}/{ClientBuildsRoute}/streaming-assets", initRequest, ct);

            if (initResponse.Files == null || initResponse.Files.Length == 0)
                throw new ElympicsException("Elympics cloud returned no files to upload.");

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
                    payload = await File.ReadAllBytesAsync(filePath, ct);
                }

                var extension = Path.GetExtension(fileUploadInfo.FilePath);
                await PutToSignedUrlAsync(fileUploadInfo.FilePath, fileUploadInfo.SignedUrl, payload, WebGLUploader.FetchStreamingAssetsContentType(extension),
                    WebGLUploader.FetchEncoding(extension), ct);
            }

            progress?.Invoke(string.Empty, 1f);
            ElympicsLogger.LogInfo($"StreamingAssets content uploaded successfully - {relativePaths.Count} file(s) as version {version}.");
            return relativePaths.Count;
        }

        private static async UniTask PutToSignedUrlAsync(string fileName, string signedUrl, byte[] payload, string contentType, string contentEncoding, CancellationToken ct)
        {
            var response = await Transport.SendAsync(new EditorApiRequest
            {
                Method = UnityWebRequest.kHttpVerbPUT,
                Url = signedUrl,
                RawBody = payload,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                ElympicsApi = false,
            }, ct);
            if (!response.Succeeded)
                throw new ElympicsEditorApiException(response.IsConnectionError ? EditorApiErrorKind.Network : EditorApiErrorKind.RequestFailed,
                    $"Failed to upload file '{fileName}': {response.Error}\n{response.Text}", response.StatusCode);
        }

        #endregion

        #region Plumbing

        private static async UniTask<T> SendJsonAsync<T>(string action, string method, string url, object body, CancellationToken ct)
        {
            var response = await SendAsync(action, new EditorApiRequest
            {
                Method = method,
                Url = url,
                JsonBody = body == null ? null : JsonConvert.SerializeObject(body),
                TimeoutSeconds = JsonTimeoutSeconds,
            }, true, ct);
            return Deserialize<T>(action, response);
        }

        private static async UniTask<EditorApiResponse> SendAsync(string action, EditorApiRequest request, bool requiresLogin, CancellationToken ct)
        {
            if (requiresLogin)
                await EnsureLoggedInAsync(0, ct);
            var response = await Transport.SendAsync(request, ct);
            if (!response.Succeeded)
                throw ToException(action, response);
            return response;
        }

        internal static T Deserialize<T>(string action, EditorApiResponse response)
        {
            if (typeof(T) == typeof(JToken))
                return string.IsNullOrWhiteSpace(response.Text) ? default : (T)(object)Parse(action, response.Text);
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

        private static JToken Parse(string action, string text)
        {
            try
            {
                return JToken.Parse(text);
            }
            catch (JsonException e)
            {
                throw new ElympicsEditorApiException(EditorApiErrorKind.InvalidResponse, $"{action} failed: invalid response ({e.Message}).");
            }
        }

        internal static ElympicsEditorApiException ToException(string action, EditorApiResponse response)
        {
            if (response.IsConnectionError)
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
                return $"Received error response code {statusCode} with errors:\n{string.Join("\n", errorModel.Errors.SelectMany(r => (r.Value ?? Array.Empty<string>()).Select(x => $"[{r.Key}] {x}")))}";
            if (!string.IsNullOrEmpty(errorModel?.Detail ?? errorModel?.Title))
                return $"Received error response code {statusCode}: {errorModel.Detail ?? errorModel.Title}";
            return $"Received error response code {statusCode} with error:\n{text}";
        }

        #endregion
    }
}
