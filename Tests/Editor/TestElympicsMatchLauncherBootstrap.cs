using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    [Category("MultiplayerPlayMode")]
    public class TestElympicsMatchLauncherBootstrap
    {
        private const string Gameplay = "Assets/Gameplay.unity";

        [TestCase(Gameplay, "Assets/Boot.unity", true)]
        [TestCase(Gameplay, "", true)]
        [TestCase(Gameplay, Gameplay, false)]
        [TestCase("", "Assets/Boot.unity", false)]
        [TestCase(null, "Assets/Boot.unity", false)]
        public void LaunchModeOpensTheGameplaySceneUnlessItIsActive(string gameplayScene, string activeScene, bool expected) =>
            Assert.That(ElympicsMatchLauncherBootstrap.ShouldLoadGameplayScene(gameplayScene, activeScene), Is.EqualTo(expected));
    }
}
