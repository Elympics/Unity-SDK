using System;
using System.Linq;
using System.Threading.Tasks;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.UnityCli.PackageUpdates;
using Unity.Pipeline.Commands;
using UnityEditor;

namespace Elympics.Editor.UnityCli
{
    public static class ElympicsPackagesCliCommands
    {
        internal static PackageUpdater Updater = new();

        [CliCommand("elympics_update_packages",
            "Update the project's com.daftmobile.* git packages by the SDK ref policy: a version tag moves to the newest version tag, a branch to its head "
            + "(a deleted branch falls back to develop), no ref pins the newest tag, a commit is kept; --pin and --track override that. "
            + "Reads refs with git ls-remote (never prompts). Applying rewrites Packages/manifest.json and starts a resolve that may reload the Editor: "
            + "it returns status \"started\" with an operationId to poll with --operation_id until \"done\". Versions are reported, not compared.",
            Tags = new[] { "elympics", "write" })]
        public static Task<object> UpdatePackages(
            [CliArg("dry_run", "Only plan: report what would change.")] bool dryRun = false,
            [CliArg("packages", "JSON array of package names to update. Defaults to every com.daftmobile.* git package.")] string packages = "",
            [CliArg("pin", "JSON object of explicit tags, e.g. '{\"com.daftmobile.elympics\":\"v0.29.0\"}'.")] string pin = "",
            [CliArg("track", "JSON object of explicit branches, e.g. '{\"com.daftmobile.elympics\":\"develop\"}'.")] string track = "",
            [CliArg("operation_id", "Return the state of an earlier apply instead of planning (read-only).")] string operationId = "") => CliRun.Async(async () =>
        {
            if (CliArgs.Optional(operationId) is { } existingOperationId)
                return Describe(Updater.ReadOperation(existingOperationId), false);

            var packageNames = CliArgs.ParseStringArray(packages, "packages");
            var pins = CliArgs.ParseStringObject(pin, "pin");
            var tracks = CliArgs.ParseStringObject(track, "track");
            var updates = await Updater.PlanAsync(packageNames, pins, tracks);

            var explicitError = updates.FirstOrDefault(update => update.error != null && update.error.code != PackageUpdater.RemoteUnreachable
                && (pins.ContainsKey(update.name) || tracks.ContainsKey(update.name)));
            if (explicitError != null)
                throw new ElympicsCliException(explicitError.error.code, $"{explicitError.name}: {explicitError.error.message}.", explicitError.error.fix);

            if (dryRun)
            {
                PackageUpdater.RedactCredentials(updates);
                return Describe(new PackageUpdateOperation
                {
                    status = updates.All(update => update.action == "keep") ? "up_to_date" : "planned",
                    packages = updates,
                }, true);
            }

            CliRun.RequireNotPlaying("Updating packages reloads scripts, which Play mode doesn't allow.");
            await UniTask.SwitchToMainThread();
            return Describe(Updater.Apply(updates), false);
        });

        private static object Describe(PackageUpdateOperation operation, bool dryRun) => new
        {
            operation.status,
            dryRun,
            operation.operationId,
            operation.startedAt,
            operation.finishedAt,
            operation.packages,
            warnings = operation.warnings.Concat(operation.packages.SelectMany(update => update.warnings.Select(warning => $"{update.name}: {warning}"))).ToArray(),
            compileErrors = EditorUtility.scriptCompilationFailed,
        };

        [InitializeOnLoadMethod]
        private static void FinalizeAfterReload() => EditorApplication.delayCall += () =>
        {
            try
            {
                Updater.FinalizePending();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        };
    }
}
