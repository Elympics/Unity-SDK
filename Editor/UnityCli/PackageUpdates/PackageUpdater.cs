using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elympics.Editor.UnityCli.PackageUpdates
{
    internal sealed class PackageUpdate
    {
        public string name;
        public string urlBase;
        public string path;
        public string oldRef;
        public string newRef;
        public string refKind;
        public string action;
        public string oldSpec;
        public string newSpec;
        public string oldCommit;
        public string newCommit;
        public string oldVersion;
        public string newVersion;
        public bool? lockHashMatchesHead;
        public List<string> warnings = new();
        public PackageUpdateError error;
    }

    internal sealed class PackageUpdateError
    {
        public string code;
        public string message;
        public string fix;
    }

    internal sealed class PackageUpdateOperation
    {
        public string operationId;
        public string status;
        public DateTime startedAt;
        public DateTime? finishedAt;
        public List<PackageUpdate> packages = new();
        public List<string> warnings = new();
    }

    /// <summary>
    /// Plans and applies ref updates of the project's <c>com.daftmobile.*</c> git packages (<see cref="ElympicsRefPolicy"/>).
    /// Applying rewrites <c>manifest.json</c>, drops the packages' lock entries (UPM keeps a branch's locked commit otherwise) and starts one resolve.
    /// The resolve may reload the domain running the command, so the result is an operation record that a later call finalizes.
    /// </summary>
    internal sealed class PackageUpdater
    {
        public const string Prefix = "com.daftmobile.";
        public const string RemoteUnreachable = "ELYMPICS_REMOTE_UNREACHABLE";
        public const string UnsupportedRemote = "ELYMPICS_UNSUPPORTED_GIT_URL";
        private static readonly TimeSpan LsRemoteTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan StaleOperation = TimeSpan.FromMinutes(10);

        public string ManifestPath = Path.Combine("Packages", "manifest.json");
        public string LockPath = Path.Combine("Packages", "packages-lock.json");
        public string OperationsDirectory = Path.Combine("Library", "Elympics", "update-packages");
        public IGitProcess Git = new GitProcess();
        public Action Resolve = () => UnityEditor.PackageManager.Client.Resolve();
        public Func<string, string> InstalledVersion = name => UnityEditor.PackageManager.PackageInfo.FindForPackageName(name)?.version;
        public Func<DateTime> UtcNow = () => DateTime.UtcNow;

        public async Task<List<PackageUpdate>> PlanAsync(ICollection<string> packages, IDictionary<string, string> pins, IDictionary<string, string> tracks)
        {
            var dependencies = ReadDependencies();
            var selected = dependencies.Where(entry => entry.Key.StartsWith(Prefix, StringComparison.Ordinal) && GitDependency.Parse(entry.Value) != null)
                .Where(entry => packages.Count == 0 || packages.Contains(entry.Key))
                .ToList();

            foreach (var requested in packages.Concat(pins.Keys).Concat(tracks.Keys).Distinct())
                if (!dependencies.ContainsKey(requested) || GitDependency.Parse(dependencies[requested]) == null)
                    throw ElympicsCliErrors.InvalidArgumentException($"{requested} isn't a git dependency in Packages/manifest.json.",
                        "list the Elympics git packages with a dry run without --packages, and pass only those names.");
            foreach (var name in pins.Keys.Intersect(tracks.Keys))
                throw ElympicsCliErrors.InvalidArgumentException($"{name} is both pinned and tracked.", "pass it in --pin or in --track, not both.");
            if (selected.Count == 0)
                throw new ElympicsCliException(ElympicsCliErrors.NoElympicsPackages, "Packages/manifest.json has no com.daftmobile.* git dependencies.",
                    "add the Elympics SDK as a git dependency (package_add with its git URL); embedded, local and registry packages aren't updated by this command.");

            var locked = ReadLockHashes();
            var plans = selected.Select(entry => PlanOneAsync(entry.Key, entry.Value, locked, pins, tracks)).ToList();
            return (await Task.WhenAll(plans)).ToList();
        }

        private async Task<PackageUpdate> PlanOneAsync(string name, string spec, Dictionary<string, string> locked, IDictionary<string, string> pins, IDictionary<string, string> tracks)
        {
            var dependency = GitDependency.Parse(spec);
            var update = new PackageUpdate
            {
                name = name,
                urlBase = dependency.Base,
                path = dependency.Path,
                oldRef = dependency.Ref,
                refKind = dependency.Kind.ToString().ToLowerInvariant(),
                oldSpec = spec,
                oldCommit = locked.TryGetValue(name, out var hash) ? hash : null,
                oldVersion = InstalledVersion(name),
            };

            if (!dependency.HasSupportedRemote)
            {
                update.action = "error";
                update.error = new PackageUpdateError
                {
                    code = UnsupportedRemote,
                    message = $"{GitDependency.RedactCredentials(dependency.RemoteUrl)} isn't an https, ssh or git@host: URL, so git isn't run for it.",
                    fix = "change the package's URL in Packages/manifest.json to its https:// or git@host: form.",
                };
                return update;
            }

            RemoteRefs remote;
            try
            {
                remote = RemoteRefs.Parse(await Git.LsRemoteAsync(dependency.RemoteUrl, LsRemoteTimeout));
            }
            catch (InvalidOperationException e)
            {
                update.action = "error";
                update.error = new PackageUpdateError
                {
                    code = RemoteUnreachable,
                    message = GitDependency.RedactCredentials($"Can't read the refs of {dependency.RemoteUrl}: {e.Message}"),
                    fix = "check that git is installed and that `git ls-remote <url>` works in a terminal without prompting (stored credentials or an SSH agent), then retry.",
                };
                return update;
            }

            var plan = ElympicsRefPolicy.Plan(dependency, remote, pins.TryGetValue(name, out var pin) ? pin : null, tracks.TryGetValue(name, out var track) ? track : null);
            update.action = plan.Action.ToString().ToLowerInvariant();
            update.newRef = plan.Ref;
            update.newCommit = plan.Head;
            if (plan.Warning != null)
                update.warnings.Add(plan.Warning);
            if (plan.Action == RefAction.Error)
            {
                update.error = new PackageUpdateError { code = plan.ErrorCode, message = plan.Reason, fix = FixFor(plan.ErrorCode) };
                return update;
            }

            update.newSpec = dependency.WithRef(plan.Ref);
            update.newVersion = ElympicsRefPolicy.VersionTag.IsMatch(plan.Ref ?? string.Empty) ? plan.Ref.TrimStart('v') : null;
            if (plan.Action == RefAction.Refresh && update.oldCommit != null && update.oldCommit == plan.Head)
                update.action = "keep";
            return update;
        }

        private static string FixFor(string code) => code switch
        {
            ElympicsRefPolicy.TagNotFound => "pin a tag that exists on the remote (git ls-remote --tags <url> lists them), or omit --pin.",
            ElympicsRefPolicy.BranchNotFound => "track a branch that exists on the remote (git ls-remote --heads <url>), or pin a tag with --pin.",
            ElympicsRefPolicy.NoVersionTags => "track a branch instead with --track '{\"<package>\":\"develop\"}'.",
            _ => "check the package's git URL in Packages/manifest.json.",
        };

        /// <summary>Writes the new refs, drops their lock entries and starts a resolve.</summary>
        public PackageUpdateOperation Apply(List<PackageUpdate> updates)
        {
            FinalizePending();
            var pending = ReadOperations().FirstOrDefault(operation => operation.status == "started" && UtcNow() - operation.startedAt < StaleOperation);
            if (pending != null)
                throw new ElympicsCliException(ElympicsCliErrors.UpdateInProgress, $"The package update {pending.operationId} is still resolving.",
                    $"poll it with elympics_update_packages --operation_id {pending.operationId} and retry when it's done.");

            var changes = updates.Where(update => update.action is "move" or "refresh").ToList();
            var operation = new PackageUpdateOperation
            {
                operationId = UtcNow().ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")[..8],
                status = changes.Count == 0 ? "up_to_date" : "started",
                startedAt = UtcNow(),
                packages = updates,
            };
            if (changes.Count > 0)
            {
                var manifest = JObject.Parse(File.ReadAllText(ManifestPath));
                foreach (var change in changes)
                    manifest["dependencies"]![change.name] = change.newSpec;
                File.WriteAllText(ManifestPath, manifest.ToString(Formatting.Indented) + "\n");

                if (File.Exists(LockPath))
                {
                    var lockFile = JObject.Parse(File.ReadAllText(LockPath));
                    if (lockFile["dependencies"] is JObject lockedDependencies)
                        foreach (var change in changes)
                            _ = lockedDependencies.Remove(change.name);
                    File.WriteAllText(LockPath, lockFile.ToString(Formatting.Indented) + "\n");
                }
            }
            else
                operation.finishedAt = operation.startedAt;

            RedactCredentials(updates);
            Save(operation);
            if (changes.Count > 0)
                Resolve();
            return operation;
        }

        /// <summary>Strips credentials from the URLs shown in results and saved in records; call it once the manifest no longer needs them.</summary>
        public static void RedactCredentials(IEnumerable<PackageUpdate> updates)
        {
            foreach (var update in updates)
            {
                update.urlBase = update.urlBase == null ? null : GitDependency.RedactCredentials(update.urlBase);
                update.oldSpec = update.oldSpec == null ? null : GitDependency.RedactCredentials(update.oldSpec);
                update.newSpec = update.newSpec == null ? null : GitDependency.RedactCredentials(update.newSpec);
            }
        }

        public PackageUpdateOperation ReadOperation(string operationId)
        {
            var path = OperationPath(operationId);
            if (!File.Exists(path))
                throw new ElympicsCliException(ElympicsCliErrors.OperationNotFound, $"No package update operation {operationId} in {OperationsDirectory}.",
                    "pass the operationId an apply returned; records live in the project's Library folder.");
            var operation = JsonConvert.DeserializeObject<PackageUpdateOperation>(File.ReadAllText(path));
            TryFinalize(operation);
            return operation;
        }

        /// <summary>Completes started operations whose packages are all locked again; called after domain reloads and when polled.</summary>
        public void FinalizePending()
        {
            foreach (var operation in ReadOperations().Where(operation => operation.status == "started"))
                TryFinalize(operation);
        }

        private void TryFinalize(PackageUpdateOperation operation)
        {
            if (operation.status != "started")
                return;
            var locked = ReadLockHashes();
            var changed = operation.packages.Where(update => update.action is "move" or "refresh").ToList();
            if (changed.Any(update => !locked.ContainsKey(update.name)))
            {
                if (UtcNow() - operation.startedAt < StaleOperation)
                    return;
                operation.status = "failed";
                operation.warnings.Add("The Package Manager didn't lock every updated package within 10 minutes; check the Console for resolve errors.");
            }
            else
                operation.status = "done";

            foreach (var update in changed)
            {
                var hash = locked.TryGetValue(update.name, out var lockHash) ? lockHash : null;
                update.newVersion = InstalledVersion(update.name) ?? update.newVersion;
                update.lockHashMatchesHead = hash != null && update.newCommit != null ? hash == update.newCommit : null;
                if (hash != null)
                    update.newCommit ??= hash;
            }
            operation.finishedAt = UtcNow();
            Save(operation);
        }

        private Dictionary<string, string> ReadDependencies()
        {
            if (!File.Exists(ManifestPath))
                throw new ElympicsCliException(ElympicsCliErrors.PackageManagerFailed, $"{ManifestPath} doesn't exist.", "run the command in a Unity project.");
            var manifest = JObject.Parse(File.ReadAllText(ManifestPath));
            return (manifest["dependencies"] as JObject)?.Properties().ToDictionary(property => property.Name, property => (string)property.Value)
                ?? new Dictionary<string, string>();
        }

        private Dictionary<string, string> ReadLockHashes()
        {
            if (!File.Exists(LockPath))
                return new Dictionary<string, string>();
            var lockFile = JObject.Parse(File.ReadAllText(LockPath));
            return (lockFile["dependencies"] as JObject)?.Properties()
                .Where(property => property.Value["hash"] != null)
                .ToDictionary(property => property.Name, property => (string)property.Value["hash"])
                ?? new Dictionary<string, string>();
        }

        private string OperationPath(string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId) || operationId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || operationId.Contains(".."))
                throw ElympicsCliErrors.InvalidArgumentException($"--operation_id '{operationId}' is invalid.", "pass the operationId an apply returned.");
            return Path.Combine(OperationsDirectory, operationId + ".json");
        }

        private IEnumerable<PackageUpdateOperation> ReadOperations() =>
            Directory.Exists(OperationsDirectory)
                ? Directory.GetFiles(OperationsDirectory, "*.json").Select(file => JsonConvert.DeserializeObject<PackageUpdateOperation>(File.ReadAllText(file)))
                : Enumerable.Empty<PackageUpdateOperation>();

        private void Save(PackageUpdateOperation operation)
        {
            _ = Directory.CreateDirectory(OperationsDirectory);
            File.WriteAllText(OperationPath(operation.operationId), JsonConvert.SerializeObject(operation, Formatting.Indented));
        }
    }
}
