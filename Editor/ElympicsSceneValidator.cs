#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics.Editor
{
    internal enum SceneFindingSeverity
    {
        Error,
        Warning,
    }

    internal sealed class SceneFinding
    {
        public string Code { get; }
        public SceneFindingSeverity Severity { get; }
        public string Message { get; }
        public string Fix { get; }
        public string? ObjectPath { get; }

        public SceneFinding(string code, SceneFindingSeverity severity, string message, string fix, string? objectPath = null)
        {
            Code = code;
            Severity = severity;
            Message = message;
            Fix = fix;
            ObjectPath = objectPath;
        }
    }

    /// <summary>Checks an Elympics gameplay scene without changing it. Every finding names what to do next.</summary>
    internal static class ElympicsSceneValidator
    {
        public const string NoActiveGameConfig = "NO_ACTIVE_GAME_CONFIG";
        public const string NoElympicsSystem = "NO_ELYMPICS_SYSTEM";
        public const string MultipleElympicsSystems = "MULTIPLE_ELYMPICS_SYSTEMS";
        public const string NetworkIdUnassigned = "NETWORK_ID_UNASSIGNED";
        public const string NetworkIdDuplicate = "NETWORK_ID_DUPLICATE";
        public const string NetworkIdsStale = "NETWORK_IDS_STALE";
        public const string SceneNotGameplayScene = "SCENE_NOT_GAMEPLAY_SCENE";
        public const string SceneNotInBuild = "SCENE_NOT_IN_BUILD";
        public const string BootSceneNotSet = "BOOT_SCENE_NOT_SET";
        public const string BootSceneProblem = "BOOT_SCENE_PROBLEM";
        public const string Physics2DSimulatorMissing = "PHYSICS2D_SIMULATOR_MISSING";
        public const string TransformRigidbodySyncConflict = "TRANSFORM_RIGIDBODY_SYNC_CONFLICT";
        public const string UpdatableNotPredictable = "UPDATABLE_NOT_PREDICTABLE";

        private const string ResetIdsFix = "run elympics_reset_network_ids (or " + ElympicsEditorMenuPaths.ResetIdsMenuPath + "), then save the scene.";

        public static List<SceneFinding> Validate(Scene scene, ElympicsConfig? config, IReadOnlyCollection<string> enabledBuildScenes)
        {
            var findings = new List<SceneFinding>();
            var activeGame = config?.GetCurrentGameConfig();
            if (activeGame == null)
                findings.Add(new SceneFinding(NoActiveGameConfig, SceneFindingSeverity.Error,
                    config == null ? "The project has no ElympicsConfig." : "ElympicsConfig lists no game configs.",
                    "create or register a game config with elympics_config_select_game --create_if_missing true --config_asset <path>."));

            CheckGameplayScene(scene, activeGame, enabledBuildScenes, findings);
            if (config != null)
                CheckBootScene(config, enabledBuildScenes, findings);
            CheckElympicsSystem(scene, findings);

            var behaviours = SceneObjectsFinder.FindObjectsOfType<ElympicsBehaviour>(scene, true);
            CheckNetworkIds(behaviours, findings);
            CheckBehaviours(behaviours, findings);
#if !ELYMPICS_DISABLE_PHYSICS2D_SCENE_CHECK
            if (Physics2DSimulatorSceneCheck.LacksPhysics2DSimulator(scene.GetRootGameObjects()))
                findings.Add(new SceneFinding(Physics2DSimulatorMissing, SceneFindingSeverity.Error,
                    $"The scene uses 2D physics but has no {nameof(ElympicsUnityPhysics2DSimulator)}, so its 2D physics doesn't follow Elympics ticks.",
                    $"add it with {ElympicsEditorMenuPaths.AddPhysics2DSimulator} (or elympics_scene_setup --add_physics2d_simulator true on a new scene); "
                    + "if the scene only uses triggers and doesn't need it, define ELYMPICS_DISABLE_PHYSICS2D_SCENE_CHECK or ignore this code."));
#endif
            return findings;
        }

        private static void CheckGameplayScene(Scene scene, ElympicsGameConfig? activeGame, IReadOnlyCollection<string> enabledBuildScenes, List<SceneFinding> findings)
        {
            if (activeGame == null)
                return;
            var gameplayScene = activeGame.GameplayScene;
            if (!string.IsNullOrEmpty(gameplayScene) && gameplayScene != scene.path)
                findings.Add(new SceneFinding(SceneNotGameplayScene, SceneFindingSeverity.Warning,
                    $"The scene isn't the gameplay scene of the active game {activeGame.GameName} ({gameplayScene}).",
                    "validate the gameplay scene instead, or make this scene the gameplay scene (elympics_scene_setup --set_gameplay_scene true)."));
            if (string.IsNullOrEmpty(gameplayScene))
                findings.Add(new SceneFinding(SceneNotGameplayScene, SceneFindingSeverity.Error,
                    $"The active game {activeGame.GameName} has no gameplay scene.",
                    "set it in the game config, or run elympics_scene_setup --set_gameplay_scene true on the gameplay scene."));
            else if (!enabledBuildScenes.Contains(gameplayScene))
                findings.Add(new SceneFinding(SceneNotInBuild, SceneFindingSeverity.Error,
                    $"The gameplay scene {gameplayScene} isn't enabled in Build Settings.",
                    $"add it with add_scene_to_build --scene_path {gameplayScene} (or elympics_scene_setup --add_to_build true)."));
        }

        private static void CheckBootScene(ElympicsConfig config, IReadOnlyCollection<string> enabledBuildScenes, List<SceneFinding> findings)
        {
            var problem = ElympicsBootScene.FindProblem(config, enabledBuildScenes.Contains);
            if (problem == null)
                return;
            if (problem == ElympicsBootScene.NotSetWarning)
                findings.Add(new SceneFinding(BootSceneNotSet, SceneFindingSeverity.Warning, problem,
                    "set ElympicsConfig > Boot scene to a separate scene in Build Settings if matches should return to it."));
            else
                findings.Add(new SceneFinding(BootSceneProblem, SceneFindingSeverity.Error, problem,
                    "point ElympicsConfig > Boot scene at a separate scene that is enabled in Build Settings, or clear it."));
        }

        private static void CheckElympicsSystem(Scene scene, List<SceneFinding> findings)
        {
            var systems = SceneObjectsFinder.FindObjectsOfType<GameSceneManager>(scene, true);
            if (systems.Count == 0)
                findings.Add(new SceneFinding(NoElympicsSystem, SceneFindingSeverity.Error, "The scene has no Elympics system (no GameSceneManager).",
                    $"run elympics_scene_setup, or add it with {ElympicsEditorMenuPaths.MouseActionCreateElympicsSystem}."));
            else if (systems.Count > 1)
                findings.Add(new SceneFinding(MultipleElympicsSystems, SceneFindingSeverity.Error,
                    $"The scene has {systems.Count} Elympics systems: {string.Join(", ", systems.Select(system => GetPath(system.transform)))}.",
                    "keep a single Elympics system prefab instance and delete the others.", GetPath(systems[1].transform)));
        }

        private static void CheckNetworkIds(List<ElympicsBehaviour> behaviours, List<SceneFinding> findings)
        {
            foreach (var behaviour in behaviours.Where(behaviour => behaviour.NetworkId == ElympicsBehaviour.UndefinedNetworkId))
                findings.Add(new SceneFinding(NetworkIdUnassigned, SceneFindingSeverity.Error, "An ElympicsBehaviour has no network ID.", ResetIdsFix,
                    GetPath(behaviour.transform)));

            foreach (var group in behaviours.Where(behaviour => behaviour.NetworkId != ElympicsBehaviour.UndefinedNetworkId).GroupBy(behaviour => behaviour.NetworkId)
                .Where(group => group.Count() > 1))
                findings.Add(new SceneFinding(NetworkIdDuplicate, SceneFindingSeverity.Error,
                    $"{group.Count()} ElympicsBehaviours share network ID {group.Key}: {string.Join(", ", group.Select(behaviour => GetPath(behaviour.transform)))}.",
                    ResetIdsFix, GetPath(group.First().transform)));

            var expected = SceneNetworkIdAssigner.ComputeIds(behaviours);
            var stale = behaviours.Count(behaviour => behaviour.NetworkId != expected[behaviour]);
            if (stale > 0)
                findings.Add(new SceneFinding(NetworkIdsStale, SceneFindingSeverity.Warning,
                    $"{stale} network ID(s) differ from what the automatic assignment would give.", ResetIdsFix));
        }

        private static void CheckBehaviours(List<ElympicsBehaviour> behaviours, List<SceneFinding> findings)
        {
            foreach (var behaviour in behaviours)
            {
                if (HasTransformRigidbodySyncConflict(behaviour))
                    findings.Add(new SceneFinding(TransformRigidbodySyncConflict, SceneFindingSeverity.Error,
                        "The object synchronizes both its Transform and its Rigidbody, which fight over the position.",
                        $"remove the {nameof(ElympicsTransformSynchronizer)}; the Rigidbody synchronizer already syncs the position.", GetPath(behaviour.transform)));
                if (IsUpdatableButNotPredictable(behaviour))
                    findings.Add(new SceneFinding(UpdatableNotPredictable, SceneFindingSeverity.Warning,
                        "ElympicsUpdate of this object doesn't run on clients, because the object isn't predictable for any player.",
                        "set Predictable For on the ElympicsBehaviour if clients should predict it; otherwise ignore this code.", GetPath(behaviour.transform)));
            }
        }

        internal static bool HasTransformRigidbodySyncConflict(ElympicsBehaviour behaviour) =>
            behaviour.TryGetComponent<ElympicsTransformSynchronizer>(out _)
            && (behaviour.TryGetComponent<ElympicsRigidBodySynchronizer>(out _) || behaviour.TryGetComponent<ElympicsRigidBody2DSynchronizer>(out _));

        internal static bool IsUpdatableButNotPredictable(ElympicsBehaviour behaviour) =>
            behaviour.TryGetComponent<IUpdatable>(out _)
            && (behaviour.predictableFor == ElympicsPlayer.World || behaviour.predictableFor == ElympicsPlayer.Invalid)
            && !behaviour.isUpdatableForNonOwners;

        internal static string GetPath(Transform transform)
        {
            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }
    }
}
