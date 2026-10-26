using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Elympics.Editor.UnityCli.PackageUpdates
{
    internal enum GitRefKind
    {
        None,
        Tag,
        Commit,
        Branch,
    }

    internal sealed class GitDependency
    {
        private static readonly Regex GitUrl = new("^(https?://|ssh://|git@|git\\+)");
        public static readonly Regex VersionTag = new("^v?\\d+\\.\\d+\\.\\d+$");
        public static readonly Regex Commit = new("^[0-9a-f]{7,40}$");
        private const string PathQuery = "?path=";

        public string Spec { get; }
        public string Base { get; }
        public string Path { get; }
        public string Ref { get; }
        public GitRefKind Kind { get; }

        private GitDependency(string spec, string baseUrl, string path, string reference)
        {
            Spec = spec;
            Base = baseUrl;
            Path = path;
            Ref = reference;
            Kind = KindOf(reference);
        }

        public static GitRefKind KindOf(string reference) =>
            string.IsNullOrEmpty(reference) ? GitRefKind.None
            : VersionTag.IsMatch(reference) ? GitRefKind.Tag
            : Commit.IsMatch(reference) ? GitRefKind.Commit
            : GitRefKind.Branch;

        /// <returns>Null for a value that isn't a git URL (a plain version, a <c>file:</c> path).</returns>
        public static GitDependency Parse(string spec)
        {
            if (spec == null || !GitUrl.IsMatch(spec))
                return null;
            var hash = spec.IndexOf('#');
            var beforeRef = hash < 0 ? spec : spec[..hash];
            var reference = hash < 0 ? null : spec[(hash + 1)..];
            var query = beforeRef.IndexOf(PathQuery, StringComparison.Ordinal);
            var baseUrl = query < 0 ? beforeRef : beforeRef[..query];
            var path = query < 0 ? null : beforeRef[(query + PathQuery.Length)..];
            return new GitDependency(spec, baseUrl, path, reference);
        }

        public static string Format(string baseUrl, string path, string reference) =>
            baseUrl + (string.IsNullOrEmpty(path) ? string.Empty : PathQuery + path) + (string.IsNullOrEmpty(reference) ? string.Empty : "#" + reference);

        public string WithRef(string reference) => Format(Base, Path, reference);

        public string RemoteUrl => Base.StartsWith("git+", StringComparison.Ordinal) ? Base[4..] : Base;

        /// <summary><c>ext::</c>, <c>file://</c> or a value starting with <c>-</c> could make <c>git ls-remote</c> run a command taken from the manifest.</summary>
        public bool HasSupportedRemote => IsSupportedRemote(RemoteUrl);

        public static bool IsSupportedRemote(string url) => SupportedRemote.IsMatch(url) && !url.Contains("::") && !url.Any(character => char.IsControl(character) || character is '"' or '\\');

        private static readonly Regex SupportedRemote = new(@"^(https://(?!-)[^\s/]+|ssh://(?!-)[^\s/]+|git@(?!-)[^\s:/]+:)\S*$");
        private static readonly Regex UserInfo = new(@"(?<=://)[^/@\s]+@");

        public static string RedactCredentials(string text) => text == null ? null : UserInfo.Replace(text, string.Empty);
    }

    internal sealed class RemoteRefs
    {
        public List<string> Tags { get; } = new();

        /// <summary>The commit of each tag: the peeled commit for annotated tags.</summary>
        public Dictionary<string, string> TagCommits { get; } = new();

        public Dictionary<string, string> Branches { get; } = new();

        private static readonly Regex Line = new("^([0-9a-f]{40})\\s+refs/(tags|heads)/(.+)$");
        private const string Peeled = "^{}";

        public static RemoteRefs Parse(string lsRemoteOutput)
        {
            var refs = new RemoteRefs();
            foreach (var line in (lsRemoteOutput ?? string.Empty).Split('\n'))
            {
                var match = Line.Match(line.TrimEnd('\r'));
                if (!match.Success)
                    continue;
                var (sha, kind, name) = (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                if (kind == "heads")
                    refs.Branches[name] = sha;
                else if (name.EndsWith(Peeled, StringComparison.Ordinal))
                    refs.TagCommits[name[..^Peeled.Length]] = sha;
                else
                {
                    refs.Tags.Add(name);
                    if (!refs.TagCommits.ContainsKey(name))
                        refs.TagCommits[name] = sha;
                }
            }
            return refs;
        }
    }

    internal enum RefAction
    {
        Keep,
        Move,
        Refresh,
        Error,
    }

    internal sealed class RefPlan
    {
        public RefAction Action { get; set; }
        public string Ref { get; set; }
        public GitRefKind Kind { get; set; }
        public string Head { get; set; }
        public string Warning { get; set; }
        public string ErrorCode { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>A port of the elympics-unity plugin's <c>planRefUpdate</c>; keep them identical.</summary>
    internal static class ElympicsRefPolicy
    {
        public const string Develop = "develop";
        public const string TagNotFound = "ELYMPICS_TAG_NOT_FOUND";
        public const string BranchNotFound = "ELYMPICS_BRANCH_NOT_FOUND";
        public const string NoVersionTags = "ELYMPICS_NO_VERSION_TAGS";

        /// <returns>Positive when <paramref name="a"/> is newer. A leading v is ignored and only the first three components count.</returns>
        public static int VersionCompare(string a, string b)
        {
            static int[] Parts(string version) =>
                version.TrimStart('v').Split('.', '-').Select(part => int.TryParse(part, out var number) ? number : 0).ToArray();
            var (partsA, partsB) = (Parts(a), Parts(b));
            for (var i = 0; i < 3; i++)
            {
                var difference = (i < partsA.Length ? partsA[i] : 0) - (i < partsB.Length ? partsB[i] : 0);
                if (difference != 0)
                    return difference;
            }
            return 0;
        }

        public static string NewestVersionTag(RemoteRefs remote) =>
            remote.Tags.Where(tag => GitDependency.VersionTag.IsMatch(tag)).OrderBy(tag => tag, Comparer<string>.Create(VersionCompare)).LastOrDefault();

        public static RefPlan Plan(GitDependency dependency, RemoteRefs remote, string pin = null, string track = null)
        {
            var plan = PlanCore(dependency, remote, pin, track);
            plan.Kind = GitDependency.KindOf(plan.Ref);
            return plan;
        }

        private static RefPlan PlanCore(GitDependency dependency, RemoteRefs remote, string pin, string track)
        {
            var newestTag = NewestVersionTag(remote);
            if (!string.IsNullOrEmpty(pin))
                return remote.Tags.Contains(pin)
                    ? new RefPlan { Action = pin == dependency.Ref ? RefAction.Keep : RefAction.Move, Ref = pin, Head = TagCommit(remote, pin) }
                    : Error(dependency, TagNotFound, $"tag {pin} not on the remote");

            if (!string.IsNullOrEmpty(track))
                return remote.Branches.TryGetValue(track, out var trackHead)
                    ? new RefPlan { Action = track == dependency.Ref ? RefAction.Refresh : RefAction.Move, Ref = track, Head = trackHead }
                    : Error(dependency, BranchNotFound, $"branch {track} not on the remote");

            switch (dependency.Kind)
            {
                case GitRefKind.Tag:
                    if (newestTag == null)
                        return Error(dependency, NoVersionTags, "no version tags on the remote");
                    return VersionCompare(newestTag, dependency.Ref) > 0
                        ? new RefPlan { Action = RefAction.Move, Ref = newestTag, Head = TagCommit(remote, newestTag) }
                        : new RefPlan { Action = RefAction.Keep, Ref = dependency.Ref, Head = TagCommit(remote, dependency.Ref) };
                case GitRefKind.Branch:
                    if (remote.Branches.TryGetValue(dependency.Ref, out var head))
                        return new RefPlan { Action = RefAction.Refresh, Ref = dependency.Ref, Head = head };
                    if (!remote.Branches.TryGetValue(Develop, out var developHead))
                        return Error(dependency, BranchNotFound, $"branch {dependency.Ref} and develop both missing on the remote");
                    return new RefPlan
                    {
                        Action = RefAction.Move,
                        Ref = Develop,
                        Head = developHead,
                        Warning = $"branch {dependency.Ref} no longer exists on the remote; falling back to develop",
                    };
                case GitRefKind.None:
                    if (newestTag == null)
                        return Error(dependency, NoVersionTags, "no version tags on the remote to pin");
                    return new RefPlan
                    {
                        Action = RefAction.Move,
                        Ref = newestTag,
                        Head = TagCommit(remote, newestTag),
                        Warning = $"unpinned SDK ref; pinning the newest tag {newestTag}",
                    };
                case GitRefKind.Commit:
                default:
                    return new RefPlan { Action = RefAction.Keep, Ref = dependency.Ref, Head = dependency.Ref };
            }
        }

        private static string TagCommit(RemoteRefs remote, string tag) => tag != null && remote.TagCommits.TryGetValue(tag, out var sha) ? sha : null;

        private static RefPlan Error(GitDependency dependency, string code, string reason) =>
            new() { Action = RefAction.Error, Ref = dependency.Ref, ErrorCode = code, Reason = reason };
    }
}
