using System;
using System.Collections;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.Communication;
using Elympics.Editor.UnityCli;
using NUnit.Framework;
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
        public void GameCreateNeedsConfirm() => CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.GameCreate("Game"), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void GameUpdateNeedsAChange() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.GameUpdate(Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [TestCase("")]
        [TestCase("0123456789012345678901234567890123456789012345678901234567890123")]
        public void GameUpdateValidatesTheName(string name) =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.GameUpdate(Guid.NewGuid().ToString(), name, confirm: true), ElympicsCliErrors.InvalidArgument);

        [UnityTest]
        public IEnumerator GameUpdateFailsWithoutChangingWhenTheGameReplyIsNotAnObject([Values("", "[]")] string reply) => UniTask.ToCoroutine(async () =>
        {
            var transport = new FakeTransport { Respond = _ => new EditorApiResponse(200, reply, null, false, false) };
            using var scope = new EditorApiScope(new FakeLoginStore(), transport);
            Exception caught = null;
            try
            {
                _ = await ElympicsBackendWriteCliCommands.GameUpdate(Guid.NewGuid().ToString(), "Renamed", confirm: true).AsUniTask();
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
        public IEnumerator GameCreateWithAConfigFailsWhenTheResponseHasNoGameId() => UniTask.ToCoroutine(async () =>
        {
            var config = ElympicsConfig.Load();
            if (config == null)
                Assert.Ignore("The host project has no ElympicsConfig, and creating one would change the project.");
            var gamesBefore = config.availableGames.Count;
            using var scope = new EditorApiScope(new FakeLoginStore(), new FakeTransport());
            Exception caught = null;
            try
            {
                _ = await ElympicsBackendWriteCliCommands.GameCreate("Game", createConfig: true, confirm: true).AsUniTask();
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
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.GameVersionUpdate("1.0", Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void GameVersionDeleteNeedsConfirm() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.GameVersionDelete("1.0", Guid.NewGuid().ToString()), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void QueueCreateValidatesEnums() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.QueueCreate("solo", "TRIO", 1, 1, Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void QueueCreateNeedsConfirm() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.QueueCreate("solo", "solo", 1, 1, Guid.NewGuid().ToString()), ElympicsCliErrors.ConfirmRequired);

        [TestCase(0, null)]
        [TestCase(null, -1)]
        public void QueueUpdateRejectsNonPositiveTeamSizes(int? numberOfTeams, int? playersInTeam)
        {
            using var scope = new EditorApiScope(new FakeLoginStore(), new FakeTransport());
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.QueueUpdate("solo", Guid.NewGuid().ToString(), numberOfTeams: numberOfTeams, playersInTeam: playersInTeam, confirm: true),
                ElympicsCliErrors.InvalidArgument);
        }

        [Test]
        public void QueueUpdateNeedsAChange() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.QueueUpdate("solo", Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void QueueDeleteNeedsConfirm() =>
            CliAssert.ThrowsAsync(() => ElympicsBackendWriteCliCommands.QueueDelete("solo", Guid.NewGuid().ToString()), ElympicsCliErrors.ConfirmRequired);

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
            CliAssert.ThrowsAsync(() => ElympicsBackendCliCommands.QueuesList("not-a-guid"), ElympicsCliErrors.InvalidArgument);
    }
}
