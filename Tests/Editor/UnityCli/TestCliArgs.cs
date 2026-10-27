#nullable enable

using System;
using Elympics.Editor.UnityCli;
using NUnit.Framework;

namespace Elympics.Editor.Tests.UnityCli
{
    [Category("UnityCli")]
    public class TestCliArgs
    {
        private Func<string>? _originalProjectRoot;
        private string _root = string.Empty;

        [Test]
        public void ExceptionMessageHasCodeTextAndFix()
        {
            var exception = new ElympicsCliException(ElympicsCliErrors.NoConfig, "No config.", "create one.");
            Assert.That(exception.Message, Is.EqualTo("ELYMPICS_NO_CONFIG: No config. Fix: create one."));
            Assert.That(exception, Is.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void OnlyInternalErrorsMayOmitTheFix()
        {
            _ = Assert.Throws<ArgumentException>(() => _ = new ElympicsCliException(ElympicsCliErrors.NoConfig, "No config.", " "));
            Assert.That(new ElympicsCliException(ElympicsCliErrors.Internal, "Boom.", null).Message, Is.EqualTo("ELYMPICS_INTERNAL: Boom."));
        }

        [SetUp]
        public void SetUp()
        {
            _originalProjectRoot = CliArgs.GetProjectRoot;
            _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Proj");
            CliArgs.GetProjectRoot = () => _root;
        }

        [TearDown]
        public void TearDown() => CliArgs.GetProjectRoot = _originalProjectRoot!;

        [TestCase("Builds/WebGL", false, ExpectedResult = "Proj/Builds/WebGL")]
        [TestCase(".", false, ExpectedResult = "Proj")]
        [TestCase("../Proj2/Build", true, ExpectedResult = "Proj2/Build")]
        public string ProjectPathsResolveAgainstTheProjectRoot(string path, bool allowExternal) =>
            System.IO.Path.GetRelativePath(System.IO.Path.GetDirectoryName(_root), CliArgs.ResolveProjectPath(path, "p", allowExternal)).Replace('\\', '/');

        [TestCase("../Proj2/Build")]
        [TestCase("..")]
        [TestCase("Assets/../../Other")]
        public void ProjectPathsOutsideTheProjectNeedTheOptIn(string path) =>
            CliAssert.Throws(() => CliArgs.ResolveProjectPath(path, "p", false), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void AnAbsolutePathOutsideTheProjectNeedsTheOptIn()
        {
            var outside = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_root), "Other", "Build");
            CliAssert.Throws(() => CliArgs.ResolveProjectPath(outside, "p", false), ElympicsCliErrors.InvalidArgument);
            Assert.That(CliArgs.ResolveProjectPath(outside, "p", true), Is.EqualTo(outside));
        }

        [Test]
        public void MissingRequiredArgument() => CliAssert.Throws(() => CliArgs.Require(" ", "version"), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void ConfirmIsRequired()
        {
            CliAssert.Throws(() => CliArgs.RequireConfirm(false, "Uploading"), ElympicsCliErrors.ConfirmRequired);
            Assert.DoesNotThrow(() => CliArgs.RequireConfirm(true, "Uploading"));
        }

        [TestCase("1.2.0")]
        [TestCase("2026-10-22.1")]
        [TestCase("abc")]
        public void ValidVersions(string version) => Assert.That(CliArgs.ValidateVersion(version, "version"), Is.EqualTo(version));

        [TestCase("1.2 beta")]
        [TestCase("v1/2")]
        [TestCase("ąę")]
        [TestCase("0123456789012345678901234567890123456789012345678901234567890123")]
        public void InvalidVersions(string version) => CliAssert.Throws(() => CliArgs.ValidateVersion(version, "version"), ElympicsCliErrors.InvalidArgument);

        [Test]
        public void GameIdMustBeAGuid()
        {
            CliAssert.Throws(() => CliArgs.ValidateGuid("not-a-guid", "game_id"), ElympicsCliErrors.InvalidArgument);
            var guid = Guid.NewGuid();
            Assert.That(CliArgs.ValidateGuid(guid.ToString("N").ToUpperInvariant(), "game_id"), Is.EqualTo(guid.ToString()));
            Assert.That(CliArgs.ValidateGuid(guid.ToString("B"), "game_id"), Is.EqualTo(guid.ToString()));
        }

        [Test]
        public void JsonArrayArgument()
        {
            Assert.That(CliArgs.ParseStringArray("[\"a\",\"b\"]", "packages"), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(CliArgs.ParseStringArray("", "packages"), Is.Empty);
            CliAssert.Throws(() => CliArgs.ParseStringArray("{\"a\":1}", "packages"), ElympicsCliErrors.InvalidArgument);
            CliAssert.Throws(() => CliArgs.ParseStringArray("[a", "packages"), ElympicsCliErrors.InvalidArgument);
        }

        [Test]
        public void JsonObjectArgument()
        {
            Assert.That(CliArgs.ParseStringObject("{\"p\":\"v1.0.0\"}", "pin")["p"], Is.EqualTo("v1.0.0"));
            CliAssert.Throws(() => CliArgs.ParseStringObject("[\"a\"]", "pin"), ElympicsCliErrors.InvalidArgument);
            CliAssert.Throws(() => CliArgs.ParseStringObject("{\"p\":null}", "pin"), ElympicsCliErrors.InvalidArgument);
        }

        [Test]
        public void OptionalArgumentIsTrimmedOrNull()
        {
            Assert.That(CliArgs.TrimToNull("  x "), Is.EqualTo("x"));
            Assert.That(CliArgs.TrimToNull(" "), Is.Null);
            Assert.That(CliArgs.TrimToNull(string.Empty), Is.Null);
            Assert.That(CliArgs.TrimToNull(null), Is.Null);
        }

        [Test]
        public void OptionalBoolArgument()
        {
            Assert.That(CliArgs.ParseOptionalBool("auto", "x"), Is.Null);
            Assert.That(CliArgs.ParseOptionalBool("", "x"), Is.Null);
            Assert.That(CliArgs.ParseOptionalBool(null, "x"), Is.Null);
            Assert.That(CliArgs.ParseOptionalBool(" True ", "x"), Is.True);
            Assert.That(CliArgs.ParseOptionalBool("false", "x"), Is.False);
            CliAssert.Throws(() => CliArgs.ParseOptionalBool("maybe", "x"), ElympicsCliErrors.InvalidArgument);
        }

        private enum Mode { SinglePlayer, HalfRemoteServer }

        [Test]
        public void EnumArgumentAcceptsSnakeCase()
        {
            Assert.That(CliArgs.ParseEnum<Mode>("half_remote_server", "mode"), Is.EqualTo(Mode.HalfRemoteServer));
            CliAssert.Throws(() => CliArgs.ParseEnum<Mode>("online", "mode"), ElympicsCliErrors.InvalidArgument);
        }

        [Test]
        public void UnexpectedExceptionsBecomeInternal()
        {
            var exception = Assert.Catch(() => CliRun.Run(() => throw new NullReferenceException("x")));
            Assert.That(exception.Message, Does.StartWith(ElympicsCliErrors.Internal + ": NullReferenceException"));
            CliAssert.Throws(() => CliRun.Run(() => throw new ArgumentException("plain")), ElympicsCliErrors.InvalidArgument);
        }
    }
}
