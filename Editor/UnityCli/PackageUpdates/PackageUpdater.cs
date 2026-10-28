#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Elympics.Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Elympics.Editor.UnityCli.PackageUpdates
{
    internal sealed class PackageUpdate
    {
        public string name = null!;
        public string urlBase = null!;
        public string? path;
        public string? oldRef;
        public string? newRef;
        public GitRefKind refKind;
        public RefAction action;
        public string oldSpec = null!;
        public string? newSpec;
        public string? oldCommit;
        public string? newCommit;
        public string? oldVersion;
        public string? newVersion;
        public bool? lockHashMatchesHead;
        public List<string> warnings = new();
        public PackageUpdateError? error;

        public bool IsChange() => action is RefAction.Move or RefAction.Refresh;
    }

    [JsonConverter(typeof(StringEnumConverter), typeof(SnakeCaseNamingStrategy))]
    internal enum PackageUpdateStatus
    {
        Planned,
        UpToDate,
        Started,
        Done,
        Failed,
    }

    internal sealed class PackageUpdateError
    {
        public string code = null!;
        public string message = null!;
        public string fix = null!;
    }

    internal sealed class PackageUpdateOperation
    {
        public string operationId = null!;
        public PackageUpdateStatus status;
        public DateTime startedAt;
        public DateTime? finishedAt;
        public List<PackageUpdate> packages = new();
        public List<string> warnings = new();
    }

    /// <summary>The resolve it starts may reload the domain, so the result is an operation record that a later call finalizes.</summary>
    internal sealed class PackageUpdater
    {
        public const string PackagePrefix = "com.daftmobile.";
        public const string RemoteUnreachable = "ELYMPICS_REMOTE_UNREACHABLE";
        public const string UnsupportedRemote = "ELYMPICS_UNSUPPORTED_GIT_URL";
        private static readonly TimeSpan LsRemoteTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan StaleOperation = TimeSpan.FromMinutes(10);
        private const int ReplaceAttempts = 5;
        private static readonly TimeSpan ReplaceRetryDelay = TimeSpan.FromMilliseconds(100);

        public string ManifestPath = Path.Combine("Packages", "manifest.json");
        public string LockPath = Path.Combine("Packages", "packages-lock.json");
        public string OperationsDirectory = Path.Combine("Library", "Elympics", "update-packages");
        public IGitProcess Git = new GitProcess();
        public Action Resolve = () => UnityEditor.PackageManager.Client.Resolve();
        public Func<string, string?> GetInstalledVersion = name => UnityEditor.PackageManager.PackageInfo.FindForPackageName(name)?.version;
        public Func<DateTime> GetUtcNow = () => DateTime.UtcNow;

        public async UniTask<List<PackageUpdate>> PlanAsync(ICollection<string> packages, IDictionary<string, string> pins, IDictionary<string, string> tracks)
        {
            var gitDependencies = ReadDependencies()
                .Select(entry => (name: entry.Key, dependency: GitDependency.Parse(entry.Value)))
                .Where(entry => entry.dependency != null)
                .ToDictionary(entry => entry.name, entry => entry.dependency!);
            var selected = gitDependencies.Where(entry => entry.Key.StartsWith(PackagePrefix, StringComparison.Ordinal))
                .Where(entry => packages.Count == 0 || packages.Contains(entry.Key))
                .ToList();

            foreach (var requested in packages.Distinct())
                if (!requested.StartsWith(PackagePrefix, StringComparison.Ordinal) || !gitDependencies.ContainsKey(requested))
                    throw ElympicsCliErrors.InvalidArgumentException($"{requested} isn't a {PackagePrefix}* git dependency in Packages/manifest.json.",
                        "list the Elympics git packages with a dry run without --packages, and pass only those names.");
            foreach (var requested in pins.Keys.Concat(tracks.Keys).Distinct())
                if (!selected.Any(entry => entry.Key == requested))
                    throw ElympicsCliErrors.InvalidArgumentException($"{requested} isn't among the selected git packages in Packages/manifest.json.",
                        "list the Elympics git packages with a dry run without --packages, and pass only those names (and include them in --packages if it's set).");
            var pinnedAndTracked = pins.Keys.Intersect(tracks.Keys).FirstOrDefault();
            if (pinnedAndTracked != null)
                throw ElympicsCliErrors.InvalidArgumentException($"{pinnedAndTracked} is both pinned and tracked.", "pass it in --pin or in --track, not both.");
            var badPin = pins.FirstOrDefault(entry => GitDependency.GetKind(entry.Value) is not (GitRefKind.Tag or GitRefKind.Commit));
            if (badPin.Key != null)
                throw ElympicsCliErrors.InvalidArgumentException($"--pin '{badPin.Value}' for {badPin.Key} isn't a version tag or a commit.",
                    "pin a tag like v0.29.0 or a commit hash (7-40 hex characters); use --track for a branch.");
            if (selected.Count == 0)
                throw new ElympicsCliException(ElympicsCliErrors.NoElympicsPackages, "Packages/manifest.json has no com.daftmobile.* git dependencies.",
                    "add the Elympics SDK as a git dependency (package_add with its git URL); embedded, local and registry packages aren't updated by this command.");

            var locked = ReadLockHashes();
            var plans = selected.Select(entry => PlanOneAsync(entry.Key, entry.Value, locked, pins, tracks)).ToList();
            return (await UniTask.WhenAll(plans)).ToList();
        }

        private async UniTask<PackageUpdate> PlanOneAsync(string name, GitDependency dependency, Dictionary<string, string> locked, IDictionary<string, string> pins, IDictionary<string, string> tracks)
        {
            var update = new PackageUpdate
            {
                name = name,
                urlBase = dependency.Base,
                path = dependency.Path,
                oldRef = dependency.Ref,
                refKind = dependency.Kind,
                oldSpec = dependency.Spec,
                oldCommit = locked.TryGetValue(name, out var hash) ? hash : null,
                oldVersion = GetInstalledVersion(name),
            };

            if (!GitDependency.IsSupportedRemote(dependency.RemoteUrl))
                return Fail(update, UnsupportedRemote, $"{GitDependency.RedactCredentials(dependency.RemoteUrl)} isn't an https, ssh or git@host: URL, so git isn't run for it.",
                    "change the package's URL in Packages/manifest.json to its https:// or git@host: form.");

            RemoteRefs remote;
            try
            {
                remote = RemoteRefs.Parse(await Git.ListRemoteRefsAsync(dependency.RemoteUrl, LsRemoteTimeout));
            }
            catch (InvalidOperationException e)
            {
                return Fail(update, RemoteUnreachable, GitDependency.RedactCredentials($"Can't read the refs of {dependency.RemoteUrl}: {e.Message}"),
                    "check that git is installed and that `git ls-remote <url>` works in a terminal without prompting (stored credentials or an SSH agent), then retry.");
            }

            var plan = pins.TryGetValue(name, out var pin) && GitDependency.GetKind(pin) == GitRefKind.Commit
                ? new RefPlan { Action = pin == dependency.Ref ? RefAction.Keep : RefAction.Move, Ref = pin, Head = pin }
                : ElympicsRefPolicy.Plan(dependency, remote, pin, tracks.TryGetValue(name, out var track) ? track : null);
            update.newRef = plan.Ref;
            update.newCommit = plan.Head;
            if (plan.Warning != null)
                update.warnings.Add(plan.Warning);
            if (plan.Action == RefAction.Error)
                return Fail(update, plan.ErrorCode!, plan.Reason!, FixFor(plan.ErrorCode!));
            update.action = plan.Action;

            update.newSpec = dependency.FormatWithRef(plan.Ref);
            update.newVersion = plan.Kind == GitRefKind.Tag ? plan.Ref!.TrimStart('v') : null;
            if (plan.Action == RefAction.Refresh && update.oldCommit != null && update.oldCommit == plan.Head)
                update.action = RefAction.Keep;
            return update;
        }

        private static PackageUpdate Fail(PackageUpdate update, string code, string message, string fix)
        {
            update.action = RefAction.Error;
            update.error = new PackageUpdateError { code = code, message = message, fix = fix };
            return update;
        }

        private static string FixFor(string code) => code switch
        {
            ElympicsRefPolicy.TagNotFound => "pin a tag that exists on the remote (git ls-remote --tags <url> lists them), or omit --pin.",
            ElympicsRefPolicy.BranchNotFound => "track a branch that exists on the remote (git ls-remote --heads <url>), or pin a tag with --pin.",
            ElympicsRefPolicy.NoVersionTags => "track a branch instead with --track '{\"<package>\":\"develop\"}'.",
            _ => "check the package's git URL in Packages/manifest.json.",
        };

        internal static PackageUpdateStatus GetUnchangedStatus(List<PackageUpdate> updates) =>
            updates.Any(update => update.action == RefAction.Error) ? PackageUpdateStatus.Failed : PackageUpdateStatus.UpToDate;

        public PackageUpdateOperation Apply(List<PackageUpdate> updates)
        {
            FinalizePending();
            var pending = ReadOperations().FirstOrDefault(operation => operation.status == PackageUpdateStatus.Started && GetUtcNow() - operation.startedAt < StaleOperation);
            if (pending != null)
                throw new ElympicsCliException(ElympicsCliErrors.UpdateInProgress, $"The package update {pending.operationId} is still resolving.",
                    $"poll it with elympics_update_packages --operation_id {pending.operationId} and retry when it's done.");

            var changes = updates.Where(update => update.IsChange()).ToList();
            var startedAt = GetUtcNow();
            var operation = new PackageUpdateOperation
            {
                operationId = startedAt.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")[..8],
                status = changes.Count == 0 ? GetUnchangedStatus(updates) : PackageUpdateStatus.Started,
                startedAt = startedAt,
                finishedAt = changes.Count == 0 ? startedAt : null,
                packages = updates,
            };
            if (changes.Count == 0)
            {
                RedactCredentials(updates);
                Save(operation);
                return operation;
            }

            var manifest = ReadJson(ManifestPath);
            foreach (var change in changes)
                manifest["dependencies"]![change.name] = change.newSpec;
            var lockFile = File.Exists(LockPath) ? ReadJson(LockPath) : null;
            if (lockFile?["dependencies"] is JObject lockedDependencies)
                foreach (var change in changes)
                    _ = lockedDependencies.Remove(change.name);

            RedactCredentials(updates);
            Save(operation);
            try
            {
                // Both temp files are written first, so most failures happen before anything in Packages/ changes.
                var manifestTemp = WriteTemp(ManifestPath, manifest);
                var lockTemp = lockFile != null ? WriteTemp(LockPath, lockFile) : null;
                ReplaceFile(manifestTemp, ManifestPath);
                if (lockTemp != null)
                    ReplaceFile(lockTemp, LockPath);
                Resolve();
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                operation.status = PackageUpdateStatus.Failed;
                operation.finishedAt = GetUtcNow();
                Save(operation);
                throw;
            }
            return operation;
        }

        /// <summary>Call it once the manifest no longer needs the credentials in the URLs.</summary>
        public static void RedactCredentials(IEnumerable<PackageUpdate> updates)
        {
            foreach (var update in updates)
            {
                update.urlBase = GitDependency.RedactCredentials(update.urlBase);
                update.oldSpec = GitDependency.RedactCredentials(update.oldSpec);
                update.newSpec = GitDependency.RedactCredentials(update.newSpec);
            }
        }

        public PackageUpdateOperation ReadOperation(string operationId)
        {
            var path = GetOperationPath(operationId);
            if (!File.Exists(path))
                throw new ElympicsCliException(ElympicsCliErrors.OperationNotFound, $"No package update operation {operationId} in {OperationsDirectory}.",
                    "pass the operationId an apply returned; records live in the project's Library folder.");
            var operation = TryReadOperation(path)
                ?? throw new ElympicsCliException(ElympicsCliErrors.OperationNotFound, $"The package update record {operationId} in {OperationsDirectory} is unreadable.",
                    "delete the record file and run a dry run to plan the update again.");
            TryFinalize(operation);
            return operation;
        }

        public void FinalizePending()
        {
            foreach (var operation in ReadOperations().Where(operation => operation.status == PackageUpdateStatus.Started))
                TryFinalize(operation);
        }

        private void TryFinalize(PackageUpdateOperation operation)
        {
            if (operation.status != PackageUpdateStatus.Started)
                return;
            operation.packages ??= new List<PackageUpdate>();
            operation.warnings ??= new List<string>();
            var locked = ReadLockHashes();
            var changed = operation.packages.Where(update => update.IsChange()).ToList();
            var now = GetUtcNow();
            if (changed.Any(update => !locked.ContainsKey(update.name)))
            {
                if (now - operation.startedAt < StaleOperation)
                    return;
                operation.status = PackageUpdateStatus.Failed;
                operation.finishedAt = now;
                operation.warnings.Add($"The Package Manager didn't lock every updated package within {StaleOperation.TotalMinutes:0} minutes; check the Console for resolve errors.");
            }
            else
            {
                operation.status = PackageUpdateStatus.Done;
                operation.finishedAt = now;
            }

            foreach (var update in changed)
            {
                var hash = locked.TryGetValue(update.name, out var lockHash) ? lockHash : null;
                if (operation.status == PackageUpdateStatus.Done)
                    update.newVersion = GetInstalledVersion(update.name) ?? update.newVersion;
                // A commit pin may be an abbreviated hash.
                update.lockHashMatchesHead = hash != null && update.newCommit != null ? hash.StartsWith(update.newCommit, StringComparison.Ordinal) : null;
                if (hash != null && update.lockHashMatchesHead != false)
                    update.newCommit = hash;
            }
            Save(operation);
        }

        private static JObject ReadJson(string path)
        {
            try
            {
                using var reader = new JsonTextReader(new StringReader(File.ReadAllText(path))) { DateParseHandling = DateParseHandling.None };
                var json = JObject.Load(reader);
                if (reader.Read())
                    throw new JsonReaderException("Additional text found after the JSON object.");
                return json;
            }
            catch (JsonException e)
            {
                throw new ElympicsCliException(ElympicsCliErrors.PackageManagerFailed, $"{path} isn't valid JSON: {e.Message}",
                    "fix or restore the file (version control has the last good copy), then retry.");
            }
        }

        private static string WriteTemp(string path, JObject json)
        {
            var temp = path + ".tmp";
            File.WriteAllText(temp, json.ToString(Formatting.Indented) + "\n");
            return temp;
        }

        // Antivirus, indexers and Unity's file watcher can hold the file for a moment.
        internal static void ReplaceFile(string source, string destination)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    if (File.Exists(destination))
                        File.Replace(source, destination, null);
                    else
                        File.Move(source, destination);
                    return;
                }
                catch (Exception e) when (e is UnauthorizedAccessException or (IOException and not FileNotFoundException and not DirectoryNotFoundException))
                {
                    if (attempt == ReplaceAttempts)
                        throw new ElympicsCliException(ElympicsCliErrors.PackageManagerFailed, $"Couldn't replace {destination}: {e.Message}",
                            $"close any program holding {Path.GetFileName(destination)} (antivirus, indexer, IDE) or wait a moment, then retry.", e);
                    Thread.Sleep(ReplaceRetryDelay);
                }
            }
        }

        private Dictionary<string, string?> ReadDependencies()
        {
            if (!File.Exists(ManifestPath))
                throw new ElympicsCliException(ElympicsCliErrors.PackageManagerFailed, $"{ManifestPath} doesn't exist.", "run the command in a Unity project.");
            var manifest = ReadJson(ManifestPath);
            return (manifest["dependencies"] as JObject)?.Properties().ToDictionary(property => property.Name, property => (string?)property.Value)
                ?? new Dictionary<string, string?>();
        }

        private Dictionary<string, string> ReadLockHashes()
        {
            if (!File.Exists(LockPath))
                return new Dictionary<string, string>();
            var lockFile = ReadJson(LockPath);
            return (lockFile["dependencies"] as JObject)?.Properties()
                .Where(property => property.Value["hash"] != null)
                .ToDictionary(property => property.Name, property => ((string?)property.Value["hash"])!)
                ?? new Dictionary<string, string>();
        }

        private string GetOperationPath(string operationId)
        {
            if (!IsValidOperationId(operationId))
                throw ElympicsCliErrors.InvalidArgumentException($"--operation_id '{operationId}' is invalid.", "pass the operationId an apply returned.");
            return Path.Combine(OperationsDirectory, operationId + ".json");
        }

        private static bool IsValidOperationId(string? operationId) =>
            !string.IsNullOrEmpty(operationId) && operationId!.All(character => character is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_');

        private IEnumerable<PackageUpdateOperation> ReadOperations() =>
            Directory.Exists(OperationsDirectory)
                ? Directory.GetFiles(OperationsDirectory, "*.json").Select(TryReadOperation).Where(operation => operation != null).Select(operation => operation!)
                : Enumerable.Empty<PackageUpdateOperation>();

        private static PackageUpdateOperation? TryReadOperation(string file)
        {
            try
            {
                var operation = JsonConvert.DeserializeObject<PackageUpdateOperation>(File.ReadAllText(file));
                // Only records Save could have written: the ID names this file and every package has a name.
                if (operation == null || !IsValidOperationId(operation.operationId) || operation.operationId != Path.GetFileNameWithoutExtension(file))
                    return null;
                operation.packages ??= new List<PackageUpdate>();
                operation.warnings ??= new List<string>();
                if (operation.packages.Any(update => update?.name == null))
                    return null;
                foreach (var update in operation.packages)
                    update.warnings ??= new List<string>();
                return operation;
            }
            catch (Exception e) when (e is JsonException or IOException)
            {
                return null;
            }
        }

        private void Save(PackageUpdateOperation operation)
        {
            _ = Directory.CreateDirectory(OperationsDirectory);
            File.WriteAllText(GetOperationPath(operation.operationId), JsonConvert.SerializeObject(operation, Formatting.Indented));
        }
    }
}
