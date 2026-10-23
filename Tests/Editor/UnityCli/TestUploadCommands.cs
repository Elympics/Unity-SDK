using System;
using System.Threading.Tasks;
using Elympics.Editor.UnityCli;
using NUnit.Framework;

namespace Elympics.Editor.Tests.UnityCli
{
    /// <summary>Argument validation only: these commands must fail before any request when arguments or --confirm are missing.</summary>
    [Category("UnityCli")]
    public class TestUploadCommands
    {
        private static void AssertFails(Func<Task<object>> command, string code) => CliAssert.HasCodeAndFix(Assert.CatchAsync(async () => await command()), code);

        [Test]
        public void UploadServerNeedsConfirm() => AssertFails(() => ElympicsBuildCliCommands.UploadServer(), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void UploadClientBuildNeedsConfirm() =>
            AssertFails(() => ElympicsBuildCliCommands.UploadClientBuild("Builds/WebGL", "1.0"), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void UploadClientBuildValidatesTheVersion() =>
            AssertFails(() => ElympicsBuildCliCommands.UploadClientBuild("Builds/WebGL", "1.0 beta", confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void UploadClientBuildNeedsAConfiguredGame() =>
            AssertFails(() => ElympicsBuildCliCommands.UploadClientBuild("NoSuchBuild_" + Guid.NewGuid().ToString("N"), "1.0", gameId: Guid.NewGuid().ToString(), confirm: true),
                ElympicsCliErrors.GameNotFound);

        [Test]
        public void GameCreateNeedsConfirm() => AssertFails(() => ElympicsBackendWriteCliCommands.GameCreate("Game"), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void GameUpdateNeedsAChange() =>
            AssertFails(() => ElympicsBackendWriteCliCommands.GameUpdate(Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void GameVersionUpdateNeedsAChange() =>
            AssertFails(() => ElympicsBackendWriteCliCommands.GameVersionUpdate("1.0", Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void GameVersionDeleteNeedsConfirm() =>
            AssertFails(() => ElympicsBackendWriteCliCommands.GameVersionDelete("1.0", Guid.NewGuid().ToString()), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void QueueCreateValidatesEnums() =>
            AssertFails(() => ElympicsBackendWriteCliCommands.QueueCreate("solo", "TRIO", 1, 1, Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void QueueCreateNeedsConfirm() =>
            AssertFails(() => ElympicsBackendWriteCliCommands.QueueCreate("solo", "solo", 1, 1, Guid.NewGuid().ToString()), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void QueueUpdateNeedsAChange() =>
            AssertFails(() => ElympicsBackendWriteCliCommands.QueueUpdate("solo", Guid.NewGuid().ToString(), confirm: true), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void QueueDeleteNeedsConfirm() =>
            AssertFails(() => ElympicsBackendWriteCliCommands.QueueDelete("solo", Guid.NewGuid().ToString()), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void StreamingAssetsUploadRejectsCredentials() =>
            AssertFails(() => ElympicsStreamingAssetsCliCommands.UploadStreamingAssets("Assets", "1.0", username: "user", password: "secret", confirm: true),
                ElympicsCliErrors.CredentialsArgsRemoved);

        [Test]
        public void StreamingAssetsUploadNeedsConfirm() =>
            AssertFails(() => ElympicsStreamingAssetsCliCommands.UploadStreamingAssets(System.IO.Path.GetTempPath(), "1.0"), ElympicsCliErrors.ConfirmRequired);

        [Test]
        public void StreamingAssetsUploadNeedsAnExistingDirectory() =>
            AssertFails(() => ElympicsStreamingAssetsCliCommands.UploadStreamingAssets("NoSuchContent_" + Guid.NewGuid().ToString("N"), "1.0", confirm: true),
                ElympicsCliErrors.BuildDirNotFound);

        [Test]
        public void GameIdsMustBeGuids() =>
            AssertFails(() => ElympicsBackendCliCommands.QueuesList("not-a-guid"), ElympicsCliErrors.InvalidArgument);
    }
}
