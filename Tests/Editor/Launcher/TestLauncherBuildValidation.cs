using System.Collections.Generic;
using System.Linq;
using Elympics.Editor.Launcher;
using NUnit.Framework;
using UnityEngine;

namespace Elympics.Editor.Tests
{
    [Category("Launcher")]
    public class TestLauncherBuildValidation
    {
        private const string Boot = "Assets/Boot.unity";
        private const string GameplayA = "Assets/A.unity";
        private const string GameplayB = "Assets/B.unity";

        private ElympicsConfig _config;
        private readonly List<Object> _created = new();

        [SetUp]
        public void SetUp()
        {
            _config = Create<ElympicsConfig>();
            _config.bootScene = Boot;
            AddGame("A", GameplayA);
            AddGame("B", GameplayB);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
                Object.DestroyImmediate(created);
            _created.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            _created.Add(instance);
            return instance;
        }

        private void AddGame(string name, string scene)
        {
            var game = Create<ElympicsGameConfig>();
            game.gameName = name;
            game.gameplayScene = scene;
            _config.availableGames.Add(game);
        }

        private List<string> Problems(params string[] buildScenes) =>
            ElympicsLauncherBuildValidation.FindProblems(_config, buildScenes.ToHashSet());

        private static void AssertOneProblem(List<string> problems, string fragment) =>
            Assert.That(problems.Count(problem => problem.Contains(fragment)), Is.EqualTo(1), string.Join("\n", problems));

        [Test]
        public void ValidConfigurationHasNoProblems() =>
            Assert.That(Problems(Boot, GameplayA, GameplayB), Is.Empty);

        [Test]
        public void NullGameEntriesAreIgnored()
        {
            _config.availableGames.Add(null);
            Assert.That(Problems(Boot, GameplayA, GameplayB), Is.Empty);
        }

        [Test]
        public void MissingBootSceneIsReported()
        {
            _config.bootScene = "";
            AssertOneProblem(Problems(GameplayA, GameplayB), "no boot scene");
        }

        [Test]
        public void BootSceneNotInBuildIsReported() =>
            AssertOneProblem(Problems(GameplayA, GameplayB), Boot + " is not enabled");

        [Test]
        public void GameplaySceneNotInBuildIsReportedPerGame()
        {
            var problems = Problems(Boot);
            Assert.That(problems, Has.Count.EqualTo(2));
            AssertOneProblem(problems, GameplayA);
            AssertOneProblem(problems, GameplayB);
        }

        [Test]
        public void GameWithoutGameplaySceneIsReported()
        {
            AddGame("C", "");
            AssertOneProblem(Problems(Boot, GameplayA, GameplayB), "C has no gameplay scene");
        }

        [Test]
        public void BootSceneThatIsAlsoAGameplaySceneIsReported()
        {
            _config.bootScene = GameplayA;
            AssertOneProblem(Problems(GameplayA, GameplayB), "must be a separate scene");
        }
    }
}
