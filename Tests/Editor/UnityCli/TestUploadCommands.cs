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
        private FakeTransport _transport = null!;
        private EditorApiScope _scope = null!;

        [SetUp]
        public void SetUp()
        {
            _transport = new FakeTransport();
            _scope = new EditorApiScope(new FakeLoginStore(), _transport);
        }

        [TearDown]
        public void TearDown()
        {
            _scope.Dispose();
            Assert.That(_transport.Requests, Is.Empty, "A test without its own transport must not reach the backend.");
        }

        [Test]
        public void UploadServerNeedsConfirm() => CliAssert.ThrowsAsync(() => ElympicsBuildCliCommands.UploadServer(), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void GameCreateNeedsConfirm() => CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.CreateGame("Game"), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void GameUpdateNeedsAChange() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.UpdateGame(Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [TestCase("")]
        [TestCase("0123456789012345678901234567890123456789012345678901234567890123")]
        public void GameUpdateValidatesTheName(string name) =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.UpdateGame(Guid.NewGuid().ToString(), name, confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void GameUpdateRejectsAnEmptyFleetId() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.UpdateGame(Guid.NewGuid().ToString(), fleetId: " ", confirm: true), ElympicsCliErrors.InvalidArgument);

        [UnityTest]
        public IEnumerator GameUpdateSendsOnlyTheGivenFieldsAndReportsUndeclaredOnes() => UniTask.ToCoroutine(async () =>
        {
            const string game = "{\"Id\":\"g1\",\"Name\":\"Renamed\",\"FleetId\":\"f1\",\"CreatedAt\":\"2026-10-26\"}";
            var transport = new FakeTransport { Respond = _ => new EditorApiResponse(200, game, null, UnityWebRequest.Result.Success) };
            using var scope = new EditorApiScope(new FakeLoginStore(), transport);

            var result = JObject.FromObject(await ElympicsBackendWriteCliCommands.UpdateGame(Guid.NewGuid().ToString(), "Renamed", externalBackend: "", confirm: true).AsUniTask());

            Assert.That(transport.Requests.ConvertAll(r => r.Method), Is.EqualTo(new[] { "PUT" }));
            var body = JObject.Parse(transport.Requests[0].JsonBody!);
            Assert.That((string?)body["GameName"], Is.EqualTo("Renamed"));
            Assert.That((string?)body["ExternalBackend"], Is.Empty);
            Assert.That(body["FleetId"]!.Type, Is.EqualTo(JTokenType.Null));
            Assert.That(body["EventQueueJoinUrl"]!.Type, Is.EqualTo(JTokenType.Null));
            Assert.That((string?)result["game"]!["CreatedAt"], Is.EqualTo("2026-10-26"));
        });

        private const string BotQueue = "[{\"Id\":\"q1\",\"Name\":\"solo\",\"MatchType\":\"SOLO\",\"NumberOfTeams\":1,\"PlayersInTeam\":1,\"MaxWaitTime\":10,"
            + "\"MinPlayersToMatch\":1,\"SmartContractDeploymentId\":\"sc1\",\"Undeclared\":true}]";

        [UnityTest]
        public IEnumerator QueueUpdateSendsOnlyTheGivenFields() => UniTask.ToCoroutine(async () =>
        {
            var transport = new FakeTransport { Respond = request => new EditorApiResponse(200, request.Method == "GET" ? BotQueue : "", null, UnityWebRequest.Result.Success) };
            using var scope = new EditorApiScope(new FakeLoginStore(), transport);

            var result = JObject.FromObject(await ElympicsBackendWriteCliCommands.UpdateQueue("solo", Guid.NewGuid().ToString(), maxWaitTime: 30, confirm: true).AsUniTask());

            var put = transport.Requests.Find(r => r.Method == "PUT");
            Assert.That(put.Url, Does.EndWith("/queues/q1"));
            var body = JObject.Parse(put.JsonBody!);
            Assert.That((int)body["MaxWaitTime"]!, Is.EqualTo(30));
            Assert.That(body["Name"]!.Type, Is.EqualTo(JTokenType.Null));
            Assert.That(body["MinPlayersToMatch"]!.Type, Is.EqualTo(JTokenType.Null));
            Assert.That(body.ContainsKey("SmartContractDeploymentId") || body.ContainsKey("Id") || body.ContainsKey("Undeclared"), Is.False);
            Assert.That((string?)result["queueId"], Is.EqualTo("q1"));
        });

        [UnityTest]
        public IEnumerator QueueUpdateSendsTheCurrentMinPlayersWithMatchWithBots() => UniTask.ToCoroutine(async () =>
        {
            var transport = new FakeTransport { Respond = request => new EditorApiResponse(200, request.Method == "GET" ? BotQueue : "", null, UnityWebRequest.Result.Success) };
            using var scope = new EditorApiScope(new FakeLoginStore(), transport);

            _ = await ElympicsBackendWriteCliCommands.UpdateQueue("q1", Guid.NewGuid().ToString(), matchWithBots: true, confirm: true).AsUniTask();

            var body = JObject.Parse(transport.Requests.Find(r => r.Method == "PUT")!.JsonBody!);
            Assert.That((bool)body["MatchWithBots"]!, Is.True);
            Assert.That((int)body["MinPlayersToMatch"]!, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator GameCreateWithAConfigFailsWhenTheResponseHasNoGameId() => UniTask.ToCoroutine(async () =>
        {
            using var configPath = new ConfigPathScope("ElympicsTests/ElympicsConfig");
            var config = ElympicsConfig.Load()!;
            var gamesBefore = config.availableGames.Count;
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
        public void GameIdsMustBeGuids() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendCliCommands.ListQueues("not-a-guid"), ElympicsCliErrors.InvalidArgument);
    }
}
