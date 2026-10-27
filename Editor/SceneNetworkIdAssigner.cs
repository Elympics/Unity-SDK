using System.Collections.Generic;
using Elympics.Core.Logger;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Elympics
{
    /// <summary>
    /// Editor-only static class responsible for auto-assigning NetworkIds to scene objects.
    /// Auto-assigned IDs use range [0, DefaultSceneObjectsReserved).
    /// Manual IDs use range [ManualIdMin, ManualIdMax] and are validated in ElympicsBehaviourEditor.
    /// Validates all scene behaviours on play mode entry and scene save.
    /// </summary>
    internal static class SceneNetworkIdAssigner
    {
        private static int? TryGetPredefinedIdFor(ElympicsBehaviour behaviour) =>
            behaviour.TryGetComponent<ElympicsUnityPhysicsSimulator>(out _)
                ? NetworkIdConstants.PhysicsSimulatorNetworkId
                : behaviour.TryGetComponent<ServerLogBehaviour>(out _)
                    ? NetworkIdConstants.ServerLogNetworkId
                    : behaviour.GetComponent<DefaultServerHandler>()?.GetType() == typeof(DefaultServerHandler)
                        ? NetworkIdConstants.DefaultServerHandlerNetworkId
                        : null;

        internal static bool IsPredefinedBehaviour(ElympicsBehaviour behaviour) => TryGetPredefinedIdFor(behaviour) is not null;

        [InitializeOnLoadMethod]
        private static void RegisterValidationCallbacks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ResetAllIds(SceneManager.GetActiveScene());
        }

        private static void OnSceneSaved(Scene scene) =>
            ResetAllIds(scene);

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.AdditiveWithoutLoading)
                return;
            ResetAllIds(scene);
        }

        internal static void ResetAllIds(Scene scene)
        {
            var behaviours = SceneObjectsFinder.FindObjectsOfType<ElympicsBehaviour>(scene, true);
            foreach (var (behaviour, id) in ComputeIds(behaviours))
            {
                if (id >= NetworkIdConstants.DefaultSceneObjectsReserved && !IsPredefinedBehaviour(behaviour))
                    ElympicsLogger.LogError($"Scene object NetworkId {id} exceeds reserved range "
                        + $"(max {NetworkIdConstants.DefaultSceneObjectsReserved - 1}). "
                        + "Consider increasing DefaultSceneObjectsReserved.");
                if (behaviour.NetworkId == id)
                    continue;
                Undo.RecordObject(behaviour, "Assign network ID");
                behaviour.NetworkId = id;
                PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
            }
        }

        /// <returns>The network IDs <see cref="ResetAllIds"/> would assign, without changing anything.</returns>
        internal static Dictionary<ElympicsBehaviour, int> ComputeIds(List<ElympicsBehaviour> behaviours)
        {
            var ids = new Dictionary<ElympicsBehaviour, int>();
            var autoBehaviours = new List<ElympicsBehaviour>();
            foreach (var behaviour in behaviours)
                if (TryGetPredefinedIdFor(behaviour) is { } predefinedId)
                    ids[behaviour] = predefinedId;
                else
                    autoBehaviours.Add(behaviour);

            HierarchicalSorting.Sort(autoBehaviours);
            var id = NetworkIdConstants.PredefinedBehaviourCount;
            foreach (var behaviour in autoBehaviours)
                ids[behaviour] = id++;
            return ids;
        }
    }
}
