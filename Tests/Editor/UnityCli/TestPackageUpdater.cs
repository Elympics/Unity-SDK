#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.UnityCli;
using Elympics.Editor.UnityCli.PackageUpdates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Elympics.Editor.Tests.UnityCli
{
    [Category("UnityCli")]
    public class TestPackageUpdater
    {
        private const string Sdk = "com.daftmobile.elympics";
        private const string PlayPad = "com.daftmobile.elympics-playpad";
        private const string SdkUrl = "https://gitlab.example.com/sdk/unity-sdk-package.git";
        private const string PlayPadUrl = "https://gitlab.example.com/sdk/playpad-sdk.git";
        private static readonly string DevelopHead = new('a', 40);
        private static readonly string OldHead = new('b', 40);
        private static readonly string TagCommit = new('c', 40);

        private sealed class FakeGit : IGitProcess
        {
            public readonly Dictionary<string, string> Outputs = new();
            public readonly Dictionary<string, string> Errors = new();
            public readonly List<string> Requested = new();

            public UniTask<string> ListRemoteRefsAsync(string url, TimeSpan timeout)
            {
                lock (Requested)
                    Requested.Add(url);
                if (Errors.TryGetValue(url, out var error))
                    return UniTask.FromException<string>(new InvalidOperationException(error));
                return Outputs.TryGetValue(url, out var output) ? UniTask.FromResult(output) : UniTask.FromException<string>(new InvalidOperationException("unreachable"));
            }
        }

        private TempDirectory _temp = null!;
        private string _directory = null!;
        private PackageUpdater _updater = null!;
        private FakeGit _git = null!;
        private int _resolves;

        [SetUp]
        public void SetUp()
        {
            _temp = new TempDirectory("ElympicsTestPackageUpdater");
            _directory = _temp.Path;
            _git = new FakeGit();
            _git.Outputs[SdkUrl] = $"{TagCommit}\trefs/tags/v0.28.0\n{TagCommit}\trefs/tags/v0.29.0\n{DevelopHead}\trefs/heads/develop\n";
            _git.Outputs[PlayPadUrl] = $"{TagCommit}\trefs/tags/v3.0.0\n{TagCommit}\trefs/tags/v4.0.0\n";
            _resolves = 0;
            _updater = new PackageUpdater
            {
                ManifestPath = Path.Combine(_directory, "manifest.json"),
                LockPath = Path.Combine(_directory, "packages-lock.json"),
                OperationsDirectory = Path.Combine(_directory, "operations"),
                Git = _git,
                Resolve = () => _resolves++,
                GetInstalledVersion = name => name == Sdk ? "0.28.0" : null,
            };
            WriteManifest(SdkUrl + "#develop", PlayPadUrl + "#v3.0.0");
            WriteLock(new JObject { ["version"] = SdkUrl + "#develop", ["hash"] = OldHead },
                new JObject { ["version"] = PlayPadUrl + "#v3.0.0", ["hash"] = TagCommit });
        }

        private void WriteLock(JObject sdk, JObject playPad) =>
            File.WriteAllText(_updater.LockPath, new JObject { ["dependencies"] = new JObject { [Sdk] = sdk, [PlayPad] = playPad } }.ToString());

        private void WriteLockHashes(string sdkHash, string playPadHash) =>
            WriteLock(new JObject { ["hash"] = sdkHash }, new JObject { ["hash"] = playPadHash });

        [TearDown]
        public void TearDown() => _temp.Dispose();

        private void WriteManifest(string sdk, string playPad) =>
            File.WriteAllText(_updater.ManifestPath, new JObject
            {
                ["dependencies"] = new JObject { [Sdk] = sdk, [PlayPad] = playPad, ["com.unity.ugui"] = "2.0.0", ["com.daftmobile.local"] = "file:../x" },
            }.ToString());

        private static readonly Dictionary<string, string> None = new();

        [UnityTest]
        public IEnumerator PlanCoversOnlyGitElympicsPackages() => UniTask.ToCoroutine(async () =>
        {
            var plan = await _updater.PlanAsync(new List<string>(), None, None);

            Assert.That(plan.Select(update => update.name), Is.EquivalentTo(new[] { Sdk, PlayPad }));
            var sdk = plan.Single(update => update.name == Sdk);
            Assert.That(sdk.action, Is.EqualTo(RefAction.Refresh));
            Assert.That(sdk.newCommit, Is.EqualTo(DevelopHead));
            Assert.That(sdk.oldCommit, Is.EqualTo(OldHead));
            Assert.That(sdk.oldVersion, Is.EqualTo("0.28.0"));
            var playPad = plan.Single(update => update.name == PlayPad);
            Assert.That(playPad.action, Is.EqualTo(RefAction.Move));
            Assert.That(playPad.newSpec, Is.EqualTo(PlayPadUrl + "#v4.0.0"));
            Assert.That(playPad.newVersion, Is.EqualTo("4.0.0"));
        });

        [UnityTest]
        public IEnumerator ABranchAlreadyAtItsHeadIsKept() => UniTask.ToCoroutine(async () =>
        {
            _git.Outputs[SdkUrl] = $"{OldHead}\trefs/heads/develop\n";

            var plan = await _updater.PlanAsync(new List<string> { Sdk }, None, None);

            Assert.That(plan.Single().action, Is.EqualTo(RefAction.Keep));
        });

        [UnityTest]
        public IEnumerator AnUnreachableRemoteIsReportedPerPackage() => UniTask.ToCoroutine(async () =>
        {
            _ = _git.Outputs.Remove(PlayPadUrl);

            var plan = await _updater.PlanAsync(new List<string>(), None, None);

            Assert.That(plan.Single(update => update.name == PlayPad).error!.code, Is.EqualTo(PackageUpdater.RemoteUnreachable));
            Assert.That(plan.Single(update => update.name == Sdk).error, Is.Null);
        });

        [UnityTest]
        public IEnumerator AnUnsupportedRemoteIsNeverPassedToGit() => UniTask.ToCoroutine(async () =>
        {
            WriteManifest("git+ext::sh -c touch% pwned", PlayPadUrl + "#v3.0.0");

            var plan = await _updater.PlanAsync(new List<string>(), None, None);

            Assert.That(plan.Single(update => update.name == Sdk).error!.code, Is.EqualTo(PackageUpdater.UnsupportedRemote));
            Assert.That(_git.Requested, Is.EqualTo(new[] { PlayPadUrl }));
        });

        [UnityTest]
        public IEnumerator CredentialsAreRemovedFromRemoteErrors() => UniTask.ToCoroutine(async () =>
        {
            WriteManifest("https://user:glpat-secret@gitlab.example.com/sdk/unity-sdk-package.git#develop", PlayPadUrl + "#v3.0.0");

            var plan = await _updater.PlanAsync(new List<string>(), None, None);

            var error = plan.Single(update => update.name == Sdk).error;
            Assert.That(error!.code, Is.EqualTo(PackageUpdater.RemoteUnreachable));
            Assert.That(error.message, Does.Not.Contain("glpat-secret").And.Contain("gitlab.example.com"));
        });

        [UnityTest]
        public IEnumerator CredentialsAreRemovedFromGitFailureMessages() => UniTask.ToCoroutine(async () =>
        {
            const string tokenUrl = "https://user:glpat-secret@gitlab.example.com/sdk/unity-sdk-package.git";
            WriteManifest(tokenUrl + "#develop", PlayPadUrl + "#v3.0.0");
            _git.Errors[tokenUrl] = $"git ls-remote exited with 128: fatal: unable to access '{tokenUrl}'";

            var plan = await _updater.PlanAsync(new List<string>(), None, None);

            var error = plan.Single(update => update.name == Sdk).error;
            Assert.That(error!.message, Does.Not.Contain("glpat-secret").And.Contain("gitlab.example.com"));
        });

        [UnityTest]
        public IEnumerator PinAndTrackKeysOutsideTheSelectedPackagesAreRejected() => UniTask.ToCoroutine(async () =>
        {
            CliAssert.HasCodeAndFix(await CatchAsync(() => _updater.PlanAsync(new List<string> { Sdk }, new Dictionary<string, string> { [PlayPad] = "v4.0.0" }, None)),
                ElympicsCliErrors.InvalidArgument);
            CliAssert.HasCodeAndFix(await CatchAsync(() => _updater.PlanAsync(new List<string> { Sdk }, None, new Dictionary<string, string> { [PlayPad] = "develop" })),
                ElympicsCliErrors.InvalidArgument);
        });

        [Test]
        public void ACorruptOperationRecordIsReportedNotThrownAsANullReference()
        {
            _ = Directory.CreateDirectory(_updater.OperationsDirectory);
            File.WriteAllText(Path.Combine(_updater.OperationsDirectory, "nullrecord.json"), "null");
            File.WriteAllText(Path.Combine(_updater.OperationsDirectory, "garbage.json"), "{not json");

            CliAssert.HasCodeAndFix(Assert.Catch(() => _updater.ReadOperation("nullrecord")), ElympicsCliErrors.OperationNotFound);
            CliAssert.HasCodeAndFix(Assert.Catch(() => _updater.ReadOperation("garbage")), ElympicsCliErrors.OperationNotFound);
        }

        [UnityTest]
        public IEnumerator CredentialsStayInTheManifestButNotInResultsOrRecords() => UniTask.ToCoroutine(async () =>
        {
            const string tokenUrl = "https://user:glpat-secret@gitlab.example.com/sdk/playpad-sdk.git";
            _git.Outputs[tokenUrl] = _git.Outputs[PlayPadUrl];
            WriteManifest(SdkUrl + "#develop", tokenUrl + "#v3.0.0");

            var operation = _updater.Apply(await _updater.PlanAsync(new List<string>(), None, None));

            Assert.That(File.ReadAllText(_updater.ManifestPath), Does.Contain(tokenUrl + "#v4.0.0"));
            Assert.That(JsonConvert.SerializeObject(operation), Does.Not.Contain("glpat-secret"));
            Assert.That(Directory.GetFiles(_updater.OperationsDirectory).Select(File.ReadAllText), Has.None.Contain("glpat-secret"));
        });

        [UnityTest]
        public IEnumerator UnknownPackagesAreRejected() => UniTask.ToCoroutine(async () =>
            CliAssert.HasCodeAndFix(await CatchAsync(() => _updater.PlanAsync(new List<string> { "com.daftmobile.local" }, None, None)), ElympicsCliErrors.InvalidArgument));

        private static async UniTask<Exception?> CatchAsync(Func<UniTask> code)
        {
            try
            {
                await code();
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator ApplyRewritesTheManifestDropsLockEntriesAndResolvesOnce() => UniTask.ToCoroutine(async () =>
        {
            var plan = await _updater.PlanAsync(new List<string>(), None, None);

            var operation = _updater.Apply(plan);

            Assert.That(operation.status, Is.EqualTo(PackageUpdateStatus.Started));
            Assert.That(_resolves, Is.EqualTo(1));
            var manifest = JObject.Parse(File.ReadAllText(_updater.ManifestPath));
            Assert.That((string?)manifest["dependencies"]![PlayPad], Is.EqualTo(PlayPadUrl + "#v4.0.0"));
            Assert.That((string?)manifest["dependencies"]!["com.daftmobile.local"], Is.EqualTo("file:../x"));
            var lockFile = JObject.Parse(File.ReadAllText(_updater.LockPath));
            Assert.That(lockFile["dependencies"]![Sdk], Is.Null);
            Assert.That(lockFile["dependencies"]![PlayPad], Is.Null);

            var again = Assert.Catch(() => _updater.Apply(plan));
            CliAssert.HasCodeAndFix(again, ElympicsCliErrors.UpdateInProgress);
            Assert.That(_updater.ReadOperation(operation.operationId).status, Is.EqualTo(PackageUpdateStatus.Started));

            WriteLockHashes(DevelopHead, TagCommit);
            var done = _updater.ReadOperation(operation.operationId);
            Assert.That(done.status, Is.EqualTo(PackageUpdateStatus.Done));
            Assert.That(done.packages.All(update => update.lockHashMatchesHead == true), Is.True);
        });

        [UnityTest]
        public IEnumerator ApplyFinalizesAResolveThatFinishedWithoutADomainReload() => UniTask.ToCoroutine(async () =>
        {
            var first = _updater.Apply(await _updater.PlanAsync(new List<string>(), None, None));
            WriteLockHashes(DevelopHead, TagCommit);

            var second = _updater.Apply(await _updater.PlanAsync(new List<string>(), None, None));

            Assert.That(second.operationId, Is.Not.EqualTo(first.operationId));
            Assert.That(_updater.ReadOperation(first.operationId).status, Is.EqualTo(PackageUpdateStatus.Done));
        });

        [UnityTest]
        public IEnumerator NothingToChangeIsUpToDate() => UniTask.ToCoroutine(async () =>
        {
            WriteManifest(SdkUrl + "#v0.29.0", PlayPadUrl + "#v4.0.0");

            var operation = _updater.Apply(await _updater.PlanAsync(new List<string>(), None, None));

            Assert.That(operation.status, Is.EqualTo(PackageUpdateStatus.UpToDate));
            Assert.That(_resolves, Is.Zero);
        });

        [UnityTest]
        public IEnumerator CorruptOperationRecordsAreSkipped() => UniTask.ToCoroutine(async () =>
        {
            _ = Directory.CreateDirectory(_updater.OperationsDirectory);
            File.WriteAllText(Path.Combine(_updater.OperationsDirectory, "corrupt.json"), "{ not json");
            File.WriteAllText(Path.Combine(_updater.OperationsDirectory, "null.json"), "null");
            File.WriteAllText(Path.Combine(_updater.OperationsDirectory, "escape.json"), "{\"operationId\":\"../escape\",\"status\":\"started\"}");
            File.WriteAllText(Path.Combine(_updater.OperationsDirectory, "renamed.json"), "{\"operationId\":\"other\",\"status\":\"started\"}");
            File.WriteAllText(Path.Combine(_updater.OperationsDirectory, "unnamed.json"),
                "{\"operationId\":\"unnamed\",\"status\":\"started\",\"packages\":[{\"name\":null,\"newSpec\":\"x\"}]}");

            var operation = _updater.Apply(await _updater.PlanAsync(new List<string>(), None, None));

            Assert.That(operation.status, Is.EqualTo(PackageUpdateStatus.Started));
        });

        [Test]
        public void AMissingOperationIsReported() =>
            CliAssert.Throws(() => _updater.ReadOperation("20260101000000-abcdef12"), ElympicsCliErrors.OperationNotFound);
    }
}
