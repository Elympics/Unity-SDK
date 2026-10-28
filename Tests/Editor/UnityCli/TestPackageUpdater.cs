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
        private PackageUpdater _updater = null!;
        private FakeGit _git = null!;
        private int _resolves;

        [SetUp]
        public void SetUp()
        {
            _temp = new TempDirectory("ElympicsTestPackageUpdater");
            _git = new FakeGit();
            _git.Outputs[SdkUrl] = $"{TagCommit}\trefs/tags/v0.28.0\n{TagCommit}\trefs/tags/v0.29.0\n{DevelopHead}\trefs/heads/develop\n";
            _git.Outputs[PlayPadUrl] = $"{TagCommit}\trefs/tags/v3.0.0\n{TagCommit}\trefs/tags/v4.0.0\n";
            _resolves = 0;
            _updater = new PackageUpdater
            {
                ManifestPath = Path.Combine(_temp.Path, "manifest.json"),
                LockPath = Path.Combine(_temp.Path, "packages-lock.json"),
                OperationsDirectory = Path.Combine(_temp.Path, "operations"),
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

        private UniTask<List<PackageUpdate>> PlanAllAsync() => _updater.PlanAsync(new List<string>(), None, None);

        [TestCase("../x")]
        [TestCase("a/b")]
        [TestCase("a\\b")]
        [TestCase("/etc/x")]
        [TestCase("C:/x")]
        [TestCase("")]
        [TestCase("  ")]
        public void OperationIdsThatCouldLeaveTheRecordsFolderAreRejected(string operationId) =>
            CliAssert.Throws(() => _updater.ReadOperation(operationId), ElympicsCliErrors.InvalidArgument);

        [UnityTest]
        public IEnumerator AnOperationStaysStartedUntilItIsStale() => UniTask.ToCoroutine(async () =>
        {
            var now = DateTime.UtcNow;
            _updater.GetUtcNow = () => now;
            var operation = _updater.Apply(await PlanAllAsync());

            now += TimeSpan.FromMinutes(9);
            Assert.That(_updater.ReadOperation(operation.operationId).status, Is.EqualTo(PackageUpdateStatus.Started));

            now += TimeSpan.FromMinutes(2);
            var stale = _updater.ReadOperation(operation.operationId);
            Assert.That(stale.status, Is.EqualTo(PackageUpdateStatus.Failed));
            Assert.That(stale.finishedAt, Is.EqualTo(now));
            Assert.That(stale.warnings, Has.Exactly(1).Contain("didn't lock"));
        });

        [UnityTest]
        public IEnumerator AStaleOperationDoesNotBlockTheNextApply() => UniTask.ToCoroutine(async () =>
        {
            var now = DateTime.UtcNow;
            _updater.GetUtcNow = () => now;
            var first = _updater.Apply(await PlanAllAsync());
            now += TimeSpan.FromMinutes(11);

            var second = _updater.Apply(await PlanAllAsync());

            Assert.That(second.operationId, Is.Not.EqualTo(first.operationId));
            Assert.That(_updater.ReadOperation(first.operationId).status, Is.EqualTo(PackageUpdateStatus.Failed));
        });

        [UnityTest]
        public IEnumerator AStalePlanCanStillFinishWhenEveryPackageIsLocked() => UniTask.ToCoroutine(async () =>
        {
            var now = DateTime.UtcNow;
            _updater.GetUtcNow = () => now;
            var operation = _updater.Apply(await PlanAllAsync());
            now += TimeSpan.FromMinutes(11);
            WriteLockHashes(DevelopHead, TagCommit);

            Assert.That(_updater.ReadOperation(operation.operationId).status, Is.EqualTo(PackageUpdateStatus.Done));
        });

        [UnityTest]
        public IEnumerator APinMustBeAVersionTagOrACommit() => UniTask.ToCoroutine(async () =>
        {
            foreach (var pin in new[] { "feature/x", "v0.30.0-rc.1", "" })
                await CliAssert.ThrowsAwaitAsync(async () => _ = await _updater.PlanAsync(new List<string> { Sdk }, new Dictionary<string, string> { [Sdk] = pin }, None),
                    ElympicsCliErrors.InvalidArgument);

            var commit = new string('d', 40);
            var plan = await _updater.PlanAsync(new List<string> { Sdk }, new Dictionary<string, string> { [Sdk] = commit }, None);

            Assert.That(plan.Single().action, Is.EqualTo(RefAction.Move));
            Assert.That(plan.Single().newSpec, Is.EqualTo(SdkUrl + "#" + commit));
        });

        [UnityTest]
        public IEnumerator AnAbbreviatedCommitPinMatchesTheLockedHash() => UniTask.ToCoroutine(async () =>
        {
            var commit = new string('d', 40);
            var operation = _updater.Apply(await _updater.PlanAsync(new List<string> { Sdk }, new Dictionary<string, string> { [Sdk] = commit[..7] }, None));

            WriteLockHashes(commit, TagCommit);
            var sdk = _updater.ReadOperation(operation.operationId).packages.Single(update => update.name == Sdk);

            Assert.That(sdk.lockHashMatchesHead, Is.True);
            Assert.That(sdk.newCommit, Is.EqualTo(commit));
        });

        [UnityTest]
        public IEnumerator ACorruptManifestOrLockIsAPackageManagerError() => UniTask.ToCoroutine(async () =>
        {
            var plan = await PlanAllAsync();
            File.WriteAllText(_updater.LockPath, "{ not json");
            CliAssert.Throws(() => _updater.Apply(plan), ElympicsCliErrors.PackageManagerFailed);

            File.WriteAllText(_updater.ManifestPath, "{ not json");
            await CliAssert.ThrowsAwaitAsync(async () => _ = await PlanAllAsync(), ElympicsCliErrors.PackageManagerFailed);

            foreach (var trailing in new[] { " trailing", " // comment" })
            {
                File.WriteAllText(_updater.ManifestPath, "{ \"dependencies\": {} }" + trailing);
                await CliAssert.ThrowsAwaitAsync(async () => _ = await PlanAllAsync(), ElympicsCliErrors.PackageManagerFailed);
            }
        });

        [UnityTest]
        public IEnumerator ApplyLeavesNoTemporaryFiles() => UniTask.ToCoroutine(async () =>
        {
            _ = _updater.Apply(await PlanAllAsync());

            Assert.That(Directory.GetFiles(_temp.Path, "*.tmp"), Is.Empty);
        });

        [UnityTest]
        public IEnumerator PlanCoversOnlyGitElympicsPackages() => UniTask.ToCoroutine(async () =>
        {
            var plan = await PlanAllAsync();

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

            var plan = await PlanAllAsync();

            Assert.That(plan.Single(update => update.name == PlayPad).error!.code, Is.EqualTo(PackageUpdater.RemoteUnreachable));
            Assert.That(plan.Single(update => update.name == Sdk).error, Is.Null);
        });

        [UnityTest]
        public IEnumerator AnUnsupportedRemoteIsNeverPassedToGit() => UniTask.ToCoroutine(async () =>
        {
            WriteManifest("git+ext::sh -c touch% pwned", PlayPadUrl + "#v3.0.0");

            var plan = await PlanAllAsync();

            Assert.That(plan.Single(update => update.name == Sdk).error!.code, Is.EqualTo(PackageUpdater.UnsupportedRemote));
            Assert.That(_git.Requested, Is.EqualTo(new[] { PlayPadUrl }));
        });

        [UnityTest]
        public IEnumerator CredentialsAreRemovedFromRemoteErrors() => UniTask.ToCoroutine(async () =>
        {
            WriteManifest("https://user:glpat-secret@gitlab.example.com/sdk/unity-sdk-package.git#develop", PlayPadUrl + "#v3.0.0");

            var plan = await PlanAllAsync();

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

            var plan = await PlanAllAsync();

            var error = plan.Single(update => update.name == Sdk).error;
            Assert.That(error!.message, Does.Not.Contain("glpat-secret").And.Contain("gitlab.example.com"));
        });

        [UnityTest]
        public IEnumerator PinAndTrackKeysOutsideTheSelectedPackagesAreRejected() => UniTask.ToCoroutine(async () =>
        {
            await CliAssert.ThrowsAwaitAsync(async () => _ = await _updater.PlanAsync(new List<string> { Sdk }, new Dictionary<string, string> { [PlayPad] = "v4.0.0" }, None),
                ElympicsCliErrors.InvalidArgument);
            await CliAssert.ThrowsAwaitAsync(async () => _ = await _updater.PlanAsync(new List<string> { Sdk }, None, new Dictionary<string, string> { [PlayPad] = "develop" }),
                ElympicsCliErrors.InvalidArgument);
        });

        [Test]
        public void ACorruptOperationRecordIsReportedNotThrownAsANullReference()
        {
            WriteOperationFile("nullrecord", "null");
            WriteOperationFile("garbage", "{not json");

            CliAssert.Throws(() => _updater.ReadOperation("nullrecord"), ElympicsCliErrors.OperationNotFound);
            CliAssert.Throws(() => _updater.ReadOperation("garbage"), ElympicsCliErrors.OperationNotFound);
        }

        [UnityTest]
        public IEnumerator CredentialsStayInTheManifestButNotInResultsOrRecords() => UniTask.ToCoroutine(async () =>
        {
            const string tokenUrl = "https://user:glpat-secret@gitlab.example.com/sdk/playpad-sdk.git";
            _git.Outputs[tokenUrl] = _git.Outputs[PlayPadUrl];
            WriteManifest(SdkUrl + "#develop", tokenUrl + "#v3.0.0");

            var operation = _updater.Apply(await PlanAllAsync());

            Assert.That(File.ReadAllText(_updater.ManifestPath), Does.Contain(tokenUrl + "#v4.0.0"));
            Assert.That(JsonConvert.SerializeObject(operation), Does.Not.Contain("glpat-secret"));
            Assert.That(Directory.GetFiles(_updater.OperationsDirectory).Select(File.ReadAllText), Has.None.Contain("glpat-secret"));
        });

        [UnityTest]
        public IEnumerator UnknownPackagesAreRejected() => UniTask.ToCoroutine(() =>
            CliAssert.ThrowsAwaitAsync(async () => _ = await _updater.PlanAsync(new List<string> { "com.daftmobile.local" }, None, None), ElympicsCliErrors.InvalidArgument));

        [UnityTest]
        public IEnumerator OnlyElympicsGitPackagesCanBeRequested() => UniTask.ToCoroutine(async () =>
        {
            const string other = "com.example.tools";
            File.WriteAllText(_updater.ManifestPath, new JObject
            {
                ["dependencies"] = new JObject { [Sdk] = SdkUrl + "#develop", [other] = "https://gitlab.example.com/tools.git#v1.0.0" },
            }.ToString());

            await CliAssert.ThrowsAwaitAsync(async () => _ = await _updater.PlanAsync(new List<string> { other }, None, None), ElympicsCliErrors.InvalidArgument);
            Assert.That(_git.Requested, Is.Empty);
        });

        [UnityTest]
        public IEnumerator AnApplyWhereEveryPackageFailedIsFailed() => UniTask.ToCoroutine(async () =>
        {
            _ = _git.Outputs.Remove(SdkUrl);
            _ = _git.Outputs.Remove(PlayPadUrl);

            var operation = _updater.Apply(await PlanAllAsync());

            Assert.That(operation.status, Is.EqualTo(PackageUpdateStatus.Failed));
            Assert.That(_resolves, Is.Zero);
        });

        [UnityTest]
        public IEnumerator AStaleOperationKeepsThePlannedVersions() => UniTask.ToCoroutine(async () =>
        {
            var now = DateTime.UtcNow;
            _updater.GetUtcNow = () => now;
            var plan = await PlanAllAsync();
            var planned = plan.Single(update => update.name == Sdk).newVersion;
            var operation = _updater.Apply(plan);
            now += TimeSpan.FromMinutes(11);

            var stale = _updater.ReadOperation(operation.operationId);

            Assert.That(stale.status, Is.EqualTo(PackageUpdateStatus.Failed));
            Assert.That(stale.packages.Single(update => update.name == Sdk).newVersion, Is.EqualTo(planned));
        });

        [Test]
        public void ARecordWithoutListsIsReadable()
        {
            WriteOperationFile("nolists", "{\"operationId\":\"nolists\",\"packages\":null,\"warnings\":null}");

            var operation = _updater.ReadOperation("nolists");

            Assert.That(operation.packages, Is.Empty);
            Assert.That(operation.warnings, Is.Empty);
        }

        [UnityTest]
        public IEnumerator LockEntriesWithoutAStringHashCountAsUnlocked() => UniTask.ToCoroutine(async () =>
        {
            File.WriteAllText(_updater.LockPath, new JObject
            {
                ["dependencies"] = new JObject { [Sdk] = JValue.CreateNull(), [PlayPad] = new JObject { ["hash"] = 1 } },
            }.ToString());

            var plan = await PlanAllAsync();

            Assert.That(plan.Select(update => update.error), Has.All.Null);
            Assert.That(plan.Select(update => update.oldCommit), Has.All.Null);
        });

        [UnityTest]
        public IEnumerator ApplyRewritesTheManifestDropsLockEntriesAndResolvesOnce() => UniTask.ToCoroutine(async () =>
        {
            var plan = await PlanAllAsync();

            var operation = _updater.Apply(plan);

            Assert.That(operation.status, Is.EqualTo(PackageUpdateStatus.Started));
            Assert.That(_resolves, Is.EqualTo(1));
            var manifest = JObject.Parse(File.ReadAllText(_updater.ManifestPath));
            Assert.That((string?)manifest["dependencies"]![PlayPad], Is.EqualTo(PlayPadUrl + "#v4.0.0"));
            Assert.That((string?)manifest["dependencies"]!["com.daftmobile.local"], Is.EqualTo("file:../x"));
            var lockFile = JObject.Parse(File.ReadAllText(_updater.LockPath));
            Assert.That(lockFile["dependencies"]![Sdk], Is.Null);
            Assert.That(lockFile["dependencies"]![PlayPad], Is.Null);

            CliAssert.Throws(() => _updater.Apply(plan), ElympicsCliErrors.UpdateInProgress);
            Assert.That(_updater.ReadOperation(operation.operationId).status, Is.EqualTo(PackageUpdateStatus.Started));

            WriteLockHashes(DevelopHead, TagCommit);
            var done = _updater.ReadOperation(operation.operationId);
            Assert.That(done.status, Is.EqualTo(PackageUpdateStatus.Done));
            Assert.That(done.packages.All(update => update.lockHashMatchesHead == true), Is.True);
        });

        [Test]
        public void ChangeActionsAreMoveAndRefresh()
        {
            Assert.That(new PackageUpdate { action = RefAction.Move }.IsChange(), Is.True);
            Assert.That(new PackageUpdate { action = RefAction.Refresh }.IsChange(), Is.True);
            Assert.That(new PackageUpdate { action = RefAction.Keep }.IsChange(), Is.False);
            Assert.That(new PackageUpdate { action = RefAction.Error }.IsChange(), Is.False);
        }

        [UnityTest]
        public IEnumerator ApplyMarksTheOperationFailedWhenTheResolveThrows() => UniTask.ToCoroutine(async () =>
        {
            var plan = await PlanAllAsync();
            _updater.Resolve = () => throw new InvalidOperationException("resolve failed");

            _ = Assert.Throws<InvalidOperationException>(() => _updater.Apply(plan));

            var record = Directory.GetFiles(_updater.OperationsDirectory, "*.json").Single();
            Assert.That((string?)JObject.Parse(File.ReadAllText(record))["status"], Is.EqualTo("failed"));
        });

        [UnityTest]
        public IEnumerator ApplyFinalizesAResolveThatFinishedWithoutADomainReload() => UniTask.ToCoroutine(async () =>
        {
            var first = _updater.Apply(await PlanAllAsync());
            WriteLockHashes(DevelopHead, TagCommit);

            var second = _updater.Apply(await PlanAllAsync());

            Assert.That(second.operationId, Is.Not.EqualTo(first.operationId));
            Assert.That(_updater.ReadOperation(first.operationId).status, Is.EqualTo(PackageUpdateStatus.Done));
        });

        [UnityTest]
        public IEnumerator NothingToChangeIsUpToDate() => UniTask.ToCoroutine(async () =>
        {
            WriteManifest(SdkUrl + "#v0.29.0", PlayPadUrl + "#v4.0.0");

            var operation = _updater.Apply(await PlanAllAsync());

            Assert.That(operation.status, Is.EqualTo(PackageUpdateStatus.UpToDate));
            Assert.That(_resolves, Is.Zero);
        });

        [UnityTest]
        public IEnumerator CorruptOperationRecordsAreSkipped() => UniTask.ToCoroutine(async () =>
        {
            var records = new Dictionary<string, string>
            {
                ["corrupt"] = "{ not json",
                ["null"] = "null",
                ["escape"] = "{\"operationId\":\"../escape\",\"status\":\"started\"}",
                ["renamed"] = "{\"operationId\":\"other\",\"status\":\"started\"}",
                ["unnamed"] = "{\"operationId\":\"unnamed\",\"status\":\"started\",\"packages\":[{\"name\":null,\"newSpec\":\"x\"}]}",
            };
            foreach (var (operationId, json) in records)
                WriteOperationFile(operationId, json);

            var operation = _updater.Apply(await PlanAllAsync());

            Assert.That(operation.status, Is.EqualTo(PackageUpdateStatus.Started));
            foreach (var (operationId, json) in records)
                Assert.That(File.ReadAllText(Path.Combine(_updater.OperationsDirectory, operationId + ".json")), Is.EqualTo(json), operationId);
        });

        [Test]
        [UnityPlatform(UnityEngine.RuntimePlatform.WindowsEditor)] // Elsewhere FileShare.None doesn't block a rename.
        public void ReplacingAFileWaitsForABriefLock()
        {
            var (source, destination) = WriteReplacePair();
            using var locked = new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.None);
            using var release = new System.Threading.Timer(_ => locked.Dispose(), null, 50, System.Threading.Timeout.Infinite);

            PackageUpdater.ReplaceFile(source, destination);

            Assert.That(File.ReadAllText(destination), Is.EqualTo("new"));
        }

        [Test]
        [UnityPlatform(UnityEngine.RuntimePlatform.WindowsEditor)]
        public void ReplacingAFileThatStaysLockedIsAPackageManagerError()
        {
            var (source, destination) = WriteReplacePair();
            using var locked = new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.None);
            CliAssert.Throws(() => PackageUpdater.ReplaceFile(source, destination), ElympicsCliErrors.PackageManagerFailed);
        }

        private (string source, string destination) WriteReplacePair()
        {
            var source = Path.Combine(_temp.Path, "replace.tmp");
            var destination = Path.Combine(_temp.Path, "replace.json");
            File.WriteAllText(source, "new");
            File.WriteAllText(destination, "old");
            return (source, destination);
        }

        [Test]
        public void AMissingOperationIsReported() =>
            CliAssert.Throws(() => _updater.ReadOperation("20260101000000-abcdef12"), ElympicsCliErrors.OperationNotFound);

        private void WriteOperationFile(string operationId, string json)
        {
            _ = Directory.CreateDirectory(_updater.OperationsDirectory);
            File.WriteAllText(Path.Combine(_updater.OperationsDirectory, operationId + ".json"), json);
        }
    }
}
