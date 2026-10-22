using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Elympics.Editor.Launcher;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Elympics.Editor.UnityCli
{
    public static class ElympicsSceneCliCommands
    {
        private const string SceneDescription = "Path of an open scene (e.g. Assets/Scenes/Game.unity). Defaults to the active scene.";

        [CliCommand("elympics_validate_scene",
            "Check an open Elympics gameplay scene without changing it: Elympics system, network IDs, build settings, boot scene, 2D physics simulator and "
            + "synchronizer conflicts. Every finding has a code, a severity and a fix. Returns status \"issues\" when there are errors or warnings.",
            Tags = new[] { "elympics", "read" })]
        public static object ValidateScene(
            [CliArg("scene", SceneDescription)] string scene = "",
            [CliArg("ignore", "JSON array of finding codes to ignore, e.g. '[\"UPDATABLE_NOT_PREDICTABLE\"]'. Ignored findings are still listed under \"ignored\".")]
            string ignore = "") => CliRun.Sync(() =>
        {
            var ignoredCodes = new HashSet<string>(CliArgs.ParseStringArray(ignore, "ignore"), StringComparer.OrdinalIgnoreCase);
            return Validate(ResolveOpenScene(scene), ignoredCodes);
        });

        internal static object Validate(Scene scene, ICollection<string> ignoredCodes)
        {
            var config = ElympicsConfig.Load();
            var findings = ElympicsSceneValidator.Validate(scene, config, ElympicsLauncherBuildValidation.GetEnabledBuildScenes());
            var reported = findings.Where(finding => !ignoredCodes.Contains(finding.Code)).ToList();
            var errors = reported.Count(finding => finding.Severity == SceneFindingSeverity.Error);
            var warnings = reported.Count - errors;
            return new
            {
                status = reported.Count == 0 ? "ok" : "issues",
                scene = scene.path,
                gameId = config?.GetCurrentGameConfig()?.GameId,
                summary = new { errors, warnings, ignored = findings.Count - reported.Count },
                findings = reported.Select(ToResult).ToArray(),
                ignored = findings.Where(finding => ignoredCodes.Contains(finding.Code)).Select(ToResult).ToArray(),
            };
        }

        private static object ToResult(SceneFinding finding) => new
        {
            code = finding.Code,
            severity = finding.Severity.ToString().ToLowerInvariant(),
            message = finding.Message,
            fix = finding.Fix,
            objectPath = finding.ObjectPath,
        };

        [CliCommand("elympics_synced_vars",
            "List the synchronized ElympicsVars of every ElympicsBehaviour in an open scene, with their serialized sizes in bytes. "
            + "Values are read without changing state; sizes of variable-length vars (strings, lists, arrays) depend on their current values.",
            Tags = new[] { "elympics", "read" })]
        public static object SyncedVars([CliArg("scene", SceneDescription)] string scene = "") => CliRun.Sync(() =>
        {
            var resolvedScene = ResolveOpenScene(scene);
            var behaviours = SceneObjectsFinder.FindObjectsOfType<ElympicsBehaviour>(resolvedScene, true);
            var results = behaviours.Select(DescribeBehaviour).ToArray();
            return new
            {
                status = "ok",
                scene = resolvedScene.path,
                inPlayMode = EditorApplication.isPlaying,
                behaviourCount = results.Length,
                varCount = results.Sum(result => result.vars.Length),
                totalBytes = results.Sum(result => result.totalBytes),
                behaviours = results,
            };
        });

        private sealed class VarResult
        {
            public string component;
            public string field;
            public string type;
            public int? bytes;
            public bool variableLength;
        }

        private sealed class BehaviourResult
        {
            public string objectPath;
            public int networkId;
            public int totalBytes;
            public VarResult[] vars;
        }

        private static BehaviourResult DescribeBehaviour(ElympicsBehaviour behaviour)
        {
            var vars = new List<VarResult>();
            foreach (var observable in behaviour.GetComponents<IObservable>())
                foreach (var field in observable.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (!typeof(ElympicsVar).IsAssignableFrom(field.FieldType) || field.GetValue(observable) is not ElympicsVar value || !value.EnabledSynchronization)
                        continue;
                    var typeName = value.GetType().Name;
                    vars.Add(new VarResult
                    {
                        component = observable.GetType().Name,
                        field = field.Name,
                        type = typeName,
                        bytes = TryMeasure(value),
                        variableLength = typeName.StartsWith(nameof(ElympicsString), StringComparison.Ordinal)
                            || typeName.StartsWith("ElympicsList", StringComparison.Ordinal)
                            || typeName.StartsWith("ElympicsArray", StringComparison.Ordinal),
                    });
                }

            return new BehaviourResult
            {
                objectPath = ElympicsSceneValidator.GetPath(behaviour.transform),
                networkId = behaviour.NetworkId,
                totalBytes = vars.Sum(result => result.bytes ?? 0),
                vars = vars.ToArray(),
            };
        }

        private static int? TryMeasure(ElympicsVar value)
        {
            try
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                value.Serialize(writer);
                writer.Flush();
                return (int)stream.Length;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static Scene ResolveOpenScene(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                var active = SceneManager.GetActiveScene();
                if (!active.IsValid() || !active.isLoaded)
                    throw new ElympicsCliException(ElympicsCliErrors.NoActiveScene, "No scene is loaded.", "open the gameplay scene with open_scene --scene_path <path>.");
                return active;
            }

            var scene = SceneManager.GetSceneByPath(scenePath.Trim());
            if (!scene.IsValid() || !scene.isLoaded)
                throw new ElympicsCliException(ElympicsCliErrors.SceneNotOpen, $"The scene {scenePath} isn't open.",
                    $"open it first with open_scene --scene_path {scenePath.Trim()}, or omit --scene to use the active scene.");
            return scene;
        }
    }
}
