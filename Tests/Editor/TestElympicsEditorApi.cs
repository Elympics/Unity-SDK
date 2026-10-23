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
            ElympicsEditorApi.Transport = _transport = new FakeTransport();
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
            _transport.Respond = request => request.Url.EndsWith("client-builds/init") ? Ok(InitResponse()) : Ok("");

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
    }
}
