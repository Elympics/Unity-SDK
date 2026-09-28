#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
            _launcher = new ElympicsMatchLauncher(_loader, () => _config, true, reason => _exits.Add(reason));
            _events = new List<(string, string, string)>();
            _launcher.HostEventEmitted += (type, requestId, payload) => _events.Add((type, requestId, payload));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
            UnityEngine.Object.DestroyImmediate(_game);
        }

        private static ElympicsLaunchRequest Request(string requestId = "launch", int postMatchTimeout = 0, int launchTimeout = 0) => new()
        {
            requestId = requestId,
            gameId = GameId,
            matchMode = "SinglePlayer",
            postMatchTimeoutSeconds = postMatchTimeout,
            launchTimeoutSeconds = launchTimeout,
        };

        private IEnumerable<string> EventTypes => _events.Select(e => e.Type);

        /// <summary>Plays the part of GameSceneManager.Awake.</summary>
        private IElympicsMatchLifecycleSink AttachScene()
        {
            Assert.That(_launcher.AttachGameplayScene(out var session), Is.EqualTo(ElympicsMatchLauncher.SceneAttachment.Attached));
            Assert.That(session, Is.Not.Null);
            return session!;
        }

        private async UniTask<(ElympicsLaunchResult Result, IElympicsMatchLifecycleSink Sink)> LaunchToRunning(ElympicsLaunchRequest? request = null)
        {
            _loader.HoldGameplayLoad = true;
            var launch = _launcher.LaunchMatchAsync(request ?? Request());
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
            var request = Request();
            request.gameId = Guid.NewGuid().ToString();

            var result = await _launcher.LaunchMatchAsync(request);

            Assert.That(result.Code, Is.EqualTo(ElympicsLaunchErrorCode.UnknownGame));
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
            Assert.That(EventTypes, Is.EqualTo(new[] { "launchFailed" }));
            Assert.That(_loader.Loaded, Is.Empty);
        });

        [UnityTest]
        public IEnumerator LaunchWhileLoadingIsBusy() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            _ = _launcher.LaunchMatchAsync(Request());

            var second = await _launcher.LaunchMatchAsync(Request("second"));

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

            Assert.That((await _launcher.LaunchMatchAsync(Request("r2"))).Code, Is.EqualTo(ElympicsLaunchErrorCode.Busy));
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Running));

            sink.MatchEnded(Guid.Empty);
            Assert.That((await _launcher.LaunchMatchAsync(Request("r3"))).Code, Is.EqualTo(ElympicsLaunchErrorCode.Busy));
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Ended));
            Assert.That(_launcher.IsCurrentlyInMatch, Is.True);
            Assert.That(states, Is.EqualTo(new[] { MatchLauncherState.Ended }));
        });

        [UnityTest]
        public IEnumerator AbortFromIdleSucceedsImmediately() => UniTask.ToCoroutine(async () =>
        {
            var result = await _launcher.AbortAsync("abort", default);

            Assert.That(result.Success, Is.True);
            Assert.That(_events.Select(e => (e.Type, e.RequestId)), Is.EqualTo(new[] { ("abortAccepted", "abort"), ("idle", "abort") }));
            Assert.That(_loader.Loaded, Is.Empty);
        });

        [UnityTest]
        public IEnumerator AbortFromRunningReturnsToBoot() => UniTask.ToCoroutine(async () =>
        {
            _ = await LaunchToRunning();
            _events.Clear();

            var result = await _launcher.AbortAsync("abort", default);

            Assert.That(result.Success, Is.True);
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
            Assert.That(_launcher.IsCurrentlyInMatch, Is.False);
            Assert.That(_events.Select(e => (e.Type, e.RequestId)), Is.EqualTo(new[] { ("abortAccepted", "abort"), ("idle", "abort") }));
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

                _ = await _launcher.AbortAsync("abort", default);

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
            _ = await _launcher.AbortAsync("abort", default);
            _events.Clear();

            var (result, _) = await LaunchToRunning(Request("again"));

            Assert.That(result.Success, Is.True);
            Assert.That(_events.Select(e => (e.Type, e.RequestId)), Is.EqualTo(new[] { ("launchAccepted", "again"), ("launchSucceeded", "again") }));
        });

        [UnityTest]
        public IEnumerator TwoAbortsTearDownOnce() => UniTask.ToCoroutine(async () =>
        {
            _ = await LaunchToRunning();
            _loader.Loaded.Clear();

            var first = _launcher.AbortAsync("a1", default);
            var second = _launcher.AbortAsync("a2", default);
            var (r1, r2) = await UniTask.WhenAll(first, second);

            Assert.That(r1.Success && r2.Success, Is.True);
            Assert.That(_loader.Loaded, Is.EqualTo(new[] { BootScene }));
            Assert.That(_events.Where(e => e.Type == "idle").Select(e => e.RequestId), Is.EquivalentTo(new[] { "a1", "a2" }));
        });

        [UnityTest]
        public IEnumerator AbortDuringLoadingFailsLaunchWithAborted() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            var launch = _launcher.LaunchMatchAsync(Request());
            _ = AttachScene();

            var abort = _launcher.AbortAsync("abort", default);
            _ = _loader.PendingGameplayLoad!.TrySetResult(true);

            var launchResult = await launch;
            var abortResult = await abort;

            Assert.That(launchResult.Code, Is.EqualTo(ElympicsLaunchErrorCode.Aborted));
            Assert.That(abortResult.Success, Is.True);
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
            Assert.That(EventTypes.Count(t => t == "launchSucceeded"), Is.Zero);
            // The boot scene is loaded after the in-flight gameplay load, never before it.
            Assert.That(_loader.Loaded, Is.EqualTo(new[] { GameplayScene, BootScene }));
        });

        [UnityTest]
        public IEnumerator SceneActivatingDuringTeardownIsSuppressed() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            var launch = _launcher.LaunchMatchAsync(Request());
            var abort = _launcher.AbortAsync("abort", default);

            // The gameplay scene activates only now, after the abort.
            Assert.That(_launcher.AttachGameplayScene(out var session), Is.EqualTo(ElympicsMatchLauncher.SceneAttachment.Suppressed));
            Assert.That(session, Is.Null);
            _ = _loader.PendingGameplayLoad!.TrySetResult(true);

            Assert.That((await launch).Code, Is.EqualTo(ElympicsLaunchErrorCode.Aborted));
            Assert.That((await abort).Success, Is.True);
            Assert.That(_launcher.AttachGameplayScene(out _), Is.EqualTo(ElympicsMatchLauncher.SceneAttachment.NotLauncherDriven));
        });

        [Test]
        public void SecondAttachOfSameLaunchIsSuppressed()
        {
            _loader.HoldGameplayLoad = true;
            _ = _launcher.LaunchMatchAsync(Request());
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
            _ = await _launcher.AbortAsync("abort", default);
            _events.Clear();

            sink.MatchEnded(Guid.Empty);
            sink.Disconnected(false);

            Assert.That(_events, Is.Empty);
            Assert.That(_launcher.State, Is.EqualTo(MatchLauncherState.Idle));
        });

        [UnityTest]
        public IEnumerator SceneWithoutGameSceneManagerFails() => UniTask.ToCoroutine(async () =>
        {
            var result = await _launcher.LaunchMatchAsync(Request());

            Assert.That(result.Code, Is.EqualTo(ElympicsLaunchErrorCode.SceneLoadFailed));
            await WaitUntil(() => _launcher.State == MatchLauncherState.Idle);
            Assert.That(_events.Last(), Is.EqualTo(("idle", "launch", "")));
        });

        [UnityTest]
        public IEnumerator SceneThatCannotBeLoadedFails() => UniTask.ToCoroutine(async () =>
        {
            _loader.GameplayLoadResult = false;

            var result = await _launcher.LaunchMatchAsync(Request());

            Assert.That(result.Code, Is.EqualTo(ElympicsLaunchErrorCode.SceneLoadFailed));
            await WaitUntil(() => _launcher.State == MatchLauncherState.Idle);
            // Still in the boot scene: it is not reloaded.
            Assert.That(_loader.Loaded, Is.EqualTo(new[] { GameplayScene }));
        });

        [UnityTest]
        public IEnumerator SceneInitializationFailureFailsLaunch() => UniTask.ToCoroutine(async () =>
        {
            _loader.HoldGameplayLoad = true;
            var launch = _launcher.LaunchMatchAsync(Request());
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
            var launch = _launcher.LaunchMatchAsync(Request());
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
            var launch = _launcher.LaunchMatchAsync(Request(launchTimeout: 1));
            _ = AttachScene();
            _ = _loader.PendingGameplayLoad!.TrySetResult(true);

            Assert.That((await launch).Code, Is.EqualTo(ElympicsLaunchErrorCode.Timeout));
            await WaitUntil(() => _launcher.State == MatchLauncherState.Idle);

            var (result, _) = await LaunchToRunning(Request("next"));
            Assert.That(result.Success, Is.True);
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

            _ = await _launcher.AbortAsync("return", default);
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
            var launch = _launcher.LaunchMatchAsync(Request());

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
            _launcher.HandleHostLaunch("{not json");
            Assert.That(_events.Single().Type, Is.EqualTo("launchFailed"));
            Assert.That(_events.Single().Payload, Does.Contain("InvalidPayload"));
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
        public void MissingBootSceneFallsBackToActiveScene()
        {
            _config.bootScene = "";
            _loader.ActiveScenePath = "Assets/Splash.unity";
            Assert.That(_launcher.BootScene, Is.EqualTo("Assets/Splash.unity"));
        }
    }
}
