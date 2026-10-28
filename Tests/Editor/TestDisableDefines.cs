using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elympics.Editor.Tests
{
    public class TestDisableDefines
    {
        // In the Editor, runInBackground is the project's Player setting, so tests must restore it.
        private bool _runInBackground;

        [SetUp]
        public void SetUp() => _runInBackground = Application.runInBackground;

        [TearDown]
        public void TearDown() => Application.runInBackground = _runInBackground;

        [Test]
        public void UsageStatisticsSenderIsRegisteredUnlessDisabled()
        {
            var sender = typeof(Physics2DSimulatorSceneCheck).Assembly.GetTypes().SingleOrDefault(type => type.Name == "UsageStatisticsSender");
#if ELYMPICS_DISABLE_USAGE_STATISTICS
            Assert.That(sender, Is.Null);
#else
            Assert.That(sender, Is.Not.Null);
            Assert.That(sender.IsDefined(typeof(InitializeOnLoadAttribute), false), Is.True);
#endif
        }

        [Test]
        public void BuildPreprocessorIsRegisteredUnlessDisabled()
        {
            var preprocessors = TypeCache.GetTypesDerivedFrom<IPreprocessBuildWithReport>()
                .Where(type => type.Name == "ElympicsLauncherBuildPreprocessor");
#if ELYMPICS_DISABLE_BUILD_SCENE_VALIDATION
            Assert.That(preprocessors, Is.Empty);
#else
            Assert.That(preprocessors, Is.Not.Empty);
#endif
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RunInBackgroundIsEnabledForHalfRemoteServerUnlessDisabled(bool initial)
        {
            Application.runInBackground = initial;
            if (!initial)
                LogAssert.Expect(LogType.Warning, new Regex("Run In Background"));

            HalfRemoteGameServerInitializer.EnsureRunInBackground();

#if ELYMPICS_DISABLE_RUN_IN_BACKGROUND
            Assert.That(Application.runInBackground, Is.EqualTo(initial));
#else
            Assert.That(Application.runInBackground, Is.True);
#endif
        }

        private static void AddRoot(Scene scene, params Type[] components)
        {
            var root = EditorUtility.CreateGameObjectWithHideFlags("Root", HideFlags.HideAndDontSave, components);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        [Test]
        public void Physics2DSceneCheckWarnsUnlessDisabled()
        {
            // A preview scene leaves the open scenes untouched.
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                AddRoot(scene, typeof(ElympicsBehavioursManager));
                AddRoot(scene, typeof(BoxCollider2D));
#if !ELYMPICS_DISABLE_PHYSICS2D_SCENE_CHECK
                LogAssert.Expect(LogType.Warning, new Regex("no ElympicsUnityPhysics2DSimulator"));
#endif

                new Physics2DSimulatorSceneCheck().OnProcessScene(scene, null);

                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                _ = EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
