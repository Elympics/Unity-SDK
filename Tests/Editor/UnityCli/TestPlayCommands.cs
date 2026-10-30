#nullable enable

using System;
using Elympics.Editor.Launcher;
using Elympics.Editor.UnityCli;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Editor.Tests.UnityCli
{
    [Category("UnityCli")]
    public class TestPlayCommands
    {
        [Test]
        public void MatchStateWorksOutsidePlayMode()
        {
            var state = JObject.FromObject(ElympicsPlayCliCommands.GetMatchState());

            Assert.That((string?)state["status"], Is.EqualTo("ok"));
            Assert.That((bool)state["inPlayMode"]!, Is.False);
            Assert.That((bool)state["launcher"]!["exists"]!, Is.False);
            Assert.That(state["match"]!.Type, Is.EqualTo(JTokenType.Null));
        }

        [TestCase("online")]
        [TestCase("snapshot_replay")]
        [TestCase("")]
        public void LaunchRejectsUnsupportedModes(string mode) =>
            CliAssert.Throws(() => ElympicsPlayCliCommands.Launch(mode), ElympicsCliErrors.InvalidArgument);

        [TestCase("single_player", 0)]
        [TestCase("half_remote_server", 0)]
        [TestCase("half_remote_client", -2)]
        public void LaunchRejectsInvalidPlayerIndex(string mode, int playerIndex) =>
            CliAssert.Throws(() => ElympicsPlayCliCommands.Launch(mode, playerIndex: playerIndex), ElympicsCliErrors.InvalidArgument);

        // The test game has no Test players, so no index (explicit or from the config) has an entry.
        [TestCase(-1)]
        [TestCase(0)]
        public void HalfRemoteClientLaunchRequiresATestPlayerForItsIndex(int playerIndex)
        {
            using var configPath = new ConfigPathScope("ElympicsTests/ElympicsConfig");
            Assume.That(EditorMatchLaunchQueue.GetDirtyScenes(), Is.Empty);

            CliAssert.Throws(() => ElympicsPlayCliCommands.Launch("half_remote_client", playerIndex: playerIndex), ElympicsCliErrors.InvalidArgument);
        }

        [Test]
        public void LaunchRefusesUnsavedScenes()
        {
            using var scene = new AdditiveTestScene();
            _ = scene.Create("Changed");
            _ = UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene.Scene);
            // A launch without a dirty scene would enter Play mode.
            Assume.That(EditorMatchLaunchQueue.GetDirtyScenes(), Is.Not.Empty);

            CliAssert.Throws(() => ElympicsPlayCliCommands.Launch("single_player"), ElympicsCliErrors.UnsavedScenes);
        }

        [Test]
        public void RefusedLaunchIsRecordedAsFailed()
        {
            using var record = new LaunchRecordScope();
            // The launch needs an active game; CI has no ElympicsConfig.
            using var configPath = new ConfigPathScope("ElympicsTests/ElympicsConfig");
            using var scene = new AdditiveTestScene();
            _ = scene.Create("Changed");
            _ = UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene.Scene);
            // An untitled scene cannot be saved, so the launch is refused.
            Assume.That(scene.Scene.path, Is.Empty);

            CliAssert.Throws(() => ElympicsPlayCliCommands.Launch("single_player", saveScenes: true), ElympicsCliErrors.LaunchRefused);
            Assert.That(EditorLaunchRecord.Last!.result, Is.EqualTo(EditorLaunchRecord.Failed));
        }

        [TestCase("{\"2\":[\"Red\"]}")]
        [TestCase("{\"2\":[null]}")]
        [TestCase("{\"2\":[{\"tag\":\"Elympics:Red\"}]}")]
        public void VirtualPlayerTagsMustBeElympicsTags(string tags) =>
            CliAssert.Throws(() => ElympicsPlayCliCommands.SetVirtualPlayers(0, tags), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void OnlyRunningPlayersThatStayActiveWithChangedTagsNeedARestart()
        {
            static MppmReflection.VirtualPlayer Player(int number, string state, params string[] tags) => new() { number = number, state = state, tags = tags };
            var before = new[]
            {
                Player(1, "Launched", "Elympics:halfRemoteServer"),
                Player(2, "Launched", "Elympics:PlayerIndex=0", "Other"),
                Player(3, "Launched", "Elympics:PlayerIndex=1"),
                Player(4, "NotLaunched", "Elympics:PlayerIndex=2"),
            };
            var after = new[]
            {
                Player(1, "Launched", "Elympics:halfRemoteServer"),
                Player(2, "Launched", "Elympics:PlayerIndex=1", "Other"),
                Player(3, "Launched", "Elympics:PlayerIndex=0"),
                Player(4, "Launching", "Elympics:PlayerIndex=3"),
            };

            // Player 3 is being deactivated (--count 1), and player 4 has just been activated with its new tags.
            Assert.That(ElympicsPlayCliCommands.PlayersNeedingRestart(before, after, 1), Is.EqualTo(new[] { 2 }));
            Assert.That(ElympicsPlayCliCommands.PlayersNeedingRestart(before, after, 3), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(ElympicsPlayCliCommands.PlayersNeedingRestart(before, before, 3), Is.Empty);
        }

        [Test]
        public void LaunchOutcomesAreRecorded()
        {
            using var record = new LaunchRecordScope();
            EditorLaunchRecord.Start("cli-test", "single_player", "game");

            EditorLaunchRecord.OnHostEvent(ElympicsHostProtocol.Events.LaunchAccepted, "other", "");
            Assert.That(EditorLaunchRecord.Last!.result, Is.EqualTo(EditorLaunchRecord.Pending));
            EditorLaunchRecord.OnHostEvent(ElympicsHostProtocol.Events.LaunchAccepted, "cli-test", "");
            Assert.That(EditorLaunchRecord.Last!.result, Is.EqualTo(EditorLaunchRecord.Accepted));
            EditorLaunchRecord.OnHostEvent(ElympicsHostProtocol.Events.LaunchFailed, "cli-test",
                JsonUtility.ToJson(new ElympicsLaunchFailure(ElympicsLaunchErrorCode.Timeout, "The match didn't start.")));
            Assert.That(EditorLaunchRecord.Last!.result, Is.EqualTo(EditorLaunchRecord.TimedOut));

            var error = (string?)JObject.FromObject(ElympicsPlayCliCommands.GetMatchState())["lastLaunch"]!["error"];
            Assert.That(error, Does.StartWith("ELYMPICS_LAUNCH_TIMEOUT: Timeout The match didn't start.").And.Contain(" Fix: "));
        }

        [Test]
        public void ARunningLaunchEndsWhenPlayModeStops()
        {
            using var record = new LaunchRecordScope();
            EditorLaunchRecord.Restore(new EditorLaunchRecord.Record { requestId = "cli-test", result = EditorLaunchRecord.Running });
            EditorLaunchRecord.OnPlayModeStateChanged(UnityEditor.PlayModeStateChange.ExitingPlayMode);
            Assert.That(EditorLaunchRecord.Last!.result, Is.EqualTo(EditorLaunchRecord.Ended));

            var lastLaunch = JObject.FromObject(ElympicsPlayCliCommands.GetMatchState())["lastLaunch"]!;
            Assert.That(lastLaunch["error"]!.Type, Is.EqualTo(JTokenType.Null));
            Assert.That((string?)lastLaunch["message"], Does.Contain("before the match ended"));
        }

        [Test]
        public void APendingLaunchFailsWhenPlayModeNeverStarted()
        {
            using var record = new LaunchRecordScope();
            EditorLaunchRecord.Start("cli-test", "single_player", "game");
            EditorLaunchRecord.OnPlayModeStateChanged(UnityEditor.PlayModeStateChange.EnteredEditMode);
            Assert.That(EditorLaunchRecord.Last!.result, Is.EqualTo(EditorLaunchRecord.Failed));
        }

#if ELYMPICS_MPPM
        [Test]
        [Category("MultiplayerPlayMode")]
        public void MultiplayerPlayModeInternalsResolve()
        {
            Assert.That(MppmReflection.TryResolve(out var problem), Is.Not.Null, problem);
            Assert.That(MppmReflection.GetPlayers(), Has.Count.GreaterThan(1));
        }
#endif

        private sealed class LaunchRecordScope : IDisposable
        {
            private readonly EditorLaunchRecord.Record? _previous = EditorLaunchRecord.Last;

            public void Dispose() => EditorLaunchRecord.Restore(_previous);
        }
    }
}
