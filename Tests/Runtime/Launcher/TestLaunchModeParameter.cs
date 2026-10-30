#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Elympics.Tests
{
    [Category("Launcher")]
    public class TestLaunchModeParameter
    {
        private ElympicsGameConfig _config = null!;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            typeof(ElympicsGameConfig).GetField("snapshotFilePath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(_config, "Replays/config");
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        #region LaunchDataFromParameters

        [TestCase("halfRemoteClient", typeof(HalfRemoteClientLaunchData))]
        [TestCase("HALFREMOTESERVER", typeof(HalfRemoteServerLaunchData))]
        [TestCase("singleplayer", typeof(SinglePlayerLaunchData))]
        [TestCase("snapshotReplay", typeof(SnapshotReplayLaunchData))]
        public void EachModeGivesItsLaunchData(string mode, Type expected)
        {
            Assert.That(LaunchDataFromParameters.TryCreate(mode, null, _config, out var data, out var error), Is.True, error);
            Assert.That(data, Is.InstanceOf(expected));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("online")]
        [TestCase("local")]
        public void UnknownModeFails(string? mode)
        {
            Assert.That(LaunchDataFromParameters.TryCreate(mode, null, _config, out var data, out var error), Is.False);
            Assert.That(data, Is.Null);
            Assert.That(error, Does.Contain(LaunchDataFromParameters.HalfRemoteClient));
        }

        [Test]
        public void ReplayPathParameterBeatsConfig()
        {
            _ = LaunchDataFromParameters.TryCreate("snapshotReplay", "https://replays.example/r", _config, out var fromParameter, out _);
            _ = LaunchDataFromParameters.TryCreate("snapshotReplay", null, _config, out var fromConfig, out _);

            Assert.That(((SnapshotReplayLaunchData)fromParameter!).Path, Is.EqualTo("https://replays.example/r"));
            Assert.That(((SnapshotReplayLaunchData)fromConfig!).Path, Is.EqualTo("Replays/config"));
        }

        [Test]
        public void HalfRemoteDataFromParametersResolvesToConfig()
        {
            _ = LaunchDataFromParameters.TryCreate("halfRemoteClient", null, _config, out var data, out _);
            var settings = HalfRemoteClientSettings.Resolve((HalfRemoteClientLaunchData)data!, _config);

            Assert.That(settings.ServerEndPoint.Address.ToString(), Is.EqualTo(_config.IpForHalfRemoteMode));
            Assert.That(settings.PlayerIndex, Is.EqualTo(_config.PlayerIndexForHalfRemoteMode));
        }

        #endregion

        #region ExitAfterMatchSink

        [TestCase("MatchEnded")]
        [TestCase("Disconnected")]
        [TestCase("JoinFailed")]
        [TestCase("SceneInitializationFailed")]
        public void SinkExitsExactlyOnce(string report)
        {
            Action<IElympicsMatchLifecycleSink> end = report switch
            {
                "MatchEnded" => sink => sink.MatchEnded(Guid.Empty),
                "Disconnected" => sink => sink.Disconnected(true),
                "JoinFailed" => sink => sink.JoinFailed("x"),
                _ => sink => sink.SceneInitializationFailed(ElympicsLaunchErrorCode.InternalError, "x"),
            };
            LogAssert.ignoreFailingMessages = true;
            var exits = new List<string>();
            IElympicsMatchLifecycleSink sink = new ExitAfterMatchSink(exits.Add);

            sink.MatchJoined();
            Assert.That(exits, Is.Empty);
            end(sink);
            end(sink);
            sink.MatchEnded(Guid.Empty);

            Assert.That(exits, Has.Count.EqualTo(1));
        }

        #endregion

        #region Launcher and scene driver

        [Test]
        public void LauncherLimitedToParameterMatchRejectsLaunchesAsBusy()
        {
            var events = new List<(string Type, string Payload)>();
            var launcher = new DefaultElympicsMatchLauncher(new NullSceneLoader(), () => null, ElympicsLaunchPlatform.Unrestricted, _ => { }, limitedToParameterMatch: true);
            launcher.HostEventEmitted += (type, _, payload) => events.Add((type, payload));

            var result = launcher.LaunchSinglePlayerAsync("a1b2c3d4-0000-0000-0000-000000000001").GetAwaiter().GetResult();

            Assert.That(result.Code, Is.EqualTo(ElympicsLaunchErrorCode.Busy));
            Assert.That(result.Message, Is.EqualTo(DefaultElympicsMatchLauncher.LimitedToParameterMatchMessage));
            Assert.That(launcher.State, Is.EqualTo(MatchLauncherState.Idle));
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo("launchFailed"));
        }

        [TestCase("MatchEnded")]
        [TestCase("Disconnected")]
        public void ParameterMatchMovesTheLauncherThroughItsStates(string end)
        {
            LogAssert.ignoreFailingMessages = true;
            var launcher = new DefaultElympicsMatchLauncher(new NullSceneLoader(), () => null, ElympicsLaunchPlatform.Unrestricted, _ => { }, limitedToParameterMatch: true);
            var endEvents = 0;
            launcher.MatchEnded += _ => endEvents++;
            launcher.Disconnected += _ => endEvents++;
            var exits = new List<string>();
            IElympicsMatchLifecycleSink sink = new ExitAfterMatchSink(exits.Add, launcher.TrackParameterMatch());

            Assert.That(launcher.State, Is.EqualTo(MatchLauncherState.Loading));
            sink.MatchJoined();
            Assert.That(launcher.State, Is.EqualTo(MatchLauncherState.Running));
            Assert.That(launcher.IsCurrentlyInMatch, Is.True);
            if (end == "MatchEnded")
                sink.MatchEnded(Guid.Empty);
            else
                sink.Disconnected(true);

            Assert.That(launcher.State, Is.EqualTo(MatchLauncherState.Ended));
            Assert.That(endEvents, Is.EqualTo(1));
            Assert.That(exits, Has.Count.EqualTo(1));
        }

        [Test]
        public void ParameterMatchThatFailsToStartLeavesTheLauncherIdle()
        {
            LogAssert.ignoreFailingMessages = true;
            var launcher = new DefaultElympicsMatchLauncher(new NullSceneLoader(), () => null, ElympicsLaunchPlatform.Unrestricted, _ => { }, limitedToParameterMatch: true);
            IElympicsMatchLifecycleSink sink = new ExitAfterMatchSink(_ => { }, launcher.TrackParameterMatch());

            sink.JoinFailed("x");

            Assert.That(launcher.State, Is.EqualTo(MatchLauncherState.Idle));
        }

        [Test]
        public void OnlyALauncherLimitedToTheParameterMatchTracksIt()
        {
            var launcher = new DefaultElympicsMatchLauncher(new NullSceneLoader(), () => null, ElympicsLaunchPlatform.Unrestricted, _ => { });

            Assert.That(launcher.TrackParameterMatch(), Is.Null);
            Assert.That(launcher.State, Is.EqualTo(MatchLauncherState.Idle));
        }

        private sealed class NullSceneLoader : IElympicsSceneLoader
        {
            public Cysharp.Threading.Tasks.UniTask<bool> LoadSingleAsync(string scenePath) => Cysharp.Threading.Tasks.UniTask.FromResult(false);
            public bool IsInBuild(string scenePath) => false;
            public string ActiveScenePath => "";
            public Cysharp.Threading.Tasks.UniTask ReturnToEmptySceneAsync() => Cysharp.Threading.Tasks.UniTask.CompletedTask;
            public Cysharp.Threading.Tasks.UniTask ReleaseMemoryAsync(bool forceCollect) => Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }

        // Enums by name: the test method is public, the enums internal.
        [TestCase(true, true, "Attached", "OnlineServer")]
        [TestCase(false, true, "Attached", "LaunchModeParameter")]
        [TestCase(false, false, "Attached", "Session")]
        [TestCase(false, false, "Suppressed", "Suppressed")]
        [TestCase(false, false, "NotLauncherDriven", "None")]
        public void DriverFollowsPriority(bool onlineServer, bool launchMode, string attachmentName, string expectedName)
        {
            var attachment = (ElympicsMatchLauncher.SceneAttachment)Enum.Parse(typeof(ElympicsMatchLauncher.SceneAttachment), attachmentName);
            var expected = (GameSceneManager.Driver)Enum.Parse(typeof(GameSceneManager.Driver), expectedName);
            var attached = false;
            var driver = GameSceneManager.ChooseDriver(onlineServer, launchMode, () =>
            {
                attached = true;
                return attachment;
            });

            Assert.That(driver, Is.EqualTo(expected));
            // A scene driven by a parameter never picks up a launcher session.
            Assert.That(attached, Is.EqualTo(!onlineServer && !launchMode));
        }

        [Test]
        public void ManagerIsNotWantedAfterDestroyOrDisable()
        {
            var gameObject = new GameObject(nameof(ManagerIsNotWantedAfterDestroyOrDisable));
            try
            {
                var manager = gameObject.AddComponent<GameSceneManager>();
                Assert.That(manager.IsStillWanted(CancellationToken.None), Is.True);
                Assert.That(manager.IsStillWanted(new CancellationToken(true)), Is.False);

                manager.enabled = false;
                Assert.That(manager.IsStillWanted(CancellationToken.None), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        #endregion
    }
}
