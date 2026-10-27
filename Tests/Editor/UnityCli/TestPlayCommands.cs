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

        [Test]
        public void LaunchRefusesUnsavedScenes()
        {
            using var scene = new AdditiveTestScene();
            _ = scene.Create("Changed");
            _ = UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene.Scene);
            // Without a dirty scene the launch would enter Play mode in the middle of the test run.
            Assume.That(EditorMatchLaunchQueue.GetDirtyScenes(), Is.Not.Empty);

            CliAssert.Throws(() => ElympicsPlayCliCommands.Launch("single_player"), ElympicsCliErrors.UnsavedScenes);
        }

        [Test]
        public void RefusedLaunchIsRecordedAsFailed()
        {
            using var record = new LaunchRecordScope();
            // The launch resolves the active game before the queue can refuse it, and the CI project has no ElympicsConfig.
            using var configPath = new ConfigPathScope("ElympicsTests/ElympicsConfig");
            using var scene = new AdditiveTestScene();
            _ = scene.Create("Changed");
            _ = UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene.Scene);
            // Saving an untitled scene needs a path, so the launch queue refuses before entering Play mode.
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
