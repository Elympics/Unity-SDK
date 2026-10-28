using System;
using System.Collections.Specialized;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    [Category("MultiplayerPlayMode")]
    public class TestPlayModeArguments
    {
        private static readonly string[] NoTags = Array.Empty<string>();
        private ParameterList _parameters;

        [TestCase(new[] { "Unity.exe", "-name", "Player3" }, 3)]
        [TestCase(new[] { "Unity.exe", "-projectPath", "x", "-name", "Player2", "-batchmode" }, 2)]
        [TestCase(new[] { "Unity.exe" }, null)]
        [TestCase(new[] { "Unity.exe", "-name" }, null)]
        [TestCase(new[] { "Unity.exe", "-name", "Alice" }, null)]
        public void PlayerNumberComesFromTheNameArgument(string[] commandLine, int? expected) =>
            Assert.That(PlayModeArguments.GetPlayerNumber(commandLine), Is.EqualTo(expected));

        [Test]
        public void VirtualPlayerDefaultsToHalfRemoteClientWithIndexFromItsName() =>
            Assert.That(PlayModeArguments.Build(new[] { "Unity.exe", "-name", "Player2" }, NoTags),
                Is.EqualTo(new[] { "--elympics-launch-mode=halfRemoteClient", "--half-remote-player-index=0" }));

        [Test]
        public void WithoutANameOnlyTheLaunchModeIsSet() =>
            Assert.That(PlayModeArguments.Build(new[] { "Unity.exe" }, NoTags), Is.EqualTo(new[] { "--elympics-launch-mode=halfRemoteClient" }));

        [Test]
        public void WithoutAutoLaunchAPlayerWithoutALaunchModeTagIsManual() =>
            Assert.That(PlayModeArguments.Build(new[] { "Unity.exe", "-name", "Player2" }, new[] { "Elympics:PlayerIndex=3" }, launchByDefault: false), Is.Empty);

        [Test]
        public void WithoutAutoLaunchALaunchModeTagStillLaunches() =>
            Assert.That(PlayModeArguments.Build(new[] { "Unity.exe", "-name", "Player2" }, new[] { "Elympics:halfRemoteServer" }, launchByDefault: false),
                Is.EqualTo(new[] { "--half-remote-player-index=0", "--elympics-launch-mode=halfRemoteServer" }));

        [Test]
        public void ManualTagLeavesThePlayerToTheMatchLauncher() =>
            Assert.That(PlayModeArguments.Build(new[] { "Unity.exe", "-name", "Player2" }, new[] { "Elympics:Manual", "Elympics:halfRemoteServer" }), Is.Empty);

        [Test]
        public void TagsOverrideTheDefaults()
        {
            Initialize(new[] { "Unity.exe" },
                PlayModeArguments.Build(new[] { "Unity.exe", "-name", "Player4" }, new[] { "Red", "elympics:HALFREMOTESERVER", "Elympics: PlayerIndex = 5" }).ToArray());

            Assert.That(_parameters.LaunchMode.GetValue(), Is.EqualTo("halfRemoteServer"));
            Assert.That(_parameters.HalfRemotePlayerIndex.GetValue(), Is.EqualTo(5));
        }

        [Test]
        public void KeyValueTagIsPassedAsALongArgument() =>
            Assert.That(PlayModeArguments.Build(new[] { "Unity.exe" }, new[] { "Elympics:snapshot-replay=replay.bin" }),
                Is.EqualTo(new[] { "--elympics-launch-mode=halfRemoteClient", "--snapshot-replay=replay.bin" }));

        [Test]
        public void UnknownLaunchModeTagIsIgnored() =>
            Assert.That(PlayModeArguments.Build(new[] { "Unity.exe" }, new[] { "Elympics:bot" }), Is.EqualTo(new[] { "--elympics-launch-mode=halfRemoteClient" }));

        #region Precedence

        private void Initialize(string[] args, string[] playModeArgs, string envLaunchMode = null)
        {
            _parameters = new ParameterList();
            var env = new System.Collections.Hashtable();
            if (envLaunchMode != null)
                env["ELYMPICS_LAUNCH_MODE"] = envLaunchMode;
            Assert.That(_parameters.Initialize(env, args, new NameValueCollection(), new NameValueCollection(), playModeArgs), Is.True);
        }

        [Test]
        public void DefaultPlayModeArgumentsGiveHalfRemoteClientAndPlayerIndex()
        {
            Initialize(new[] { "Unity.exe" }, PlayModeArguments.Build(new[] { "Unity.exe", "-name", "Player3" }, NoTags).ToArray());

            Assert.That(_parameters.LaunchMode.GetValue(), Is.EqualTo("halfRemoteClient"));
            Assert.That(_parameters.HalfRemotePlayerIndex.GetValue(), Is.EqualTo(1));
        }

        [Test]
        public void RealArgumentsBeatPlayModeArguments()
        {
            Initialize(new[] { "Unity.exe", "--elympics-launch-mode=singlePlayer" }, new[] { "--elympics-launch-mode=halfRemoteClient" });
            Assert.That(_parameters.LaunchMode.GetValue(), Is.EqualTo("singlePlayer"));
        }

        [Test]
        public void PlayModeArgumentsBeatEnvironmentVariables()
        {
            Initialize(new[] { "Unity.exe" }, new[] { "--elympics-launch-mode=halfRemoteClient" }, "snapshotReplay");
            Assert.That(_parameters.LaunchMode.GetValue(), Is.EqualTo("halfRemoteClient"));
        }

        [Test]
        public void EnvironmentVariablesApplyWithoutPlayModeArguments()
        {
            Initialize(new[] { "Unity.exe" }, null, "snapshotReplay");
            Assert.That(_parameters.LaunchMode.GetValue(), Is.EqualTo("snapshotReplay"));
        }

        #endregion
    }
}
