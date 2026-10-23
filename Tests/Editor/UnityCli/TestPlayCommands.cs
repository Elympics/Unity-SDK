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
            var state = JObject.FromObject(ElympicsPlayCliCommands.MatchState());

            Assert.That((string)state["status"], Is.EqualTo("ok"));
            Assert.That((bool)state["inPlayMode"], Is.False);
            Assert.That((bool)state["launcher"]!["exists"], Is.False);
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
            var previous = EditorLaunchRecord.Last;
            var game = ScriptableObject.CreateInstance<ElympicsGameConfig>();
            game.gameId = "cc856fff-25e0-4595-a9cd-290cc99b774e";
            var config = ScriptableObject.CreateInstance<ElympicsConfig>();
            config.availableGames.Add(game);
            // The launch resolves the active game before the queue can refuse it, and the CI project has no ElympicsConfig.
            CliGames.LoadConfig = () => config;
            try
            {
                using var scene = new AdditiveTestScene();
                _ = scene.Create("Changed");
                _ = UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene.Scene);
                // Saving an untitled scene needs a path, so the launch queue refuses before entering Play mode.
                Assume.That(scene.Scene.path, Is.Empty);

                CliAssert.Throws(() => ElympicsPlayCliCommands.Launch("single_player", saveScenes: true), ElympicsCliErrors.LaunchRefused);
                Assert.That(EditorLaunchRecord.Last!.result, Is.EqualTo(EditorLaunchRecord.Failed));
            }
            finally
            {
                CliGames.LoadConfig = ElympicsConfig.Load;
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(game);
                EditorLaunchRecord.Restore(previous);
            }
        }

        [Test]
        public void VirtualPlayerTagsMustBeElympicsTags() =>
            CliAssert.Throws(() => ElympicsPlayCliCommands.VirtualPlayersSet(0, "{\"2\":[\"Red\"]}"), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void LaunchOutcomesAreRecorded()
        {
            var previous = EditorLaunchRecord.Last;
            try
            {
                RecordLaunchOutcomes();
            }
            finally
            {
                EditorLaunchRecord.Restore(previous);
            }
        }

        private static void RecordLaunchOutcomes()
        {
            EditorLaunchRecord.Start("cli-test", "single_player", "game");

            EditorLaunchRecord.OnHostEvent(ElympicsHostProtocol.Events.LaunchAccepted, "other", "");
            Assert.That(EditorLaunchRecord.Last!.result, Is.EqualTo(EditorLaunchRecord.Pending));
            EditorLaunchRecord.OnHostEvent(ElympicsHostProtocol.Events.LaunchAccepted, "cli-test", "");
            Assert.That(EditorLaunchRecord.Last!.result, Is.EqualTo(EditorLaunchRecord.Accepted));
            EditorLaunchRecord.OnHostEvent(ElympicsHostProtocol.Events.LaunchFailed, "cli-test",
                JsonUtility.ToJson(new ElympicsLaunchFailure(ElympicsLaunchErrorCode.Timeout, "The match didn't start.")));
            Assert.That(EditorLaunchRecord.Last!.result, Is.EqualTo(EditorLaunchRecord.TimedOut));

            var error = (string)JObject.FromObject(ElympicsPlayCliCommands.MatchState())["lastLaunch"]!["error"];
            Assert.That(error, Does.StartWith("ELYMPICS_LAUNCH_TIMEOUT: Timeout The match didn't start.").And.Contain(" Fix: "));
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
    }
}
