#nullable enable

using System;
using System.Collections;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.Communication;
using Elympics.Editor.UnityCli;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace Elympics.Editor.Tests.UnityCli
{
    [Category("UnityCli")]
    public class TestUploadCommands
    {
        [Test]
        public void UploadServerNeedsConfirm() => CliAssert.ThrowsAsync(() => ElympicsBuildCliCommands.UploadServer(), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void UploadClientBuildNeedsConfirm() =>
            CliAssert.ThrowsAsync(() => ElympicsBuildCliCommands.UploadClientBuild("Builds/WebGL", "1.0"), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void UploadClientBuildValidatesTheVersion() =>
            CliAssert.ThrowsAsync(() => ElympicsBuildCliCommands.UploadClientBuild("Builds/WebGL", "1.0 beta", confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void UploadClientBuildNeedsAConfiguredGame() =>
            CliAssert.ThrowsAsync(() => ElympicsBuildCliCommands.UploadClientBuild("NoSuchBuild_" + Guid.NewGuid().ToString("N"), "1.0", gameId: Guid.NewGuid().ToString(), confirm: true),
                ElympicsConfig.Load() == null ? ElympicsCliErrors.NoConfig : ElympicsCliErrors.GameNotFound);

        [Test]
        public void GameCreateNeedsConfirm() => CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.CreateGame("Game"), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void GameUpdateNeedsAChange() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.UpdateGame(Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [TestCase("")]
        [TestCase("0123456789012345678901234567890123456789012345678901234567890123")]
        public void GameUpdateValidatesTheName(string name) =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.UpdateGame(Guid.NewGuid().ToString(), name, confirm: true), ElympicsCliErrors.InvalidArgument);

        [UnityTest]
        public IEnumerator GameUpdateFailsWithoutChangingWhenTheGameReplyIsNotAnObject([Values("", "[]")] string reply) => UniTask.ToCoroutine(async () =>
        {
            var transport = new FakeTransport { Respond = _ => new EditorApiResponse(200, reply, null, UnityWebRequest.Result.Success) };
            using var scope = new EditorApiScope(new FakeLoginStore(), transport);
            Exception? caught = null;
            try
            {
                _ = await ElympicsBackendWriteCliCommands.UpdateGame(Guid.NewGuid().ToString(), "Renamed", confirm: true).AsUniTask();
            }
            catch (Exception e)
            {
                caught = e;
            }
            Assert.That(caught, Is.Not.Null);
            CliAssert.HasCodeAndFix(caught, ElympicsCliErrors.RequestFailed);
            Assert.That(transport.Requests.ConvertAll(r => r.Method), Is.All.EqualTo("GET"));
        });

        [UnityTest]
        public IEnumerator GameUpdateSendsBackUnchangedFieldsAndReportsUndeclaredOnes() => UniTask.ToCoroutine(async () =>
        {
            const string game = "{\"Id\":\"g1\",\"Name\":\"Old\",\"FleetId\":\"f1\",\"ExternalBackend\":\"https://backend.test\",\"CreatedAt\":\"2026-10-26\"}";
            var transport = new FakeTransport
            {
                Respond = request => new EditorApiResponse(200, request.Method == "GET" ? game : game.Replace("Old", "Renamed"), null, UnityWebRequest.Result.Success),
            };
            using var scope = new EditorApiScope(new FakeLoginStore(), transport);

            var result = JObject.FromObject(await ElympicsBackendWriteCliCommands.UpdateGame(Guid.NewGuid().ToString(), "Renamed", confirm: true).AsUniTask());

            var body = JObject.Parse(transport.Requests.Find(r => r.Method == "PUT")!.JsonBody!);
            Assert.That((string?)body["GameName"], Is.EqualTo("Renamed"));
            Assert.That((string?)body["FleetId"], Is.EqualTo("f1"));
            Assert.That((string?)body["ExternalBackend"], Is.EqualTo("https://backend.test"));
            Assert.That(body.ContainsKey("CreatedAt"), Is.False);
            Assert.That((string?)result["game"]!["Name"], Is.EqualTo("Renamed"));
            Assert.That((string?)result["game"]!["CreatedAt"], Is.EqualTo("2026-10-26"));
        });

        [UnityTest]
        public IEnumerator QueueUpdateKeepsTheSmartContractAndSendsOnlyQueueSettings() => UniTask.ToCoroutine(async () =>
        {
            const string queues = "[{\"Id\":\"q1\",\"Name\":\"solo\",\"MatchType\":\"SOLO\",\"NumberOfTeams\":1,\"PlayersInTeam\":1,\"MaxWaitTime\":10,"
                + "\"SmartContractDeploymentId\":\"sc1\",\"Undeclared\":true}]";
            var transport = new FakeTransport { Respond = request => new EditorApiResponse(200, request.Method == "GET" ? queues : "", null, UnityWebRequest.Result.Success) };
            using var scope = new EditorApiScope(new FakeLoginStore(), transport);

            var result = JObject.FromObject(await ElympicsBackendWriteCliCommands.UpdateQueue("solo", Guid.NewGuid().ToString(), maxWaitTime: 30, confirm: true).AsUniTask());

            var put = transport.Requests.Find(r => r.Method == "PUT");
            Assert.That(put.Url, Does.EndWith("/queues/q1"));
            var body = JObject.Parse(put.JsonBody!);
            Assert.That((int)body["MaxWaitTime"]!, Is.EqualTo(30));
            Assert.That((string?)body["Name"], Is.EqualTo("solo"));
            Assert.That((int)body["PlayersInTeam"]!, Is.EqualTo(1));
            Assert.That((string?)body["SmartContractDeploymentId"], Is.EqualTo("sc1"));
            Assert.That(body.ContainsKey("Id") || body.ContainsKey("Undeclared"), Is.False);
            Assert.That((string?)result["queueId"], Is.EqualTo("q1"));
        });

        [UnityTest]
        public IEnumerator GameCreateWithAConfigFailsWhenTheResponseHasNoGameId() => UniTask.ToCoroutine(async () =>
        {
            var config = ElympicsConfig.Load();
            if (config == null)
                Assert.Ignore("The host project has no ElympicsConfig, and creating one would change the project.");
            var gamesBefore = config!.availableGames.Count;
            using var scope = new EditorApiScope(new FakeLoginStore(), new FakeTransport());
            Exception? caught = null;
            try
            {
                _ = await ElympicsBackendWriteCliCommands.CreateGame("Game", createConfig: true, confirm: true).AsUniTask();
            }
            catch (Exception e)
            {
                caught = e;
            }
            Assert.That(caught, Is.Not.Null);
            CliAssert.HasCodeAndFix(caught, ElympicsCliErrors.RequestFailed);
            Assert.That(config.availableGames, Has.Count.EqualTo(gamesBefore));
        });

        [Test]
        public void GameVersionUpdateNeedsAChange() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.UpdateGameVersion("1.0", Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [UnityTest]
        public IEnumerator GameVersionUpdateReportsTheVersionReadBackAfterwards() => UniTask.ToCoroutine(async () =>
        {
            const string versions = "{\"Versions\":[{\"Id\":\"v1\",\"Version\":\"1.0\",\"Blocked\":false}]}";
            const string version = "{\"Id\":\"v1\",\"Version\":\"1.0\",\"Blocked\":true}";
            const string game = "{\"Id\":\"g1\",\"Name\":\"Game\"}";
            var transport = new FakeTransport
            {
                Respond = request => new EditorApiResponse(200,
                    request.Method == "PUT" ? game : request.Url.EndsWith("/versions") ? versions : version, null, UnityWebRequest.Result.Success),
            };
            using var scope = new EditorApiScope(new FakeLoginStore(), transport);

            var result = JObject.FromObject(await ElympicsBackendWriteCliCommands.UpdateGameVersion("1.0", Guid.NewGuid().ToString(), blocked: true, confirm: true).AsUniTask());

            Assert.That(transport.Requests.ConvertAll(r => r.Method), Is.EqualTo(new[] { "GET", "PUT", "GET" }));
            Assert.That(transport.Requests[2].Url, Does.EndWith("/versions/v1"));
            Assert.That((string?)result["version"]!["Id"], Is.EqualTo("v1"));
            Assert.That((bool)result["version"]!["Blocked"]!, Is.True);
        });

        [Test]
        public void GameVersionDeleteNeedsConfirm() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.DeleteGameVersion("1.0", Guid.NewGuid().ToString()), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void QueueCreateValidatesEnums() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.CreateQueue("solo", "TRIO", 1, 1, Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void QueueCreateNeedsConfirm() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.CreateQueue("solo", "solo", 1, 1, Guid.NewGuid().ToString()), ElympicsCliErrors.ConfirmRequired);

        [TestCase(0, null)]
        [TestCase(null, -1)]
        public void QueueUpdateRejectsNonPositiveTeamSizes(int? numberOfTeams, int? playersInTeam)
        {
            using var scope = new EditorApiScope(new FakeLoginStore(), new FakeTransport());
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.UpdateQueue("solo", Guid.NewGuid().ToString(), numberOfTeams: numberOfTeams, playersInTeam: playersInTeam, confirm: true),
                ElympicsCliErrors.InvalidArgument);
        }

        [Test]
        public void QueueUpdateNeedsAChange() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.UpdateQueue("solo", Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void QueueDeleteNeedsConfirm() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.DeleteQueue("solo", Guid.NewGuid().ToString()), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void StreamingAssetsUploadRejectsCredentials() =>
            CliAssert.ThrowsAsync(() => ElympicsStreamingAssetsCliCommands.UploadStreamingAssets("Assets", "1.0", username: "user", password: "secret", confirm: true),
                ElympicsCliErrors.CredentialsArgsRemoved);

        [Test]
        public void StreamingAssetsUploadNeedsConfirm() =>
            CliAssert.ThrowsAsync(() => ElympicsStreamingAssetsCliCommands.UploadStreamingAssets("Assets", "1.0"), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void StreamingAssetsUploadRejectsADirectoryOutsideTheProject() =>
            CliAssert.ThrowsAsync(() => ElympicsStreamingAssetsCliCommands.UploadStreamingAssets(System.IO.Path.GetTempPath(), "1.0", confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void StreamingAssetsUploadAllowsAnExternalDirectoryWhenAsked() =>
            CliAssert.ThrowsAsync(() => ElympicsStreamingAssetsCliCommands.UploadStreamingAssets(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NoSuchContent_" + Guid.NewGuid().ToString("N")),
                "1.0", confirm: true, allowExternalPath: true), ElympicsCliErrors.BuildDirNotFound);

        [TestCase("../NoSuchBuild")]
        [TestCase("Builds/../../NoSuchBuild")]
        public void UploadClientBuildRejectsADirectoryOutsideTheProject(string path) =>
            CliAssert.ThrowsAsync(() => ElympicsBuildCliCommands.UploadClientBuild(path, "1.0", gameId: Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void StreamingAssetsUploadNeedsAnExistingDirectory() =>
            CliAssert.ThrowsAsync(() => ElympicsStreamingAssetsCliCommands.UploadStreamingAssets("NoSuchContent_" + Guid.NewGuid().ToString("N"), "1.0", confirm: true),
                ElympicsCliErrors.BuildDirNotFound);

        [Test]
        public void GameIdsMustBeGuids() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendCliCommands.ListQueues("not-a-guid"), ElympicsCliErrors.InvalidArgument);
    }
}
