using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.UnityCli;
using Elympics.Editor.UnityCli.PackageUpdates;
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

            public Task<string> LsRemoteAsync(string url, TimeSpan timeout) =>
                Outputs.TryGetValue(url, out var output) ? Task.FromResult(output) : Task.FromException<string>(new InvalidOperationException("unreachable"));
        }

        private string _directory;
        private PackageUpdater _updater;
        private FakeGit _git;
        private int _resolves;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ElympicsTestPackageUpdater_" + Path.GetRandomFileName());
            _ = Directory.CreateDirectory(_directory);
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
                InstalledVersion = name => name == Sdk ? "0.28.0" : null,
            };
            WriteManifest(SdkUrl + "#develop", PlayPadUrl + "#v3.0.0");
            File.WriteAllText(_updater.LockPath, new JObject
            {
                ["dependencies"] = new JObject
                {
                    [Sdk] = new JObject { ["version"] = SdkUrl + "#develop", ["hash"] = OldHead },
                    [PlayPad] = new JObject { ["version"] = PlayPadUrl + "#v3.0.0", ["hash"] = TagCommit },
                },
            }.ToString());
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_directory, true);

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
            Assert.That(sdk.action, Is.EqualTo("refresh"));
            Assert.That(sdk.newCommit, Is.EqualTo(DevelopHead));
            Assert.That(sdk.oldCommit, Is.EqualTo(OldHead));
            Assert.That(sdk.oldVersion, Is.EqualTo("0.28.0"));
            var playPad = plan.Single(update => update.name == PlayPad);
            Assert.That(playPad.action, Is.EqualTo("move"));
            Assert.That(playPad.newSpec, Is.EqualTo(PlayPadUrl + "#v4.0.0"));
            Assert.That(playPad.newVersion, Is.EqualTo("4.0.0"));
        });

        [UnityTest]
        public IEnumerator ABranchAlreadyAtItsHeadIsKept() => UniTask.ToCoroutine(async () =>
        {
            _git.Outputs[SdkUrl] = $"{OldHead}\trefs/heads/develop\n";

            var plan = await _updater.PlanAsync(new List<string> { Sdk }, None, None);

            Assert.That(plan.Single().action, Is.EqualTo("keep"));
        });

        [UnityTest]
        public IEnumerator AnUnreachableRemoteIsReportedPerPackage() => UniTask.ToCoroutine(async () =>
        {
            _ = _git.Outputs.Remove(PlayPadUrl);

            var plan = await _updater.PlanAsync(new List<string>(), None, None);

            Assert.That(plan.Single(update => update.name == PlayPad).error.code, Is.EqualTo(PackageUpdater.RemoteUnreachable));
            Assert.That(plan.Single(update => update.name == Sdk).error, Is.Null);
        });

        [UnityTest]
        public IEnumerator AnUnsupportedRemoteIsNeverPassedToGit() => UniTask.ToCoroutine(async () =>
        {
            WriteManifest("git+ext::sh -c touch% pwned", PlayPadUrl + "#v3.0.0");
            var urls = new List<string>();
            _updater.Git = new RecordingGit(urls, _git);

            var plan = await _updater.PlanAsync(new List<string>(), None, None);

            Assert.That(plan.Single(update => update.name == Sdk).error.code, Is.EqualTo(PackageUpdater.UnsupportedRemote));
            Assert.That(urls, Is.EqualTo(new[] { PlayPadUrl }));
        });

        [UnityTest]
        public IEnumerator CredentialsAreRemovedFromRemoteErrors() => UniTask.ToCoroutine(async () =>
        {
            WriteManifest("https://user:glpat-secret@gitlab.example.com/sdk/unity-sdk-package.git#develop", PlayPadUrl + "#v3.0.0");

            var plan = await _updater.PlanAsync(new List<string>(), None, None);

            var error = plan.Single(update => update.name == Sdk).error;
            Assert.That(error.code, Is.EqualTo(PackageUpdater.RemoteUnreachable));
            Assert.That(error.message, Does.Not.Contain("glpat-secret").And.Contain("gitlab.example.com"));
        });

        private sealed class RecordingGit : IGitProcess
        {
            private readonly List<string> _urls;
            private readonly IGitProcess _inner;

            public RecordingGit(List<string> urls, IGitProcess inner)
            {
                _urls = urls;
                _inner = inner;
            }

            public Task<string> LsRemoteAsync(string url, TimeSpan timeout)
            {
                lock (_urls)
                    _urls.Add(url);
                return _inner.LsRemoteAsync(url, timeout);
            }
        }

        [Test]
        public void UnknownPackagesAreRejected()
        {
            var exception = Assert.CatchAsync(async () => await _updater.PlanAsync(new List<string> { "com.daftmobile.local" }, None, None));
            CliAssert.HasCodeAndFix(exception, ElympicsCliErrors.InvalidArgument);
        }

        [UnityTest]
        public IEnumerator ApplyRewritesTheManifestDropsLockEntriesAndResolvesOnce() => UniTask.ToCoroutine(async () =>
        {
            var plan = await _updater.PlanAsync(new List<string>(), None, None);

            var operation = _updater.Apply(plan);

            Assert.That(operation.status, Is.EqualTo("started"));
            Assert.That(_resolves, Is.EqualTo(1));
            var manifest = JObject.Parse(File.ReadAllText(_updater.ManifestPath));
            Assert.That((string)manifest["dependencies"]![PlayPad], Is.EqualTo(PlayPadUrl + "#v4.0.0"));
            Assert.That((string)manifest["dependencies"]!["com.daftmobile.local"], Is.EqualTo("file:../x"));
            var lockFile = JObject.Parse(File.ReadAllText(_updater.LockPath));
            Assert.That(lockFile["dependencies"]![Sdk], Is.Null);
            Assert.That(lockFile["dependencies"]![PlayPad], Is.Null);

            var again = Assert.Catch(() => _updater.Apply(plan));
            CliAssert.HasCodeAndFix(again, ElympicsCliErrors.UpdateInProgress);
            Assert.That(_updater.ReadOperation(operation.operationId).status, Is.EqualTo("started"));

            File.WriteAllText(_updater.LockPath, new JObject
            {
                ["dependencies"] = new JObject
                {
                    [Sdk] = new JObject { ["hash"] = DevelopHead },
                    [PlayPad] = new JObject { ["hash"] = TagCommit },
                },
            }.ToString());
            var done = _updater.ReadOperation(operation.operationId);
            Assert.That(done.status, Is.EqualTo("done"));
            Assert.That(done.packages.All(update => update.lockHashMatchesHead == true), Is.True);
        });

        [UnityTest]
        public IEnumerator ApplyFinalizesAResolveThatFinishedWithoutADomainReload() => UniTask.ToCoroutine(async () =>
        {
            var first = _updater.Apply(await _updater.PlanAsync(new List<string>(), None, None));
            File.WriteAllText(_updater.LockPath, new JObject
            {
                ["dependencies"] = new JObject
                {
                    [Sdk] = new JObject { ["hash"] = DevelopHead },
                    [PlayPad] = new JObject { ["hash"] = TagCommit },
                },
            }.ToString());

            var second = _updater.Apply(await _updater.PlanAsync(new List<string>(), None, None));

            Assert.That(second.operationId, Is.Not.EqualTo(first.operationId));
            Assert.That(_updater.ReadOperation(first.operationId).status, Is.EqualTo("done"));
        });

        [UnityTest]
        public IEnumerator NothingToChangeIsUpToDate() => UniTask.ToCoroutine(async () =>
        {
            WriteManifest(SdkUrl + "#v0.29.0", PlayPadUrl + "#v4.0.0");

            var operation = _updater.Apply(await _updater.PlanAsync(new List<string>(), None, None));

            Assert.That(operation.status, Is.EqualTo("up_to_date"));
            Assert.That(_resolves, Is.Zero);
        });

        [Test]
        public void AMissingOperationIsReported() =>
            CliAssert.Throws(() => _updater.ReadOperation("20260101000000-abcdef12"), ElympicsCliErrors.OperationNotFound);
    }
}
