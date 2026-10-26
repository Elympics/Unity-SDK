using System.Collections.Generic;
using System.Linq;
using Elympics.Editor.UnityCli.PackageUpdates;
using NUnit.Framework;

namespace Elympics.Editor.Tests.UnityCli
{
    /// <summary>The policy table shared with the elympics-unity plugin's tests.</summary>
    [Category("UnityCli")]
    public class TestElympicsRefPolicy
    {
        private const string Sdk = "https://gitlab.example.com/elympics/sdk/unity-sdk-package.git";
        private const string Template = "https://gitlab.example.com/elympics/server-client-architecture-template.git";
        private static readonly string Develop = new('a', 40);
        private static readonly string Feature = new('b', 40);

        private static RemoteRefs Remote(IEnumerable<string> tags = null, Dictionary<string, string> branches = null)
        {
            var lines = (tags ?? new[] { "v0.9.0", "v0.10.0", "v0.28.0", "v0.29.0", "v0.30.0-rc.1" })
                .Select((tag, i) => $"{i.ToString().PadLeft(40, 'c')}\trefs/tags/{tag}")
                .Concat((branches ?? new Dictionary<string, string> { ["develop"] = Develop, ["chore/remove-obsolete-features"] = Feature })
                    .Select(branch => $"{branch.Value}\trefs/heads/{branch.Key}"));
            return RemoteRefs.Parse(string.Join("\n", lines));
        }

        private static RefPlan Plan(string spec, RemoteRefs remote = null, string pin = null, string track = null) =>
            ElympicsRefPolicy.Plan(GitDependency.Parse(spec), remote ?? Remote(), pin, track);

        private static void AssertPlan(RefPlan plan, RefAction action, string reference = null)
        {
            Assert.That(plan.Action, Is.EqualTo(action), plan.Reason);
            if (reference != null)
                Assert.That(plan.Ref, Is.EqualTo(reference));
        }

        [Test]
        public void PlanKindDescribesThePlannedRef()
        {
            Assert.That(Plan(Sdk + "#v0.28.0").Kind, Is.EqualTo(GitRefKind.Tag));
            Assert.That(Plan(Sdk + "#feature/gone").Kind, Is.EqualTo(GitRefKind.Branch));
            Assert.That(Plan(Sdk + "#v0.28.0", track: "develop").Kind, Is.EqualTo(GitRefKind.Branch));
            Assert.That(Plan(Sdk + "#develop", pin: "v0.9.0").Kind, Is.EqualTo(GitRefKind.Tag));
        }

        [Test]
        public void ChangeActionsAreMoveAndRefresh()
        {
            Assert.That(new PackageUpdate { action = "move" }.IsChange(), Is.True);
            Assert.That(new PackageUpdate { action = "refresh" }.IsChange(), Is.True);
            Assert.That(new PackageUpdate { action = "keep" }.IsChange(), Is.False);
            Assert.That(new PackageUpdate { action = "error" }.IsChange(), Is.False);
        }

        [Test]
        public void TagMovesToTheNewestVersionTag() => AssertPlan(Plan(Sdk + "#v0.28.0"), RefAction.Move, "v0.29.0");

        [Test]
        public void NewestTagIsKept() => AssertPlan(Plan(Sdk + "#v0.29.0"), RefAction.Keep, "v0.29.0");

        [Test]
        public void TagNewerThanTheRemoteIsKept() => AssertPlan(Plan(Sdk + "#v0.31.0"), RefAction.Keep, "v0.31.0");

        [Test]
        public void TagWithoutVKeepsTheRepositorySpelling() => AssertPlan(Plan(Sdk + "#0.28.0", Remote(new[] { "0.28.0", "0.29.0" })), RefAction.Move, "0.29.0");

        [Test]
        public void TagWithoutVersionTagsOnTheRemoteIsAnError()
        {
            var plan = Plan(Sdk + "#v0.28.0", Remote(new string[0]));
            AssertPlan(plan, RefAction.Error);
            Assert.That(plan.ErrorCode, Is.EqualTo(ElympicsRefPolicy.NoVersionTags));
        }

        [Test]
        public void DevelopIsRefreshedToItsHead()
        {
            var plan = Plan(Sdk + "#develop");
            AssertPlan(plan, RefAction.Refresh, "develop");
            Assert.That(plan.Head, Is.EqualTo(Develop));
        }

        [Test]
        public void AFeatureBranchIsRefreshedNotMovedToDevelop()
        {
            var plan = Plan(Sdk + "#chore/remove-obsolete-features");
            AssertPlan(plan, RefAction.Refresh, "chore/remove-obsolete-features");
            Assert.That(plan.Head, Is.EqualTo(Feature));
        }

        [Test]
        public void ADeletedBranchFallsBackToDevelop()
        {
            var dependency = GitDependency.Parse(Sdk + "#feature/gone");
            var plan = ElympicsRefPolicy.Plan(dependency, Remote());
            AssertPlan(plan, RefAction.Move, "develop");
            Assert.That(plan.Head, Is.EqualTo(Develop));
            Assert.That(plan.Warning, Does.Contain("feature/gone").And.Contain("develop"));
            Assert.That(dependency.WithRef(plan.Ref), Is.EqualTo(Sdk + "#develop"));
        }

        [Test]
        public void ADeletedBranchWithoutDevelopIsAnError() =>
            AssertPlan(Plan(Sdk + "#feature/gone", Remote(branches: new Dictionary<string, string> { ["main"] = Feature })), RefAction.Error);

        [Test]
        public void NoRefPinsTheNewestTag()
        {
            var plan = Plan(Sdk);
            AssertPlan(plan, RefAction.Move, "v0.29.0");
            Assert.That(plan.Warning, Does.Contain("unpinned SDK ref"));
        }

        [Test]
        public void NoRefAndNoTagsIsAnError() => AssertPlan(Plan(Sdk, Remote(new string[0])), RefAction.Error);

        [TestCase("690e505d")]
        [TestCase("690e505d690e505d690e505d690e505d690e505d")]
        public void ACommitIsKept(string commit)
        {
            var plan = Plan(Sdk + "#" + commit);
            AssertPlan(plan, RefAction.Keep, commit);
            Assert.That(plan.Warning, Is.Null);
        }

        [Test]
        public void PathIsPreserved()
        {
            var dependency = GitDependency.Parse(Template + "?path=/template/Assets/Template#v0.5.0");
            var plan = ElympicsRefPolicy.Plan(dependency, Remote(new[] { "v0.5.0", "v0.6.0" }));
            AssertPlan(plan, RefAction.Move, "v0.6.0");
            Assert.That(dependency.WithRef(plan.Ref), Is.EqualTo(Template + "?path=/template/Assets/Template#v0.6.0"));
        }

        [Test]
        public void PathWithoutRefPinsTheNewestTag()
        {
            var dependency = GitDependency.Parse(Template + "?path=/template/Assets/Template");
            var plan = ElympicsRefPolicy.Plan(dependency, Remote(new[] { "v0.5.0", "v0.6.0" }));
            AssertPlan(plan, RefAction.Move, "v0.6.0");
            Assert.That(plan.Warning, Is.Not.Null);
            Assert.That(dependency.WithRef(plan.Ref), Is.EqualTo(Template + "?path=/template/Assets/Template#v0.6.0"));
        }

        [Test]
        public void PinMovesToTheTag() => AssertPlan(Plan(Sdk + "#develop", pin: "v0.28.0"), RefAction.Move, "v0.28.0");

        [Test]
        public void PinEqualToTheCurrentRefIsKept() => AssertPlan(Plan(Sdk + "#v0.28.0", pin: "v0.28.0"), RefAction.Keep, "v0.28.0");

        [Test]
        public void PinOfAMissingTagIsAnError()
        {
            var plan = Plan(Sdk + "#develop", pin: "v9.9.9");
            AssertPlan(plan, RefAction.Error);
            Assert.That(plan.ErrorCode, Is.EqualTo(ElympicsRefPolicy.TagNotFound));
        }

        [Test]
        public void TrackMovesToTheBranchHead()
        {
            var plan = Plan(Sdk + "#v0.28.0", track: "develop");
            AssertPlan(plan, RefAction.Move, "develop");
            Assert.That(plan.Head, Is.EqualTo(Develop));
        }

        [Test]
        public void TrackEqualToTheCurrentRefRefreshes() => AssertPlan(Plan(Sdk + "#develop", track: "develop"), RefAction.Refresh, "develop");

        [Test]
        public void TrackOfAMissingBranchIsAnError()
        {
            var plan = Plan(Sdk + "#develop", track: "nope");
            AssertPlan(plan, RefAction.Error);
            Assert.That(plan.ErrorCode, Is.EqualTo(ElympicsRefPolicy.BranchNotFound));
        }

        [TestCase("https://gitlab.example.com/a/b.git#v1.0.0", "https://gitlab.example.com/a/b.git", "https://gitlab.example.com/a/b.git", "v1.0.0")]
        [TestCase("ssh://git@gitlab.example.com/a/b.git#develop", "ssh://git@gitlab.example.com/a/b.git", "ssh://git@gitlab.example.com/a/b.git", "develop")]
        [TestCase("git@gitlab.example.com:a/b.git", "git@gitlab.example.com:a/b.git", "git@gitlab.example.com:a/b.git", null)]
        [TestCase("git+https://github.com/a/b.git?path=/x#v1.0.0", "git+https://github.com/a/b.git", "https://github.com/a/b.git", "v1.0.0")]
        public void UrlFormsAreParsed(string spec, string baseUrl, string remoteUrl, string reference)
        {
            var dependency = GitDependency.Parse(spec);
            Assert.That(dependency.Base, Is.EqualTo(baseUrl));
            Assert.That(dependency.RemoteUrl, Is.EqualTo(remoteUrl));
            Assert.That(dependency.Ref, Is.EqualTo(reference));
            Assert.That(dependency.WithRef(dependency.Ref), Is.EqualTo(spec));
            Assert.That(dependency.HasSupportedRemote, Is.True);
        }

        [TestCase("git+ext::sh -c touch% /tmp/pwned")]
        [TestCase("git+--upload-pack=touch /tmp/pwned")]
        [TestCase("git+-uhttps://example.com/a.git")]
        [TestCase("git+file:///C:/repos/a.git")]
        [TestCase("git+fd::17")]
        [TestCase("http://gitlab.example.com/a/b.git")]
        [TestCase("ssh://-oProxyCommand=touch%20pwned/a.git")]
        [TestCase("git@-oProxyCommand=x:a/b.git")]
        [TestCase("git+https://example.com/a::b.git")]
        public void CommandLikeRemotesAreUnsupported(string spec) => Assert.That(GitDependency.Parse(spec).HasSupportedRemote, Is.False);

        [TestCase("https://example.com/a\\")]
        [TestCase("https://example.com/a\\b.git")]
        [TestCase("https://example.com/a\".git")]
        [TestCase("https://example.com/a\u0001.git")]
        public void RemotesThatBreakArgumentQuotingAreUnsupported(string url) => Assert.That(GitDependency.IsSupportedRemote(url), Is.False);

        [Test]
        public void CredentialsAreRedacted() =>
            Assert.That(GitDependency.RedactCredentials("fatal: unable to access 'https://user:glpat-secret@gitlab.example.com/a.git/'"),
                Is.EqualTo("fatal: unable to access 'https://gitlab.example.com/a.git/'"));

        [TestCase("1.2.3")]
        [TestCase("file:../x")]
        public void NonGitValuesAreSkipped(string spec) => Assert.That(GitDependency.Parse(spec), Is.Null);

        [Test]
        public void EachRepositoryUsesItsOwnTags()
        {
            AssertPlan(Plan("https://gitlab.example.com/elympics/sdk/playpad-sdk.git#v3.1.0", Remote(new[] { "v3.1.0", "v4.0.0" })), RefAction.Move, "v4.0.0");
            AssertPlan(Plan("https://gitlab.example.com/elympics/sdk/unity-lobby.git#v3.1.0", Remote(new[] { "v3.1.0", "v3.2.0" })), RefAction.Move, "v3.2.0");
        }

        [Test]
        public void GitHubTagsComeFromThatRepository() =>
            AssertPlan(Plan("https://github.com/Elympics/Unity-SDK.git#v0.28.0", Remote(new[] { "v0.28.0", "v0.29.0" })), RefAction.Move, "v0.29.0");

        [Test]
        public void AnnotatedTagsArePeeled()
        {
            var tagObject = new string('1', 40);
            var commit = new string('2', 40);
            var remote = RemoteRefs.Parse($"{tagObject}\trefs/tags/v1.0.0\r\n{commit}\trefs/tags/v1.0.0^{{}}\r\n{Develop}\trefs/heads/develop\r\n");

            Assert.That(remote.Tags, Is.EqualTo(new[] { "v1.0.0" }));
            Assert.That(remote.TagCommits["v1.0.0"], Is.EqualTo(commit));
            Assert.That(remote.Branches["develop"], Is.EqualTo(Develop));
        }

        [TestCase("v0.10.0", "v0.9.0", 1)]
        [TestCase("0.29.0", "v0.29.0", 0)]
        [TestCase("v0.30.0-rc.1", "v0.30.0", 0)]
        [TestCase("v1.0.0", "v1.0.1", -1)]
        public void VersionsCompareNumerically(string a, string b, int sign) => Assert.That(System.Math.Sign(ElympicsRefPolicy.VersionCompare(a, b)), Is.EqualTo(sign));
    }
}
