#nullable enable

using System;
using System.Linq;
using Elympics.Editor.UnityCli;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Editor.Tests.UnityCli
{
    [Category("UnityCli")]
    public class TestReadCommands
    {
        private static JObject ToJson(object result) => JObject.FromObject(result);

        private static string?[] Codes(JObject result, string list = "findings") => result[list]!.Select(finding => (string?)finding["code"]).ToArray();

        [Test]
        public void StatusWorksWithoutAConfig()
        {
            using var _ = ConfigPathScope.Missing();
            var status = ToJson(ElympicsStatusCliCommands.GetStatus());
            Assert.That((string?)status["status"], Is.EqualTo("ok"));
            Assert.That((bool)status["config"]!["present"]!, Is.False);
            Assert.That(status["activeGame"]!.Type, Is.EqualTo(JTokenType.Null));
            Assert.That((string?)status["sdk"]!["version"], Is.Not.Empty);
        }

        [Test]
        public void StatusNeverReportsCredentials()
        {
            var login = new FakeLoginStore { AuthToken = "secret-auth-7f3a", RefreshToken = "secret-refresh-9c1d" };
            using var _ = new EditorApiScope(login);
            var json = ToJson(ElympicsStatusCliCommands.GetStatus()).ToString();
            Assert.That(json, Does.Not.Contain("token").IgnoreCase.And.Not.Contain("password").IgnoreCase.And.Not.Contain("username").IgnoreCase);
            Assert.That(json, Does.Not.Contain("secret-auth-7f3a").And.Not.Contain("secret-refresh-9c1d"));
        }

        [Test]
        public void ConfigCommandsReportAMissingConfig()
        {
            using var _ = ConfigPathScope.Missing();
            CliAssert.Throws(() => CliGames.RequireConfig(), ElympicsCliErrors.NoConfig);
            CliAssert.Throws(() => CliGames.ResolveGameId(""), ElympicsCliErrors.NoConfig);
        }

        [Test]
        public void SceneProblemsAreFound()
        {
            using var scene = new AdditiveTestScene();
            var first = scene.Create("First").AddComponent<ElympicsBehaviour>();
            var second = scene.Create("Second").AddComponent<ElympicsBehaviour>();
            var unassigned = scene.Create("Unassigned").AddComponent<ElympicsBehaviour>();
            first.NetworkId = second.NetworkId = 10;
            unassigned.NetworkId = ElympicsBehaviour.UndefinedNetworkId;
            var synchronized = scene.Create("Synchronized");
            _ = synchronized.AddComponent<Rigidbody>();
            _ = synchronized.AddComponent<ElympicsBehaviour>();
            _ = synchronized.AddComponent<ElympicsTransformSynchronizer>();
            _ = synchronized.AddComponent<ElympicsRigidBodySynchronizer>();

            var result = ToJson(ElympicsSceneCliCommands.Validate(scene.Scene, Array.Empty<string>()));

            Assert.That((string?)result["status"], Is.EqualTo("issues"));
            Assert.That(Codes(result), Is.SupersetOf(new[]
            {
                ElympicsSceneValidator.NoElympicsSystem,
                ElympicsSceneValidator.NetworkIdDuplicate,
                ElympicsSceneValidator.NetworkIdUnassigned,
                ElympicsSceneValidator.NetworkIdsStale,
                ElympicsSceneValidator.TransformRigidbodySyncConflict,
            }));
            Assert.That(result["findings"]!.All(finding => !string.IsNullOrWhiteSpace((string?)finding["fix"])), Is.True);
            Assert.That(result["findings"]!.First(finding => (string?)finding["code"] == ElympicsSceneValidator.TransformRigidbodySyncConflict)["objectPath"]!.ToString(),
                Is.EqualTo("Synchronized"));
        }

        [Test]
        public void IgnoredFindingsAreListedSeparately()
        {
            using var scene = new AdditiveTestScene();

            var result = ToJson(ElympicsSceneCliCommands.Validate(scene.Scene, new[] { ElympicsSceneValidator.NoElympicsSystem }));

            Assert.That(Codes(result), Does.Not.Contain(ElympicsSceneValidator.NoElympicsSystem));
            Assert.That(Codes(result, "ignored"), Does.Contain(ElympicsSceneValidator.NoElympicsSystem));
            Assert.That((int)result["summary"]!["ignored"]!, Is.EqualTo(1));
        }

        [Test]
        public void ComputedIdsMatchTheAssignedOnes()
        {
            using var scene = new AdditiveTestScene();
            var parent = scene.Create("Parent");
            var behaviours = new[] { parent, scene.Create("Child", parent.transform), scene.Create("Other") }.Select(gameObject => gameObject.AddComponent<ElympicsBehaviour>()).ToList();

            var expected = SceneNetworkIdAssigner.ComputeIds(behaviours);
            SceneNetworkIdAssigner.ResetAllIds(scene.Scene);

            Assert.That(behaviours.Select(behaviour => behaviour.NetworkId), Is.EqualTo(behaviours.Select(behaviour => expected[behaviour])));
        }

        [Test]
        public void ValidateSceneRejectsAClosedScene() =>
            CliAssert.Throws(() => ElympicsSceneCliCommands.ValidateScene("Assets/NoSuchScene.unity"), ElympicsCliErrors.SceneNotOpen);
    }
}
