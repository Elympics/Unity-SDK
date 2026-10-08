#nullable enable

using NUnit.Framework;

namespace Elympics.Tests
{
    [Category("Launcher")]
    public class TestElympicsMatchLauncherBootstrap
    {
        private DefaultElympicsMatchLauncher? _original;

        [SetUp]
        public void SetUp() => _original = ElympicsMatchLauncher.Current;

        // The play mode launcher of the test run is put back. Its host bridge (none in the Editor by default) is not restored.
        [TearDown]
        public void TearDown() => ElympicsMatchLauncher.Current = _original;

        [Test]
        public void ShutdownClearsCurrentAndIsIdempotent()
        {
            ElympicsMatchLauncherBootstrap.Shutdown();
            Assert.That(ElympicsMatchLauncher.Current, Is.Null);

            Assert.DoesNotThrow(ElympicsMatchLauncherBootstrap.Shutdown);
            Assert.That(ElympicsMatchLauncher.Current, Is.Null);
        }

        [Test]
        public void CreateAfterShutdownGivesFreshLauncher()
        {
            ElympicsMatchLauncherBootstrap.Create();
            var first = ElympicsMatchLauncher.Current;
            Assert.That(first, Is.Not.Null);

            ElympicsMatchLauncherBootstrap.Shutdown();
            ElympicsMatchLauncherBootstrap.Create();

            Assert.That(ElympicsMatchLauncher.Current, Is.Not.Null);
            Assert.That(ElympicsMatchLauncher.Current, Is.Not.SameAs(first));
            Assert.That(ElympicsMatchLauncher.Instance, Is.SameAs(ElympicsMatchLauncher.Current));
        }

        [Test]
        public void CreateKeepsExistingLauncher()
        {
            ElympicsMatchLauncherBootstrap.Create();
            var first = ElympicsMatchLauncher.Current;

            ElympicsMatchLauncherBootstrap.Create();

            Assert.That(ElympicsMatchLauncher.Current, Is.SameAs(first));
        }
    }
}
