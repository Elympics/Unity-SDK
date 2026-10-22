using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Elympics.Editor.Launcher;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        [CliCommand("elympics_reset_network_ids",
            "Reassign the automatic network IDs of every ElympicsBehaviour in the active scene (like Tools > Elympics > Reset Network Ids). "
            + "Doesn't save the scene unless --save true.",
            Tags = new[] { "elympics", "write" })]
        public static object ResetNetworkIds([CliArg("save", "Save the scene afterwards.")] bool save = false) => CliRun.Sync(() =>
        {
            RequireEditMode();
            var scene = ResolveOpenScene(string.Empty);
            var behaviours = SceneObjectsFinder.FindObjectsOfType<ElympicsBehaviour>(scene, true);
            var before = behaviours.ToDictionary(behaviour => behaviour, behaviour => behaviour.NetworkId);
            SceneNetworkIdAssigner.ResetAllIds(scene);
            var changed = behaviours.Count(behaviour => behaviour.NetworkId != before[behaviour]);
            if (changed > 0)
                _ = EditorSceneManager.MarkSceneDirty(scene);
            var saved = save && Save(scene);
            return new { status = "reset", scene = scene.path, behaviourCount = behaviours.Count, changed, sceneDirty = scene.isDirty, saved };
        });

        [CliCommand("elympics_scene_setup",
            "Turn an open scene into an Elympics gameplay scene: add the Elympics system prefab, optionally the 2D physics simulator, add the scene to Build Settings, "
            + "make it the active game's gameplay scene and assign network IDs. Refuses a scene that already has an Elympics system. Returns elympics_validate_scene's findings.",
            Tags = new[] { "elympics", "write" })]
        public static object SceneSetup(
            [CliArg("scene", SceneDescription)] string scene = "",
            [CliArg("add_to_build", "Add the scene to Build Settings (or enable it there).")] bool addToBuild = true,
            [CliArg("set_gameplay_scene", "Make the scene the active game config's gameplay scene.")] bool setGameplayScene = true,
            [CliArg("create_if_missing", "Create ElympicsConfig if the project has none (--set_gameplay_scene still needs a game config).")] bool createIfMissing = false,
            [CliArg("add_physics2d_simulator", "auto (add it if the scene has 2D colliders or rigidbodies), true or false.")] string addPhysics2DSimulator = "auto",
            [CliArg("save", "Save the scene afterwards.")] bool save = false) => CliRun.Sync(() =>
        {
            RequireEditMode();
            var addSimulator = addPhysics2DSimulator?.Trim().ToLowerInvariant() switch
            {
                "auto" or "" or null => (bool?)null,
                "true" => true,
                "false" => false,
                _ => throw ElympicsCliErrors.InvalidArgumentException($"Unknown --add_physics2d_simulator '{addPhysics2DSimulator}'.", "use auto, true or false."),
            };
            var target = ResolveOpenScene(scene);
            if (SceneObjectsFinder.FindObjectsOfType<GameSceneManager>(target, true).Count > 0)
                throw new ElympicsCliException(ElympicsCliErrors.SystemAlreadyPresent, $"The scene {target.path} already has an Elympics system.",
                    "check it with elympics_validate_scene instead; delete the Elympics system first to set the scene up again.");
            if ((addToBuild || setGameplayScene || save) && string.IsNullOrEmpty(target.path))
                throw ElympicsCliErrors.InvalidArgumentException("The scene has never been saved, so it has no path for Build Settings or the game config.",
                    "save it first (save_scene), or pass --add_to_build false --set_gameplay_scene false.");
            var gameConfig = setGameplayScene ? CliGames.RequireActiveGame(CliGames.RequireConfig(createIfMissing)) : null;

            var prefab = UnityEngine.Resources.Load<GameSceneManager>(ElympicsSystemPrefabPath)
                ?? throw new ElympicsCliException(ElympicsCliErrors.Internal, $"The Elympics system prefab (Resources/{ElympicsSystemPrefabPath}) is missing.", null);
            var system = (UnityEngine.GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, target);
            Undo.RegisterCreatedObjectUndo(system, "Set up Elympics scene");

            var roots = target.GetRootGameObjects();
            var simulatorAdded = false;
            if (addSimulator ?? Uses2DPhysics(roots))
            {
                var simulatorTarget = Physics2DSimulatorSceneCheck.FindPhysics2DSimulatorTarget(roots);
                if (simulatorTarget != null)
                {
                    _ = Undo.AddComponent<ElympicsUnityPhysics2DSimulator>(simulatorTarget);
                    simulatorAdded = true;
                }
            }

            var addedToBuild = addToBuild && AddToBuildSettings(target.path);
            if (gameConfig != null)
            {
                Undo.RecordObject(gameConfig, "Set Elympics gameplay scene");
                gameConfig.gameplaySceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(target.path);
                gameConfig.gameplayScene = target.path;
                EditorUtility.SetDirty(gameConfig);
                AssetDatabase.SaveAssets();
            }

            SceneNetworkIdAssigner.ResetAllIds(target);
            _ = EditorSceneManager.MarkSceneDirty(target);
            var saved = save && Save(target);
            return new
            {
                status = "set_up",
                scene = target.path,
                systemObject = system.name,
                physics2DSimulatorAdded = simulatorAdded,
                addedToBuild,
                gameplaySceneOf = gameConfig?.GameId,
                saved,
                validation = Validate(target, Array.Empty<string>()),
            };
        });

        private const string ElympicsSystemPrefabPath = "Elympics";

        private static bool Uses2DPhysics(UnityEngine.GameObject[] roots) =>
            roots.Any(root => root.GetComponentInChildren<UnityEngine.Rigidbody2D>(true) != null || root.GetComponentInChildren<UnityEngine.Collider2D>(true) != null);

        /// <returns>Whether Build Settings changed.</returns>
        private static bool AddToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var existing = scenes.FindIndex(buildScene => buildScene.path == scenePath);
            if (existing >= 0 && scenes[existing].enabled)
                return false;
            if (existing >= 0)
                scenes[existing] = new EditorBuildSettingsScene(scenePath, true);
            else
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            return true;
        }

        private static bool Save(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.path))
                throw ElympicsCliErrors.InvalidArgumentException("The scene has never been saved, so --save true has no path to save it to.",
                    "save it with save_scene and a path instead.");
            return EditorSceneManager.SaveScene(scene);
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new ElympicsCliException(ElympicsCliErrors.EditorPlaying, "The Editor is in Play mode, where scene changes are lost.",
                    "stop Play mode (editor_stop), then retry.");
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
