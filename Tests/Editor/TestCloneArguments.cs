using System;
using System.Collections.Specialized;
using System.IO;
using Elympics.Editor.ParrelSync;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    [Category("ParrelSync")]
    public class TestCloneArguments
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ElympicsCloneTests-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_directory, true);

        private string ArgumentFile => Path.Combine(_directory, CloneArguments.FileName);

        [TestCase("", new string[0])]
        [TestCase("  a   b\tc ", new[] { "a", "b", "c" })]
        [TestCase("--x=\"a b\" \"c d\" e", new[] { "--x=a b", "c d", "e" })]
        [TestCase("\"\"", new[] { "" })]
        public void SplitsLikeACommandLine(string text, string[] expected) =>
            Assert.That(CloneArguments.Split(text), Is.EqualTo(expected));

        [Test]
        public void DefaultArgumentIsRewrittenWithBothArguments()
        {
            File.WriteAllText(ArgumentFile, "client");

            Assert.That(ElympicsCloneArguments.TryRewriteDefault(_directory, 3), Is.True);

            Assert.That(File.ReadAllText(ArgumentFile), Is.EqualTo("--elympics-launch-mode=halfRemoteClient --half-remote-player-index=3"));
            Assert.That(ElympicsCloneArguments.TryRewriteDefault(_directory, 3), Is.False);
        }

        [TestCase("bot")]
        [TestCase("--elympics-launch-mode=singlePlayer")]
        [TestCase("")]
        public void EditedArgumentIsKept(string edited)
        {
            File.WriteAllText(ArgumentFile, edited);

            Assert.That(ElympicsCloneArguments.TryRewriteDefault(_directory, 1), Is.False);
            Assert.That(File.ReadAllText(ArgumentFile), Is.EqualTo(edited));
        }

        [Test]
        public void MissingArgumentFileIsNotCreated()
        {
            Assert.That(ElympicsCloneArguments.TryRewriteDefault(_directory, 1), Is.False);
            Assert.That(File.Exists(ArgumentFile), Is.False);
        }

        #region Precedence

        private ParameterList _parameters;

        private void Initialize(string[] args, string[] cloneArgs, string envLaunchMode = null)
        {
            _parameters = new ParameterList();
            var env = new System.Collections.Hashtable();
            if (envLaunchMode != null)
                env["ELYMPICS_LAUNCH_MODE"] = envLaunchMode;
            Assert.That(_parameters.Initialize(env, args, new NameValueCollection(), new NameValueCollection(), cloneArgs), Is.True);
        }

        [Test]
        public void DefaultCloneArgumentsGiveHalfRemoteClientAndCloneNumber()
        {
            Initialize(new[] { "Unity.exe" }, CloneArguments.Split(ElympicsCloneArguments.GetDefaultArguments(2)).ToArray());

            Assert.That(_parameters.LaunchMode.GetValue(), Is.EqualTo("halfRemoteClient"));
            Assert.That(_parameters.HalfRemotePlayerIndex.GetValue(), Is.EqualTo(2));
        }

        [Test]
        public void RealArgumentsBeatCloneArguments()
        {
            Initialize(new[] { "Unity.exe", "--elympics-launch-mode=singlePlayer" }, new[] { "--elympics-launch-mode=halfRemoteClient" });
            Assert.That(_parameters.LaunchMode.GetValue(), Is.EqualTo("singlePlayer"));
        }

        [Test]
        public void CloneArgumentsBeatEnvironmentVariables()
        {
            Initialize(new[] { "Unity.exe" }, new[] { "--elympics-launch-mode=halfRemoteClient" }, "snapshotReplay");
            Assert.That(_parameters.LaunchMode.GetValue(), Is.EqualTo("halfRemoteClient"));
        }

        [Test]
        public void EnvironmentVariablesApplyWithoutCloneArguments()
        {
            Initialize(new[] { "Unity.exe" }, null, "snapshotReplay");
            Assert.That(_parameters.LaunchMode.GetValue(), Is.EqualTo("snapshotReplay"));
        }

        #endregion
    }
}
