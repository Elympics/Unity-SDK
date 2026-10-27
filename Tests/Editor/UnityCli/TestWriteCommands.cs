using System.Linq;
using Elympics.Editor.UnityCli;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Editor.Tests.UnityCli
{
    [Category("UnityCli")]
    public class TestWriteCommands
    {
        [TestCase("", -1, "")]
        [TestCase("abc", 0, "")]
        [TestCase("abc", -1, "Assets/x.asset")]
        public void SelectGameNeedsExactlyOneSelector(string gameId, int index, string configAsset) =>
            CliAssert.Throws(() => ElympicsConfigCliCommands.SelectGame(gameId, index, configAsset), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void SelectGameRejectsAMissingAsset() =>
            CliAssert.Throws(() => ElympicsConfigCliCommands.SelectGame(configAsset: "Assets/NoSuchGameConfig.asset", createIfMissing: false), ElympicsCliErrors.GameNotFound);

        [Test]
        public void SetVersionValidatesTheVersion() =>
            CliAssert.Throws(() => ElympicsConfigCliCommands.SetConfigGameVersion("1.0 beta"), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void ImportSampleRejectsAnUnknownSample() =>
            CliAssert.Throws(() => ElympicsConfigCliCommands.ImportSample("No such sample"), ElympicsCliErrors.SampleNotFound);

        [Test]
        public void SceneSetupAddsTheElympicsSystemOnce()
        {
            using var scene = new AdditiveTestScene();
            _ = scene.Create("Player").AddComponent<ElympicsBehaviour>();
            _ = scene.Create("Ground").AddComponent<BoxCollider2D>();

            var result = JObject.FromObject(ElympicsSceneCliCommands.SetUpScene(addToBuild: false, setGameplayScene: false));
            foreach (var root in scene.Scene.GetRootGameObjects().Where(root => root.GetComponent<GameSceneManager>() != null))
                scene.Track(root);

            Assert.That((string)result["status"], Is.EqualTo("set_up"));
            Assert.That((bool)result["physics2DSimulatorAdded"], Is.True);
            var codes = result["validation"]!["findings"]!.Select(finding => (string)finding["code"]).ToArray();
            Assert.That(codes, Does.Not.Contain(ElympicsSceneValidator.NoElympicsSystem)
                .And.Not.Contain(ElympicsSceneValidator.NetworkIdsStale)
                .And.Not.Contain(ElympicsSceneValidator.Physics2DSimulatorMissing));
            CliAssert.Throws(() => ElympicsSceneCliCommands.SetUpScene(addToBuild: false, setGameplayScene: false), ElympicsCliErrors.SystemAlreadyPresent);
        }

        [Test]
        public void SceneSetupNeedsASavedSceneForBuildSettings()
        {
            using var scene = new AdditiveTestScene();
            CliAssert.Throws(() => ElympicsSceneCliCommands.SetUpScene(), ElympicsCliErrors.InvalidArgument);
        }

        [Test]
        public void SceneSetupRejectsAnUnknownSimulatorOption()
        {
            using var scene = new AdditiveTestScene();
            CliAssert.Throws(() => ElympicsSceneCliCommands.SetUpScene(addToBuild: false, setGameplayScene: false, addPhysics2DSimulator: "maybe"),
                ElympicsCliErrors.InvalidArgument);
        }

        [Test]
        public void ResetNetworkIdsReportsChanges()
        {
            using var scene = new AdditiveTestScene();
            var behaviour = scene.Create("Object").AddComponent<ElympicsBehaviour>();
            behaviour.NetworkId = 77;

            var result = JObject.FromObject(ElympicsSceneCliCommands.ResetNetworkIds());

            Assert.That((int)result["changed"], Is.EqualTo(1));
            Assert.That(behaviour.NetworkId, Is.EqualTo(NetworkIdConstants.PredefinedBehaviourCount));
            Assert.That((bool)result["saved"], Is.False);
        }
    }
}
