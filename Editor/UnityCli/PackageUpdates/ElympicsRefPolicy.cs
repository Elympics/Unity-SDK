#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Elympics.Editor.UnityCli.PackageUpdates
{
    [JsonConverter(typeof(StringEnumConverter), typeof(SnakeCaseNamingStrategy))]
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
        public static readonly Regex VersionTag = new("^v?\\d+\\.\\d+\\.\\d+(-[0-9A-Za-z-]+(\\.[0-9A-Za-z-]+)*)?(\\+[0-9A-Za-z-]+(\\.[0-9A-Za-z-]+)*)?$");
        public static readonly Regex Commit = new("^[0-9a-f]{7,40}$");
        private const string PathQuery = "?path=";

        public string Spec { get; }
        public string Base { get; }
        public string? Path { get; }
        public string? Ref { get; }
        public GitRefKind Kind { get; }

        private GitDependency(string spec, string baseUrl, string? path, string? reference)
        {
            Spec = spec;
            Base = baseUrl;
            Path = path;
            Ref = reference;
            Kind = GetKind(reference);
        }

        public static GitRefKind GetKind(string? reference) =>
            string.IsNullOrEmpty(reference) ? GitRefKind.None
            : VersionTag.IsMatch(reference) ? GitRefKind.Tag
            : Commit.IsMatch(reference) ? GitRefKind.Commit
            : GitRefKind.Branch;

        /// <returns>Null for a value that isn't a git URL (a plain version, a <c>file:</c> path).</returns>
        public static GitDependency? Parse(string? spec)
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

        public string FormatWithRef(string? reference) =>
            Base + (string.IsNullOrEmpty(Path) ? string.Empty : PathQuery + Path) + (string.IsNullOrEmpty(reference) ? string.Empty : "#" + reference);

        public string RemoteUrl => Base.StartsWith("git+", StringComparison.Ordinal) ? Base[4..] : Base;

        /// <summary>Allows only https, ssh and <c>git@host:</c> remotes without <c>::</c>, quotes, backslashes or control characters, so <c>git ls-remote</c> can't run a command from the manifest.</summary>
        public static bool IsSupportedRemote(string url) => SupportedRemote.IsMatch(url) && !url.Contains("::") && !url.Any(character => char.IsControl(character) || character is '"' or '\\');

        private static readonly Regex SupportedRemote = new(@"^(https://(?!-)[^\s/]+|ssh://(?!-)[^\s/]+|git@(?!-)[^\s:/]+:)\S*$");
        private static readonly Regex UserInfo = new(@"(?<=://)[^/\s]*@");

        [return: NotNullIfNotNull("text")]
        public static string? RedactCredentials(string? text) => text == null ? null : UserInfo.Replace(text, string.Empty);
    }

    internal sealed class RemoteRefs
    {
        public List<string> Tags { get; } = new();

        /// <summary>The commit of each tag: the peeled commit for annotated tags.</summary>
        public Dictionary<string, string> TagCommits { get; } = new();

        public Dictionary<string, string> Branches { get; } = new();

        private static readonly Regex Line = new("^([0-9a-f]{40})\\s+refs/(tags|heads)/(.+)$");
        private const string Peeled = "^{}";

        public static RemoteRefs Parse(string? lsRemoteOutput)
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

    [JsonConverter(typeof(StringEnumConverter), typeof(SnakeCaseNamingStrategy))]
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
        public string? Ref { get; set; }
        public GitRefKind Kind => GitDependency.GetKind(Ref);
        public string? Head { get; set; }
        public string? Warning { get; set; }
        public string? ErrorCode { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>A port of the elympics-unity plugin's <c>planRefUpdate</c>; keep them identical.</summary>
    internal static class ElympicsRefPolicy
    {
        public const string Develop = "develop";
        public const string TagNotFound = "ELYMPICS_TAG_NOT_FOUND";
        public const string BranchNotFound = "ELYMPICS_BRANCH_NOT_FOUND";
        public const string NoVersionTags = "ELYMPICS_NO_VERSION_TAGS";

        /// <returns>Positive when <paramref name="a"/> is newer by semver precedence. A leading v and build metadata are ignored.</returns>
        public static int CompareVersions(string a, string b)
        {
            var (coreA, prereleaseA) = SplitVersion(a);
            var (coreB, prereleaseB) = SplitVersion(b);
            for (var i = 0; i < 3; i++)
            {
                var difference = CompareNumbers(i < coreA.Length ? coreA[i] : "0", i < coreB.Length ? coreB[i] : "0");
                if (difference != 0)
                    return difference;
            }
            if (prereleaseA == null || prereleaseB == null)
                return prereleaseA == prereleaseB ? 0 : prereleaseA == null ? 1 : -1;

            var (fieldsA, fieldsB) = (prereleaseA.Split('.'), prereleaseB.Split('.'));
            for (var i = 0; i < Math.Min(fieldsA.Length, fieldsB.Length); i++)
            {
                var (numericA, numericB) = (IsNumeric(fieldsA[i]), IsNumeric(fieldsB[i]));
                var difference = numericA && numericB ? CompareNumbers(fieldsA[i], fieldsB[i])
                    : numericA != numericB ? (numericA ? -1 : 1)
                    : string.CompareOrdinal(fieldsA[i], fieldsB[i]);
                if (difference != 0)
                    return difference;
            }
            return fieldsA.Length - fieldsB.Length;
        }

        public static bool IsPrerelease(string version) => SplitVersion(version).Prerelease != null;

        private static (string[] Core, string? Prerelease) SplitVersion(string version)
        {
            var plus = version.IndexOf('+');
            var withoutBuild = (plus < 0 ? version : version[..plus]).TrimStart('v');
            var dash = withoutBuild.IndexOf('-');
            return dash < 0 ? (withoutBuild.Split('.'), null) : (withoutBuild[..dash].Split('.'), withoutBuild[(dash + 1)..]);
        }

        private static bool IsNumeric(string field) => field.Length > 0 && field.All(character => character is >= '0' and <= '9');

        private static int CompareNumbers(string a, string b)
        {
            var (trimmedA, trimmedB) = (a.TrimStart('0'), b.TrimStart('0'));
            return trimmedA.Length != trimmedB.Length ? trimmedA.Length - trimmedB.Length : string.CompareOrdinal(trimmedA, trimmedB);
        }

        /// <param name="includePrereleases">False keeps a stable or unpinned ref on stable tags.</param>
        public static string? FindNewestVersionTag(RemoteRefs remote, bool includePrereleases = false) =>
            remote.Tags.Where(tag => GitDependency.VersionTag.IsMatch(tag) && (includePrereleases || !IsPrerelease(tag)))
                .OrderBy(tag => tag, Comparer<string>.Create(CompareVersions)).LastOrDefault();

        public static RefPlan Plan(GitDependency dependency, RemoteRefs remote, string? pin = null, string? track = null)
        {
            var onPrerelease = dependency.Kind == GitRefKind.Tag && IsPrerelease(dependency.Ref!);
            var newestTag = FindNewestVersionTag(remote, onPrerelease);
            if (!string.IsNullOrEmpty(pin))
                return remote.Tags.Contains(pin!)
                    ? new RefPlan { Action = pin == dependency.Ref ? RefAction.Keep : RefAction.Move, Ref = pin, Head = TagCommit(remote, pin) }
                    : CreateErrorPlan(dependency, TagNotFound, $"tag {pin} not on the remote");

            if (!string.IsNullOrEmpty(track))
                return remote.Branches.TryGetValue(track!, out var trackHead)
                    ? new RefPlan { Action = track == dependency.Ref ? RefAction.Refresh : RefAction.Move, Ref = track, Head = trackHead }
                    : CreateErrorPlan(dependency, BranchNotFound, $"branch {track} not on the remote");

            switch (dependency.Kind)
            {
                case GitRefKind.Tag:
                    if (newestTag == null)
                        return CreateErrorPlan(dependency, NoVersionTags, onPrerelease ? "no version tags on the remote" : "no stable version tags on the remote");
                    if (CompareVersions(newestTag, dependency.Ref!) > 0)
                        return new RefPlan { Action = RefAction.Move, Ref = newestTag, Head = TagCommit(remote, newestTag) };
                    return new RefPlan
                    {
                        Action = RefAction.Keep,
                        Ref = dependency.Ref,
                        Head = TagCommit(remote, dependency.Ref),
                        Warning = remote.Tags.Contains(dependency.Ref!) ? null : $"tag {dependency.Ref} not on the remote; keeping it",
                    };
                case GitRefKind.Branch:
                    if (remote.Branches.TryGetValue(dependency.Ref!, out var head))
                        return new RefPlan { Action = RefAction.Refresh, Ref = dependency.Ref, Head = head };
                    if (!remote.Branches.TryGetValue(Develop, out var developHead))
                        return CreateErrorPlan(dependency, BranchNotFound, $"branch {dependency.Ref} and develop both missing on the remote");
                    return new RefPlan
                    {
                        Action = RefAction.Move,
                        Ref = Develop,
                        Head = developHead,
                        Warning = $"branch {dependency.Ref} no longer exists on the remote; falling back to develop",
                    };
                case GitRefKind.None:
                    if (newestTag == null)
                        return CreateErrorPlan(dependency, NoVersionTags, "no stable version tags on the remote to pin");
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

        private static string? TagCommit(RemoteRefs remote, string? tag) => tag != null && remote.TagCommits.TryGetValue(tag, out var sha) ? sha : null;

        private static RefPlan CreateErrorPlan(GitDependency dependency, string code, string reason) =>
            new() { Action = RefAction.Error, Ref = dependency.Ref, ErrorCode = code, Reason = reason };
    }
}
