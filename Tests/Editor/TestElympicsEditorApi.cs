using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.Communication;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Elympics.Editor.Tests
{
    public class TestElympicsEditorApi
    {
        private const string Api = "https://api.test/v2/api";
        private const long Now = 1_800_000_000;

        private sealed class FakeLoginStore : IEditorLoginStore
        {
            public bool IsLogin { get; set; }
            public string AuthToken { get; set; }
            public string AuthTokenExp { get; set; }
            public string RefreshToken { get; set; }
        }

        private sealed class FakeTransport : IEditorApiTransport
        {
            public readonly List<EditorApiRequest> Requests = new();
            public Func<EditorApiRequest, EditorApiResponse> Respond = _ => Ok("{}");

            public UniTask<EditorApiResponse> SendAsync(EditorApiRequest request, CancellationToken ct)
            {
                Requests.Add(request);
                return UniTask.FromResult(Respond(request));
            }
        }

        private FakeTransport _transport;
        private FakeLoginStore _login;
        private IEditorApiTransport _originalTransport;
        private IEditorLoginStore _originalLogin;
        private Func<long> _originalNow;
        private Func<IEditorApiTransport> _originalBlockingTransportFactory;
        private string _buildDirectory;

        private static EditorApiResponse Ok(string text) => new(200, text, null, false, false);
        private static EditorApiResponse Status(long code, string text) => new(code, text, $"HTTP/1.1 {code}", false, true);
        private static EditorApiResponse ConnectionError(string error) => new(0, null, error, true, false);

        private static string Jwt(long expiration)
        {
            static string Encode(string json) => Convert.ToBase64String(Encoding.ASCII.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"{Encode("{\"alg\":\"none\"}")}.{Encode($"{{\"exp\":{expiration}}}")}.signature";
        }

        [SetUp]
        public void SetUp()
        {
            _originalTransport = ElympicsEditorApi.Transport;
            _originalLogin = ElympicsEditorApi.LoginStore;
            _originalNow = ElympicsEditorApi.UtcNowSeconds;
            _originalBlockingTransportFactory = ElympicsEditorApi.BlockingTransportFactory;
            ElympicsEditorApi.Transport = _transport = new FakeTransport();
            ElympicsEditorApi.BlockingTransportFactory = () => _transport;
            ElympicsEditorApi.LoginStore = _login = new FakeLoginStore { IsLogin = true, AuthToken = "old", AuthTokenExp = (Now + 100).ToString(), RefreshToken = "refresh" };
            ElympicsEditorApi.UtcNowSeconds = () => Now;
            ElympicsEditorApi.ApiEndpointOverride = () => Api;
        }

        [TearDown]
        public void TearDown()
        {
            ElympicsEditorApi.Transport = _originalTransport;
            ElympicsEditorApi.LoginStore = _originalLogin;
            ElympicsEditorApi.UtcNowSeconds = _originalNow;
            ElympicsEditorApi.BlockingTransportFactory = _originalBlockingTransportFactory;
            ElympicsEditorApi.ApiEndpointOverride = null;
            if (_buildDirectory != null && Directory.Exists(_buildDirectory))
                Directory.Delete(_buildDirectory, true);
        }

        private static ElympicsEditorApiException CatchApiException(Func<UniTask> action) =>
            Assert.ThrowsAsync<ElympicsEditorApiException>(async () => await action());

        [UnityTest]
        public IEnumerator GamesAreFetchedWithAValidToken() => UniTask.ToCoroutine(async () =>
        {
            _transport.Respond = _ => Ok("[{\"Id\":\"g1\",\"Name\":\"Game\"}]");

            var games = await ElympicsEditorApi.GetGamesAsync();

            Assert.That(games.Single().Name, Is.EqualTo("Game"));
            Assert.That(_transport.Requests.Single().Url, Is.EqualTo($"{Api}/games"));
            Assert.That(_transport.Requests.Single().Method, Is.EqualTo("GET"));
        });

        [UnityTest]
        public IEnumerator AnExpiredTokenIsRefreshedFirst() => UniTask.ToCoroutine(async () =>
        {
            _login.AuthTokenExp = (Now - 1).ToString();
            var newToken = Jwt(Now + 3600);
            _transport.Respond = request => request.Url.EndsWith("auth/refresh")
                ? Ok($"{{\"AuthToken\":\"{newToken}\",\"RefreshToken\":\"refresh2\"}}")
                : Ok("[]");

            _ = await ElympicsEditorApi.GetGamesAsync();

            Assert.That(_transport.Requests.Select(request => request.Url), Is.EqualTo(new[] { $"{Api}/auth/refresh", $"{Api}/games" }));
            Assert.That(_transport.Requests[0].Auth, Is.False);
            Assert.That(_login.AuthToken, Is.EqualTo(newToken));
            Assert.That(_login.AuthTokenExp, Is.EqualTo((Now + 3600).ToString()));
            Assert.That(_login.RefreshToken, Is.EqualTo("refresh2"));
        });

        [Test]
        public void ARejectedRefreshLogsOut()
        {
            _login.AuthTokenExp = (Now - 1).ToString();
            _transport.Respond = _ => Status(401, "");

            var exception = CatchApiException(() => ElympicsEditorApi.GetGamesAsync());

            Assert.That(exception.Kind, Is.EqualTo(EditorApiErrorKind.NotLoggedIn));
            Assert.That(_login.IsLogin, Is.False);
            Assert.That(_transport.Requests, Has.Count.EqualTo(1));
        }

        [Test]
        public void AnUnreachableBackendDoesNotLogOut()
        {
            _login.AuthTokenExp = (Now - 1).ToString();
            _transport.Respond = _ => ConnectionError("Cannot connect to destination host");

            var exception = CatchApiException(() => ElympicsEditorApi.GetGamesAsync());

            Assert.That(exception.Kind, Is.EqualTo(EditorApiErrorKind.Network));
            Assert.That(_login.IsLogin, Is.True);
        }

        [Test]
        public void NothingIsSentWhenLoggedOut()
        {
            _login.IsLogin = false;

            var exception = CatchApiException(() => ElympicsEditorApi.GetGamesAsync());

            Assert.That(exception.Kind, Is.EqualTo(EditorApiErrorKind.NotLoggedIn));
            Assert.That(_transport.Requests, Is.Empty);
        }

        [TestCase(401, nameof(EditorApiErrorKind.Unauthorized))]
        [TestCase(403, nameof(EditorApiErrorKind.Forbidden))]
        [TestCase(404, nameof(EditorApiErrorKind.NotFound))]
        [TestCase(500, nameof(EditorApiErrorKind.RequestFailed))]
        public void ErrorStatusesHaveKinds(long statusCode, string kind)
        {
            _transport.Respond = _ => Status(statusCode, "");

            Assert.That(CatchApiException(() => ElympicsEditorApi.GetGamesAsync()).Kind.ToString(), Is.EqualTo(kind));
        }

        [Test]
        public void ValidationErrorsAreDescribed()
        {
            _transport.Respond = _ => Status(400, "{\"errors\":{\"Name\":[\"Name is taken.\"]}}");

            var exception = CatchApiException(() => ElympicsEditorApi.CreateQueueAsync("g1", new Newtonsoft.Json.Linq.JObject()));

            Assert.That(exception.Message, Does.Contain("[Name] Name is taken."));
            Assert.That(_transport.Requests.Single().Url, Is.EqualTo($"{Api}/games/g1/queues"));
        }

        [Test]
        public void ValidationErrorsWithoutMessagesAreDescribed()
        {
            _transport.Respond = _ => Status(400, "{\"errors\":{\"Name\":null,\"Size\":[\"Size is too big.\"]}}");

            var exception = CatchApiException(() => ElympicsEditorApi.CreateQueueAsync("g1", new Newtonsoft.Json.Linq.JObject()));

            Assert.That(exception.Message, Does.Contain("[Size] Size is too big."));
        }

        [Test]
        public void ATimeoutIsReportedAsSuch()
        {
            _transport.Respond = _ => ConnectionError("Request timeout");

            Assert.That(CatchApiException(() => ElympicsEditorApi.GetGamesAsync()).Kind, Is.EqualTo(EditorApiErrorKind.Timeout));
        }

        [Test]
        public void LoggingInStoresTheTokens()
        {
            _login.IsLogin = false;
            var token = Jwt(Now + 3600);
            _transport.Respond = _ => Ok($"{{\"UserName\":\"dev\",\"AuthToken\":\"{token}\",\"RefreshToken\":\"refresh2\"}}");

            var userName = ElympicsEditorApi.RunBlocking(ct => ElympicsEditorApi.LoginAsync("dev", "secret", ct));

            Assert.That(userName, Is.EqualTo("dev"));
            Assert.That(_login.IsLogin, Is.True);
            Assert.That(_login.AuthToken, Is.EqualTo(token));
            Assert.That(_login.AuthTokenExp, Is.EqualTo((Now + 3600).ToString()));
            Assert.That(_login.RefreshToken, Is.EqualTo("refresh2"));
            var request = _transport.Requests.Single();
            Assert.That(request.Url, Is.EqualTo($"{Api}/auth/login"));
            Assert.That(request.Auth, Is.False);
            Assert.That(request.JsonBody, Does.Contain("\"UserName\":\"dev\"").And.Contain("\"Password\":\"secret\""));
        }

        [Test]
        public void RejectedCredentialsStoreNothing()
        {
            _login.IsLogin = false;
            _login.AuthToken = null;
            _transport.Respond = _ => Status(401, "");

            _ = CatchApiException(() => ElympicsEditorApi.LoginAsync("dev", "wrong"));

            Assert.That(_login.IsLogin, Is.False);
            Assert.That(_login.AuthToken, Is.Null);
        }

        [UnityTest]
        public IEnumerator EndpointsAreFetched() => UniTask.ToCoroutine(async () =>
        {
            _transport.Respond = _ => Ok("{\"Lobby\":\"https://lobby.test\",\"GameServers\":\"https://servers.test\"}");

            var endpoints = await ElympicsEditorApi.GetEndpointsAsync();

            Assert.That(endpoints.Lobby, Is.EqualTo("https://lobby.test"));
            Assert.That(endpoints.GameServers, Is.EqualTo("https://servers.test"));
            Assert.That(_transport.Requests.Single().Url, Is.EqualTo($"{Api}/users/endpoint"));
        });

        [UnityTest]
        public IEnumerator OnlyEventsNotRequiringALoginAreSentWhenLoggedOut() => UniTask.ToCoroutine(async () =>
        {
            await ElympicsEditorApi.PostUsageEventAsync("start", new { gameId = "g1" }, true);
            _login.IsLogin = false;
            await ElympicsEditorApi.PostUsageEventAsync("start", new { gameId = "g1" }, true);

            await ElympicsEditorApi.PostUsageEventAsync("stop", new { gameId = "g1" }, false);

            Assert.That(_transport.Requests.Select(request => request.Url), Is.EqualTo(new[] { $"{Api}/hello/start", $"{Api}/hello/stop" }));
            Assert.That(_transport.Requests.Select(request => request.Auth), Is.EqualTo(new[] { true, false }));
            Assert.That(_transport.Requests[0].JsonBody, Is.EqualTo("{\"gameId\":\"g1\"}"));
        });

        [UnityTest]
        public IEnumerator RegionsRoutesAreNotEscaped() => UniTask.ToCoroutine(async () =>
        {
            _transport.Respond = _ => Ok("{\"Regions\":[]}");

            _ = await ElympicsEditorApi.GetRegionsAsync(string.Empty);
            _ = await ElympicsEditorApi.GetRegionsAsync("g1");

            Assert.That(_transport.Requests.Select(request => request.Url), Is.EqualTo(new[] { $"{Api}/Regions/unity-format", $"{Api}/Regions/game/g1/unity-format" }));
        });

        [Test]
        public void ABlockingCallReturnsItsResult()
        {
            _transport.Respond = _ => Ok("[{\"Id\":\"g1\",\"Name\":\"Game\"}]");

            var games = ElympicsEditorApi.RunBlocking(ct => ElympicsEditorApi.GetGamesAsync(ct));

            Assert.That(games.Single().Name, Is.EqualTo("Game"));
            Assert.That(_transport.Requests, Has.Count.EqualTo(1));
        }

        [Test]
        public void ABlockingCallPropagatesItsException()
        {
            _transport.Respond = _ => Status(404, "");

            var exception = Assert.Throws<ElympicsEditorApiException>(() => ElympicsEditorApi.RunBlocking(ct => ElympicsEditorApi.GetGamesAsync(ct)));

            Assert.That(exception.Kind, Is.EqualTo(EditorApiErrorKind.NotFound));
        }

        [Test]
        public void ABlockingCallThatAwaitsThePlayerLoopThrows()
        {
            var pending = new UniTaskCompletionSource<int>();
            CancellationToken token = default;

            _ = Assert.Throws<InvalidOperationException>(() => ElympicsEditorApi.RunBlocking(ct =>
            {
                token = ct;
                return pending.Task;
            }));

            Assert.That(token.IsCancellationRequested, Is.True);
        }

        [Test]
        public void ABlockingCallUsesTheBlockingTransportAndRestoresTheTransport()
        {
            var blocking = new FakeTransport();
            ElympicsEditorApi.BlockingTransportFactory = () => blocking;
            IEditorApiTransport transportDuringCall = null;

            ElympicsEditorApi.RunBlocking(ct =>
            {
                transportDuringCall = ElympicsEditorApi.Transport;
                return UniTask.CompletedTask;
            });
            Assert.That(transportDuringCall, Is.SameAs(blocking));
            Assert.That(ElympicsEditorApi.Transport, Is.SameAs(_transport));

            _ = Assert.Throws<InvalidOperationException>(() => ElympicsEditorApi.RunBlocking(ct => new UniTaskCompletionSource<int>().Task));
            Assert.That(ElympicsEditorApi.Transport, Is.SameAs(_transport));

            _transport.Respond = _ => Status(500, "");
            blocking.Respond = _ => Status(500, "");
            _ = Assert.Throws<ElympicsEditorApiException>(() => ElympicsEditorApi.RunBlocking(ct => ElympicsEditorApi.GetGamesAsync(ct)));
            Assert.That(ElympicsEditorApi.Transport, Is.SameAs(_transport));
            Assert.That(_transport.Requests, Is.Empty);
        }

        private string CreateClientBuild()
        {
            _buildDirectory = Path.Combine(Path.GetTempPath(), "ElympicsTestEditorApi_" + Path.GetRandomFileName());
            var buildFolder = Path.Combine(_buildDirectory, WebGLUploader.BuildFolderName);
            _ = Directory.CreateDirectory(buildFolder);
            foreach (var extension in WebGLUploader.CompoundExtensions)
                File.WriteAllText(Path.Combine(buildFolder, "Build" + extension), extension);
            return _buildDirectory;
        }

        private static string InitResponse() =>
            "{\"UploadId\":\"u1\",\"Files\":["
            + string.Join(",", WebGLUploader.CompoundExtensions.OrderBy(extension => "Build" + extension, StringComparer.Ordinal).Select(extension => $"{{\"FilePath\":\"Build{extension}\",\"SignedUrl\":\"https://gcs.test/Build{extension}\"}}"))
            + "]}";

        [UnityTest]
        public IEnumerator AClientBuildIsInitializedUploadedAndCompleted() => UniTask.ToCoroutine(async () =>
        {
            var buildRoot = CreateClientBuild();
            _transport.Respond = request => request.Url.EndsWith("client-builds/init") ? Ok(InitResponse()) : Ok("OK");

            var fileCount = await ElympicsEditorApi.UploadClientBuildAsync("g1", buildRoot, "c1", "s1");

            Assert.That(fileCount, Is.EqualTo(WebGLUploader.CompoundExtensions.Length));
            var puts = _transport.Requests.Where(request => request.Url.StartsWith("https://gcs.test/")).ToList();
            Assert.That(puts, Has.Count.EqualTo(fileCount));
            Assert.That(puts.All(request => request.Method == "PUT" && !request.ElympicsApi), Is.True);
            Assert.That(_transport.Requests.Last().Url, Is.EqualTo($"{Api}/client-builds/complete"));
            Assert.That(_transport.Requests.Last().JsonBody, Does.Contain("\"success\":true"));
        });

        [UnityTest]
        public IEnumerator AFailedFileUploadMarksTheClientBuildUploadAsFailed() => UniTask.ToCoroutine(async () =>
        {
            var buildRoot = CreateClientBuild();
            _transport.Respond = request => request.Url.EndsWith("client-builds/init") ? Ok(InitResponse())
                : request.Url.StartsWith("https://gcs.test/") ? Status(403, "denied")
                : Ok("");

            try
            {
                _ = await ElympicsEditorApi.UploadClientBuildAsync("g1", buildRoot, "c1", "s1");
                Assert.Fail("Expected the upload to fail.");
            }
            catch (ElympicsEditorApiException e)
            {
                Assert.That(e.Message, Does.Contain("Failed to upload file"));
            }

            Assert.That(_transport.Requests.Last().Url, Is.EqualTo($"{Api}/client-builds/complete"));
            Assert.That(_transport.Requests.Last().JsonBody, Does.Contain("\"success\":false"));
        });

        [Test]
        public void AClientBuildIsNotSentToANonHttpsUrl()
        {
            var buildRoot = CreateClientBuild();
            _transport.Respond = request => request.Url.EndsWith("client-builds/init") ? Ok(InitResponse().Replace("https://gcs.test/", "http://gcs.test/")) : Ok("");

            Assert.That(CatchApiException(() => ElympicsEditorApi.UploadClientBuildAsync("g1", buildRoot, "c1", "s1")).Kind, Is.EqualTo(EditorApiErrorKind.InvalidResponse));
            Assert.That(_transport.Requests.Any(request => request.Method == "PUT"), Is.False);
            Assert.That(_transport.Requests.Last().JsonBody, Does.Contain("\"success\":false"));
        }

        [Test]
        public void AClientBuildInitWithTheWrongNumberOfUrlsFails()
        {
            var buildRoot = CreateClientBuild();
            _transport.Respond = request => request.Url.EndsWith("client-builds/init") ? Ok("{\"UploadId\":\"u1\",\"Files\":[]}") : Ok("");

            Assert.That(CatchApiException(() => ElympicsEditorApi.UploadClientBuildAsync("g1", buildRoot, "c1", "s1")).Kind, Is.EqualTo(EditorApiErrorKind.InvalidResponse));
            Assert.That(_transport.Requests.Last().JsonBody, Does.Contain("\"success\":false"));
        }

        private string CreateStreamingAssets()
        {
            _buildDirectory = Path.Combine(Path.GetTempPath(), "ElympicsTestEditorApi_" + Path.GetRandomFileName());
            _ = Directory.CreateDirectory(_buildDirectory);
            File.WriteAllText(Path.Combine(_buildDirectory, "a.txt"), "a");
            return _buildDirectory;
        }

        private static string StreamingAssetsInitResponse(params string[] paths) =>
            "{\"Files\":[" + string.Join(",", paths.Select(path => $"{{\"FilePath\":\"{path}\",\"SignedUrl\":\"https://gcs.test/{path}\"}}")) + "]}";

        [UnityTest]
        public IEnumerator StreamingAssetsUploadOnlyTheRequestedLocalFiles() => UniTask.ToCoroutine(async () =>
        {
            var root = CreateStreamingAssets();
            _transport.Respond = request => request.Method == "PUT" ? Ok("") : Ok(StreamingAssetsInitResponse("a.txt"));

            _ = await ElympicsEditorApi.UploadStreamingAssetsAsync("g1", root, "v1", StreamingAssetsLayout.UnstructuredAssets);

            var put = _transport.Requests.Single(request => request.Method == "PUT");
            Assert.That(put.Url, Is.EqualTo("https://gcs.test/a.txt"));
            Assert.That(put.ContentType, Is.EqualTo("text/plain"));
        });

        [TestCase("../outside.txt")]
        [TestCase("b.txt")]
        [TestCase("a.txt,a.txt")]
        public void StreamingAssetsRequestedOutsideTheLocalFilesAreRejected(string serverPaths)
        {
            var root = CreateStreamingAssets();
            var init = StreamingAssetsInitResponse(serverPaths.Split(','));
            _transport.Respond = request => request.Method == "PUT" ? Ok("") : Ok(init);

            Assert.That(CatchApiException(() => ElympicsEditorApi.UploadStreamingAssetsAsync("g1", root, "v1", StreamingAssetsLayout.UnstructuredAssets)).Kind,
                Is.EqualTo(EditorApiErrorKind.InvalidResponse));
            Assert.That(_transport.Requests.Any(request => request.Method == "PUT"), Is.False);
        }

        [Test]
        public void BackendErrorTextHasNoControlCharactersAndIsTruncated()
        {
            _transport.Respond = _ => Status(500, "\u001b[2J\rcleared" + new string('x', 2000));

            var message = CatchApiException(() => ElympicsEditorApi.GetGamesAsync()).Message;

            Assert.That(message.Any(c => char.IsControl(c) && c != '\n'), Is.False);
            Assert.That(message, Does.Contain("(truncated)"));
            Assert.That(message.Length, Is.LessThan(700));
        }

        [Test]
        public void OnlyTheFirstValidationErrorsAreListed()
        {
            var errors = string.Join(",", Enumerable.Range(0, 15).Select(i => $"\"F{i}\":[\"bad\"]"));
            _transport.Respond = _ => Status(400, $"{{\"errors\":{{{errors}}}}}");

            var message = CatchApiException(() => ElympicsEditorApi.GetGamesAsync()).Message;

            Assert.That(message, Does.Contain("[F9] bad").And.Not.Contain("[F10]").And.Contain("(and 5 more)"));
        }
    }
}
