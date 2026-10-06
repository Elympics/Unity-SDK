#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics.SnapshotAnalysis;
using Elympics.SnapshotAnalysis.Retrievers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Elympics.Tests
{
    [Category("Launcher")]
    public class TestElympicsMatchLauncher
    {
        private const string GameId = "a1b2c3d4-0000-0000-0000-000000000001";
        private const string BootScene = "Assets/Boot.unity";
        private const string GameplayScene = "Assets/Gameplay.unity";

        private sealed class FakeSceneLoader : IElympicsSceneLoader
        {
            public readonly List<string> Loaded = new();
            public UniTaskCompletionSource<bool>? PendingGameplayLoad;
            public bool HoldGameplayLoad;
            public bool GameplayLoadResult = true;
            public string ActiveScenePath { get; set; } = BootScene;
            public int EmptyReturns;

            public async UniTask<bool> LoadSingleAsync(string scenePath)
            {
                Loaded.Add(scenePath);
                if (scenePath == GameplayScene && HoldGameplayLoad)
                {
                    PendingGameplayLoad = new UniTaskCompletionSource<bool>();
                    var result = await PendingGameplayLoad.Task;
                    if (result)
                        ActiveScenePath = scenePath;
                    return result;
                }
                await UniTask.Yield();
                var ok = scenePath != GameplayScene || GameplayLoadResult;
                if (ok)
                    ActiveScenePath = scenePath;
                return ok;
            }

            public bool IsInBuild(string scenePath) => scenePath is BootScene or GameplayScene;

            public async UniTask ReturnToEmptySceneAsync()
            {
                EmptyReturns++;
                await UniTask.Yield();
                ActiveScenePath = "";
            }

            public UniTask ReleaseMemoryAsync() => UniTask.CompletedTask;
        }

        private FakeSceneLoader _loader = null!;
        private ElympicsConfig _config = null!;
        private ElympicsGameConfig _game = null!;
        private ElympicsMatchLauncher _launcher = null!;
        private List<(string Type, string RequestId, string Payload)> _events = null!;
        private List<string> _exits = null!;

        [SetUp]
        public void SetUp()
        {
            _loader = new FakeSceneLoader();
            _game = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            _game.gameId = GameId;
            _game.gameplayScene = GameplayScene;
            _config = ScriptableObject.CreateInstance<ElympicsConfig>();
            _config.availableGames.Add(_game);
            _config.bootScene = BootScene;
            _exits = new List<string>();
            _launcher = new ElympicsMatchLauncher(_loader, () => _config, ElympicsLaunchPlatform.Unrestricted, reason => _exits.Add(reason));
            // A local list: a launcher a test leaves loading must not add its late events to the next test's list.
            var events = _events = new List<(string, string, string)>();
            _launcher.HostEventEmitted += (type, requestId, payload) => events.Add((type, requestId, payload));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
            UnityEngine.Object.DestroyImmediate(_game);
        }

        private static ElympicsLaunchOptions Request(string requestId = "launch", int postMatchTimeout = 0, int launchTimeout = 0) => new()
        {
            RequestId = requestId,
            PostMatchTimeout = postMatchTimeout switch
            {
                0 => null,
                -1 => System.Threading.Timeout.InfiniteTimeSpan,
                _ => TimeSpan.FromSeconds(postMatchTimeout),
            },
            LaunchTimeout = launchTimeout == 0 ? null : TimeSpan.FromSeconds(launchTimeout),
        };

        private UniTask<ElympicsLaunchResult> Launch(ElympicsLaunchOptions options) => _launcher.LaunchSinglePlayerAsync(GameId, null, options);

        private IEnumerable<string> EventTypes => _events.Select(e => e.Type);

        /// <summary>Plays the part of GameSceneManager.Awake.</summary>
        private IElympicsMatchLifecycleSink AttachScene()
        {
            Assert.That(_launcher.AttachGameplayScene(out var session), Is.EqualTo(ElympicsMatchLauncher.SceneAttachment.Attached));
            Assert.That(session, Is.Not.Null);
            return session!;
        }

        private async UniTask<(ElympicsLaunchResult Result, IElympicsMatchLifecycleSink Sink)> LaunchToRunning(ElympicsLaunchOptions? request = null)
        {
            _loader.HoldGameplayLoad = true;
            var launch = Launch(request ?? Request());
            var sink = AttachScene();
            _ = _loader.PendingGameplayLoad!.TrySetResult(true);
            await UniTask.Yield();
            sink.MatchJoined();
            var result = await launch;
            return (result, sink);
        }

        private static async UniTask WaitUntil(Func<bool> condition, float timeoutSeconds = 5)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail("Timed out waiting for condition.");
                await UniTask.Yield();
            }
        }

        [UnityTest]
        public IEnumerator LaunchSucceedsWhenMatchIsJoined() => UniTask.ToCoroutine(async () =>
        {
            var states = new List<MatchLauncherState>();
            _launcher.StateChanged += states.Add;

            var (result, _) = await LaunchToRunning();

            Assert.That(result.Success, Is.True, result.ToString());
            Assert.That(result.RequestId, Is.EqualTo("launch"));
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Running));
            Assert.That(_launcher.IsCurrentlyInMatch, Is.True);
            Assert.That(states, Is.EqualTo(new[] { MatchLauncherState.Loading, MatchLauncherState.Running }));
            Assert.That(_events.Select(e => (e.Type, e.RequestId)), Is.EqualTo(new[] { ("launchAccepted", "launch"), ("launchSucceeded", "launch") }));
            Assert.That(_loader.Loaded, Is.EqualTo(new[] { GameplayScene }));
        });

        [UnityTest]
        public IEnumerator InvalidRequestFailsWithoutLeavingIdle() => UniTask.ToCoroutine(async () =>
        {
            var result = await _launcher.LaunchSinglePlayerAsync(Guid.NewGuid().ToString(), null, Request());

            Assert.That(result.Code, Is.EqualTo(ElympicsLaunchErrorCode.UnknownGame));
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
            Assert.That(EventTypes, Is.EqualTo(new[] { "launchFailed" }));
            Assert.That(_loader.Loaded, Is.Empty);
        });

        [UnityTest]
        public IEnumerator LaunchWhileLoadingIsBusy() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            _ = Launch(Request());

            var second = await Launch(Request("second"));

            Assert.That(second.Code, Is.EqualTo(ElympicsLaunchErrorCode.Busy));
            Assert.That(second.RequestId, Is.EqualTo("second"));
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Loading));
            Assert.That(_events.Last(), Is.EqualTo(("launchFailed", "second", _events.Last().Payload)));
        });

        [UnityTest]
        public IEnumerator LaunchWhileRunningOrEndedIsBusyAndLeavesMatchAlone() => UniTask.ToCoroutine(async () =>
        {
            var (_, sink) = await LaunchToRunning();
            var states = new List<MatchLauncherState>();
            _launcher.StateChanged += states.Add;

            Assert.That((await Launch(Request("r2"))).Code, Is.EqualTo(ElympicsLaunchErrorCode.Busy));
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Running));

            sink.MatchEnded(Guid.Empty);
            Assert.That((await Launch(Request("r3"))).Code, Is.EqualTo(ElympicsLaunchErrorCode.Busy));
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Ended));
            Assert.That(_launcher.IsCurrentlyInMatch, Is.True);
            Assert.That(states, Is.EqualTo(new[] { MatchLauncherState.Ended }));
        });

        [UnityTest]
        public IEnumerator ReturnToBootFromIdleSucceedsImmediately() => UniTask.ToCoroutine(async () =>
        {
            var result = await _launcher.ReturnToBootAsync("return", default);

            Assert.That(result.Success, Is.True);
            Assert.That(_events.Select(e => (e.Type, e.RequestId)), Is.EqualTo(new[] { ("returnToBootAccepted", "abort"), ("idle", "abort") }));
            Assert.That(_loader.Loaded, Is.Empty);
        });

        [UnityTest]
        public IEnumerator ReturnToBootFromRunningLoadsBootScene() => UniTask.ToCoroutine(async () =>
        {
            _ = await LaunchToRunning();
            _events.Clear();

            var result = await _launcher.ReturnToBootAsync("return", default);

            Assert.That(result.Success, Is.True);
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
            Assert.That(_launcher.IsCurrentlyInMatch, Is.False);
            Assert.That(_events.Select(e => (e.Type, e.RequestId)), Is.EqualTo(new[] { ("returnToBootAccepted", "abort"), ("idle", "abort") }));
            Assert.That(_loader.Loaded.Last(), Is.EqualTo(BootScene));
            Assert.That(_launcher.SessionAwaitingScene, Is.Null);
        });

        [UnityTest]
        public IEnumerator TeardownRestoresProcessGlobals() => UniTask.ToCoroutine(async () =>
        {
            var before = Time.fixedDeltaTime;
            try
            {
                _ = await LaunchToRunning();
                Time.fixedDeltaTime = before * 3;

                _ = await _launcher.ReturnToBootAsync("return", default);

                Assert.That(Time.fixedDeltaTime, Is.EqualTo(before));
            }
            finally
            {
                Time.fixedDeltaTime = before;
            }
        });

        [UnityTest]
        public IEnumerator ReplacementSequenceWorks() => UniTask.ToCoroutine(async () =>
        {
            _ = await LaunchToRunning();
            _ = await _launcher.ReturnToBootAsync("return", default);
            _events.Clear();

            var (result, _) = await LaunchToRunning(Request("again"));

            Assert.That(result.Success, Is.True);
            Assert.That(_events.Select(e => (e.Type, e.RequestId)), Is.EqualTo(new[] { ("launchAccepted", "again"), ("launchSucceeded", "again") }));
        });

        [UnityTest]
        public IEnumerator TwoReturnsToBootTearDownOnce() => UniTask.ToCoroutine(async () =>
        {
            _ = await LaunchToRunning();
            _loader.Loaded.Clear();

            var first = _launcher.ReturnToBootAsync("a1", default);
            var second = _launcher.ReturnToBootAsync("a2", default);
            var (r1, r2) = await UniTask.WhenAll(first, second);

            Assert.That(r1.Success && r2.Success, Is.True);
            Assert.That(_loader.Loaded, Is.EqualTo(new[] { BootScene }));
            Assert.That(_events.Where(e => e.Type == "idle").Select(e => e.RequestId), Is.EquivalentTo(new[] { "a1", "a2" }));
        });

        [UnityTest]
        public IEnumerator ReturnToBootDuringLoadingFailsLaunchWithAborted() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            var launch = Launch(Request());
            _ = AttachScene();

            var returnToBoot = _launcher.ReturnToBootAsync("return", default);
            _ = _loader.PendingGameplayLoad!.TrySetResult(true);

            var launchResult = await launch;
            var returnResult = await returnToBoot;

            Assert.That(launchResult.Code, Is.EqualTo(ElympicsLaunchErrorCode.Aborted));
            Assert.That(returnResult.Success, Is.True);
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
            Assert.That(EventTypes.Count(t => t == "launchSucceeded"), Is.Zero);
            Assert.That(_loader.Loaded, Is.EqualTo(new[] { GameplayScene, BootScene }));
        });

        [UnityTest]
        public IEnumerator SceneActivatingDuringTeardownIsSuppressed() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            var launch = Launch(Request());
            var returnToBoot = _launcher.ReturnToBootAsync("return", default);

            // The gameplay scene activates only now, after the return to boot.
            Assert.That(_launcher.AttachGameplayScene(out var session), Is.EqualTo(ElympicsMatchLauncher.SceneAttachment.Suppressed));
            Assert.That(session, Is.Null);
            _ = _loader.PendingGameplayLoad!.TrySetResult(true);

            Assert.That((await launch).Code, Is.EqualTo(ElympicsLaunchErrorCode.Aborted));
            Assert.That((await returnToBoot).Success, Is.True);
            Assert.That(_launcher.AttachGameplayScene(out _), Is.EqualTo(ElympicsMatchLauncher.SceneAttachment.NotLauncherDriven));
        });

        [Test]
        public void SecondAttachOfSameLaunchIsSuppressed()
        {
            _loader.HoldGameplayLoad = true;
            _ = Launch(Request());
            _ = AttachScene();

            Assert.That(_launcher.AttachGameplayScene(out _), Is.EqualTo(ElympicsMatchLauncher.SceneAttachment.Suppressed));
        }

        [Test]
        public void SceneWithoutLauncherMatchIsNotLauncherDriven() =>
            Assert.That(_launcher.AttachGameplayScene(out _), Is.EqualTo(ElympicsMatchLauncher.SceneAttachment.NotLauncherDriven));

        [UnityTest]
        public IEnumerator LateReportsFromTornDownSceneAreIgnored() => UniTask.ToCoroutine(async () =>
        {
            var (_, sink) = await LaunchToRunning();
            _ = await _launcher.ReturnToBootAsync("return", default);
            _events.Clear();

            sink.MatchEnded(Guid.Empty);
            sink.Disconnected(false);

            Assert.That(_events, Is.Empty);
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
        });

        [UnityTest]
        public IEnumerator SceneWithoutGameSceneManagerFails() => UniTask.ToCoroutine(async () =>
        {
            var result = await Launch(Request());

            Assert.That(result.Code, Is.EqualTo(ElympicsLaunchErrorCode.SceneLoadFailed));
            await WaitUntil(() => _launcher.State == MatchLauncherState.Idle);
            Assert.That(_events.Last(), Is.EqualTo(("idle", "launch", "")));
        });

        [UnityTest]
        public IEnumerator SceneThatCannotBeLoadedFails() => UniTask.ToCoroutine(async () =>
        {
            _loader.GameplayLoadResult = false;

            var result = await Launch(Request());

            Assert.That(result.Code, Is.EqualTo(ElympicsLaunchErrorCode.SceneLoadFailed));
            await WaitUntil(() => _launcher.State == MatchLauncherState.Idle);
            // Still in the boot scene: it is not reloaded.
            Assert.That(_loader.Loaded, Is.EqualTo(new[] { GameplayScene }));
        });

        [UnityTest]
        public IEnumerator SceneInitializationFailureFailsLaunch() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            var launch = Launch(Request());
            AttachScene().SceneInitializationFailed(ElympicsLaunchErrorCode.InternalError, "boom");
            _ = _loader.PendingGameplayLoad!.TrySetResult(true);

            var result = await launch;

            Assert.That(result.Code, Is.EqualTo(ElympicsLaunchErrorCode.InternalError));
            await WaitUntil(() => _launcher.State == MatchLauncherState.Idle);
        });

        [UnityTest]
        public IEnumerator JoinFailureIsConnectionFailed() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            var launch = Launch(Request());
            var sink = AttachScene();
            _ = _loader.PendingGameplayLoad!.TrySetResult(true);
            await UniTask.Yield();
            sink.JoinFailed("rejected");

            Assert.That((await launch).Code, Is.EqualTo(ElympicsLaunchErrorCode.ConnectionFailed));
        });

        [UnityTest]
        public IEnumerator LaunchTimeoutFailsAndNextLaunchWorks() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            var launch = Launch(Request(launchTimeout: 1));
            _ = AttachScene();
            _ = _loader.PendingGameplayLoad!.TrySetResult(true);

            Assert.That((await launch).Code, Is.EqualTo(ElympicsLaunchErrorCode.Timeout));
            await WaitUntil(() => _launcher.State == MatchLauncherState.Idle);

            var (result, _) = await LaunchToRunning(Request("next"));
            Assert.That(result.Success, Is.True);
        });

        private static SnapshotAnalysisRetriever Replay(string gameVersion) => new InMemorySnapshotAnalysisRetriever(new SnapshotReplayData
        {
            InitData = new SnapshotSaverInitData("1", "game", GameId, gameVersion, 1, "0", 0.02f, default, null, Array.Empty<InitialMatchPlayerDataGuid>()),
            Snapshots = new Dictionary<long, ElympicsSnapshotWithMetadata>(),
        });

        [UnityTest]
        public IEnumerator PreloadedReplayLaunchUsesTheGivenRetriever() => UniTask.ToCoroutine(async () =>
        {
            var retriever = Replay(_game.GameVersion);
            _loader.HoldGameplayLoad = true;

            var launch = _launcher.LaunchSnapshotReplayAsync(GameId, retriever, Request(), default);
            var session = (ElympicsMatchSession)AttachScene();
            _ = _loader.PendingGameplayLoad!.TrySetResult(true);
            await UniTask.Yield();
            ((IElympicsMatchLifecycleSink)session).MatchJoined();
            var result = await launch;

            Assert.That(result.Success, Is.True, result.ToString());
            Assert.That(session.SnapshotRetriever, Is.SameAs(retriever));
            Assert.That(_loader.Loaded, Is.EqualTo(new[] { GameplayScene }));
        });

        [UnityTest]
        public IEnumerator PreloadedReplayOfAnotherGameVersionIsInvalidPayload() => UniTask.ToCoroutine(async () =>
        {
            var result = await _launcher.LaunchSnapshotReplayAsync(GameId, Replay(_game.GameVersion + "-other"), Request(), default);

            Assert.That(result.Code, Is.EqualTo(ElympicsLaunchErrorCode.InvalidPayload));
            await WaitUntil(() => _launcher.State == MatchLauncherState.Idle);
            Assert.That(_loader.Loaded, Has.No.Member(GameplayScene));
        });

        [UnityTest]
        public IEnumerator PreloadedReplayIsAcceptedOnWebGl() => UniTask.ToCoroutine(async () =>
        {
            var webGlPlayer = new ElympicsLaunchPlatform(canListen: false, canUseTcpUdp: false, hasLocalFiles: false);
            var launcher = new ElympicsMatchLauncher(_loader, () => _config, webGlPlayer, _ => { });

            // No GameSceneManager attaches in this test, so a launch past validation fails at the scene, not before it.
            var result = await launcher.LaunchSnapshotReplayAsync(GameId, Replay(_game.GameVersion), Request(), default);

            Assert.That(result.Code, Is.EqualTo(ElympicsLaunchErrorCode.SceneLoadFailed));
            Assert.That(_loader.Loaded, Does.Contain(GameplayScene));
        });

        [UnityTest]
        public IEnumerator EndedStateWaitsForReturnToBoot() => UniTask.ToCoroutine(async () =>
        {
            var ended = new List<ElympicsMatchEndedInfo>();
            _launcher.MatchEnded += ended.Add;
            var (_, sink) = await LaunchToRunning(Request(postMatchTimeout: -1));

            sink.MatchEnded(Guid.Empty);

            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Ended));
            Assert.That(ended, Has.Count.EqualTo(1));
            Assert.That(EventTypes.Last(), Is.EqualTo("matchEnded"));
            Assert.That(_loader.ActiveScenePath, Is.EqualTo(GameplayScene));

            _ = await _launcher.ReturnToBootAsync("return", default);
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
        });

        [UnityTest]
        public IEnumerator DisconnectEntersEnded() => UniTask.ToCoroutine(async () =>
        {
            var disconnected = new List<ElympicsDisconnectedInfo>();
            _launcher.Disconnected += disconnected.Add;
            var (_, sink) = await LaunchToRunning();

            sink.Disconnected(true);

            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Ended));
            Assert.That(disconnected.Single().byServer, Is.True);
            Assert.That(EventTypes.Last(), Is.EqualTo("disconnected"));
        });

        [UnityTest]
        public IEnumerator PostMatchTimeoutReturnsToBoot() => UniTask.ToCoroutine(async () =>
        {
            var (_, sink) = await LaunchToRunning(Request(postMatchTimeout: 1));
            sink.MatchEnded(Guid.Empty);

            await WaitUntil(() => _launcher.State == MatchLauncherState.Idle);

            var tail = _events.Skip(_events.FindIndex(e => e.Type == "matchEnded")).Select(e => (e.Type, e.RequestId));
            Assert.That(tail, Is.EqualTo(new[] { ("matchEnded", ""), ("matchEndTimedOut", ""), ("idle", "") }));
        });

        [UnityTest]
        public IEnumerator QuitFromRunningEmitsQuittingAndExits() => UniTask.ToCoroutine(async () =>
        {
            _ = await LaunchToRunning();

            _launcher.QuitApplication("test");

            Assert.That(EventTypes.Last(), Is.EqualTo("quitting"));
            Assert.That(_events.Last().Payload, Does.Contain("test"));
            await WaitUntil(() => _exits.Count > 0, ElympicsLaunchDefaults.QuitGraceSeconds + 1);
            Assert.That(_exits, Is.EqualTo(new[] { "test" }));
        });

        [UnityTest]
        public IEnumerator QuitDuringLoadingFailsLaunchFirst() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            var launch = Launch(Request());

            _launcher.QuitApplication();

            Assert.That((await launch).Code, Is.EqualTo(ElympicsLaunchErrorCode.Aborted));
            Assert.That(EventTypes.TakeLast(2), Is.EqualTo(new[] { "launchFailed", "quitting" }));
            await WaitUntil(() => _exits.Count > 0, ElympicsLaunchDefaults.QuitGraceSeconds + 1);
        });

        [Test]
        public void PingRepliesPong()
        {
            _launcher.HandleHostPing("p");
            Assert.That(_events.Single(), Is.EqualTo(("pong", "p", "")));
        }

        [Test]
        public void MalformedHostLaunchIsInvalidPayload()
        {
            _launcher.HandleHostLaunch(ElympicsHostLaunchKind.SinglePlayer, GameId, "{not json", "");
            Assert.That(_events.Single().Type, Is.EqualTo("launchFailed"));
            Assert.That(_events.Single().Payload, Does.Contain("InvalidPayload"));
        }

        [Test]
        public void MalformedHostLaunchOptionsAreInvalidPayload()
        {
            _launcher.HandleHostLaunch(ElympicsHostLaunchKind.SinglePlayer, GameId, "", "{\"launchTimeoutSeconds\": -5}");
            Assert.That(_events.Single().Type, Is.EqualTo("launchFailed"));
            Assert.That(_events.Single().Payload, Does.Contain("InvalidPayload"));
        }

        [UnityTest]
        public IEnumerator HostLaunchWithEmptyDataStartsSinglePlayer() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            _launcher.HandleHostLaunch(ElympicsHostLaunchKind.SinglePlayer, GameId, "", "{\"requestId\": \"host\"}");
            await WaitUntil(() => _launcher.State == MatchLauncherState.Loading);

            Assert.That(_events.Select(e => (e.Type, e.RequestId)), Is.EqualTo(new[] { ("launchAccepted", "host") }));
            Assert.That(_launcher.SessionAwaitingScene!.Launch.Data, Is.InstanceOf<SinglePlayerLaunchData>());
        });

        // Command kinds by value: the test method is public, the enums internal.
        [TestCase(6, "Online")]
        [TestCase(7, "SinglePlayer")]
        [TestCase(8, "HalfRemoteClient")]
        [TestCase(9, "HalfRemoteServer")]
        [TestCase(10, "SnapshotReplay")]
        public void LaunchCommandKindsRouteToTheirMode(int kind, string mode)
        {
            // Empty data is invalid for Online and SnapshotReplay, and valid for the others: either way the command reaches its mode.
            _launcher.HandleHostCommand((ElympicsHostCommandKind)kind, GameId, "", "{\"requestId\": \"cmd\"}");

            var first = _events.First();
            Assert.That(first.RequestId, Is.EqualTo("cmd"));
            if (first.Type == "launchAccepted")
                Assert.That(_launcher.SessionAwaitingScene!.Launch.Data.ModeName, Is.EqualTo(mode));
            else
                Assert.That(first.Payload, Does.Contain("InvalidPayload"));
        }

        [Test]
        public void RemovedAbortMatchKindIsIgnored()
        {
            LogAssert.ignoreFailingMessages = true;
            _launcher.HandleHostCommand((ElympicsHostCommandKind)2, "abort");
            Assert.That(_events, Is.Empty);
        }

        [Test]
        public void RemovedGenericLaunchKindIsIgnored()
        {
            LogAssert.ignoreFailingMessages = true;
            _launcher.HandleHostCommand((ElympicsHostCommandKind)1, "{}");
            Assert.That(_events, Is.Empty);
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
        }

        [Test]
        public void ControlCommandsTakeTheirFirstString()
        {
            _launcher.HandleHostCommand(ElympicsHostCommandKind.Ping, "p");
            Assert.That(_events.Single(), Is.EqualTo(("pong", "p", "")));
        }

        [Test]
        public void ReadyListsGameIds()
        {
            _launcher.EmitReady();
            var ready = JsonUtility.FromJson<ElympicsReadyInfo>(_events.Single().Payload);
            Assert.That(ready.gameIds, Is.EqualTo(new[] { GameId }));
            Assert.That(ready.protocolVersion, Is.EqualTo(ElympicsHostProtocol.Version));
        }

        [Test]
        public void ValidBootSceneHasNoWarnings()
        {
            _launcher.EmitReady();
            var ready = JsonUtility.FromJson<ElympicsReadyInfo>(_events.Single().Payload);
            Assert.That(_launcher.Boot.ScenePath, Is.EqualTo(BootScene));
            Assert.That(ready.warnings, Is.Empty);
        }

        private static readonly (string BootScene, string WarningFragment)[] UnusableBootScenes =
        {
            ("", "Set ElympicsConfig > Boot scene to return to your own scene"),
            ("Assets/NotInBuild.unity", "not in Build Settings"),
            (GameplayScene, "also a gameplay scene"),
        };

        [UnityTest]
        public IEnumerator UnusableBootSceneReturnsToEmptySceneWithWarning([Values(0, 1, 2)] int caseIndex) => UniTask.ToCoroutine(async () =>
        {
            var (bootScene, fragment) = UnusableBootScenes[caseIndex];
            _config.bootScene = bootScene;

            _launcher.EmitReady();
            var ready = JsonUtility.FromJson<ElympicsReadyInfo>(_events.Single().Payload);
            Assert.That(_launcher.Boot.ScenePath, Is.Null);
            Assert.That(ready.warnings, Has.Length.EqualTo(1));
            Assert.That(ready.warnings[0], Does.Contain(fragment));

            _ = await LaunchToRunning();
            _loader.Loaded.Clear();
            _ = await _launcher.ReturnToBootAsync("return", default);

            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
            Assert.That(_loader.EmptyReturns, Is.EqualTo(1));
            Assert.That(_loader.Loaded, Is.Empty);
        });
    }
}
