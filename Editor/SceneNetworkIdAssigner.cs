using System.Collections.Generic;
using System.Linq;
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
        private static int nextId;
        private static int GetNextNetworkId()
        {
            if (nextId >= NetworkIdConstants.DefaultSceneObjectsReserved)
                ElympicsLogger.LogError($"Scene object NetworkId {nextId} exceeds reserved range "
                    + $"(max {NetworkIdConstants.DefaultSceneObjectsReserved - 1}). "
                    + "Consider increasing DefaultSceneObjectsReserved.");

            return nextId++;
        }

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
            AssignPredefinedNetworkIds(behaviours);

            nextId = NetworkIdConstants.PredefinedBehaviourCount;
            ReassignAutoIds(behaviours);
        }

        private static void AssignPredefinedNetworkIds(List<ElympicsBehaviour> behaviours)
        {
            // TODO: set it up so that the components from the Elympics prefab cannot be used in other places, seal the classes etc. ~dsygocki 2026-03-05
            foreach (var behaviour in behaviours)
                if (behaviour.GetComponent<ElympicsUnityPhysicsSimulator>() is not null)
                {
                    if (behaviour.NetworkId == NetworkIdConstants.PhysicsSimulatorNetworkId)
                        continue;
                    Undo.RecordObject(behaviour, "Re-assign predefined network ID");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    behaviour.NetworkId = NetworkIdConstants.PhysicsSimulatorNetworkId;
                }
                else if (behaviour.GetComponent<ServerLogBehaviour>() is not null)
                {
                    if (behaviour.NetworkId == NetworkIdConstants.ServerLogNetworkId)
                        continue;
                    Undo.RecordObject(behaviour, "Re-assign predefined network ID");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    behaviour.NetworkId = NetworkIdConstants.ServerLogNetworkId;
                }
                else if (behaviour.GetComponent<DefaultServerHandler>()?.GetType() == typeof(DefaultServerHandler))
                {
                    if (behaviour.NetworkId == NetworkIdConstants.DefaultServerHandlerNetworkId)
                        continue;
                    Undo.RecordObject(behaviour, "Re-assign predefined network ID");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    behaviour.NetworkId = NetworkIdConstants.DefaultServerHandlerNetworkId;
                }
        }

        private static void ReassignAutoIds(List<ElympicsBehaviour> behaviours)
        {
            var sortedBehaviours = behaviours
                .Where(behaviour => !IsPredefinedBehaviour(behaviour))
                .ToList();
            HierarchicalSorting.Sort(sortedBehaviours);
            foreach (var behaviour in sortedBehaviours)
            {
                var id = GetNextNetworkId();
                if (behaviour.NetworkId == id)
                    return;
                Undo.RecordObject(behaviour, "Assign predefined network ID");
                behaviour.NetworkId = id;
            }
        }
    }
}
