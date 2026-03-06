using System.Collections.Generic;
using System.Linq;
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

        internal static bool IsPredefinedBehaviour(ElympicsBehaviour behaviour)
            => (behaviour.TryGetComponent<ElympicsUnityPhysicsSimulator>(out var physicsSimulator) && physicsSimulator.GetType() == typeof(ElympicsUnityPhysicsSimulator))
               || (behaviour.TryGetComponent<ServerLogBehaviour>(out var logBehaviour) && logBehaviour.GetType() == typeof(ServerLogBehaviour))
               || (behaviour.TryGetComponent<DefaultServerHandler>(out var serverHandler) && serverHandler.GetType() == typeof(DefaultServerHandler));

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
            CheckForEmptyManualIds(behaviours);
            CheckForOutOfRangeManualIds(behaviours);
            CheckForDuplicateNetworkIds(behaviours);
        }

        private static void AssignPredefinedNetworkIds(List<ElympicsBehaviour> behaviours)
        {
            // TODO: set it up so that the components from the Elympics prefab cannot be used in other places, seal the classes etc. ~dsygocki 2026-03-05
            foreach (var behaviour in behaviours)
                if (behaviour.TryGetComponent<ElympicsUnityPhysicsSimulator>(out var physicsSimulator) && physicsSimulator.GetType() == typeof(ElympicsUnityPhysicsSimulator))
                {
                    if (behaviour.networkId == NetworkIdConstants.PhysicsSimulatorNetworkId && behaviour.AutoAssignNetworkId)
                        continue;
                    Undo.RecordObject(behaviour, "Re-assign predefined network ID");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    behaviour.networkId = NetworkIdConstants.PhysicsSimulatorNetworkId;
                    behaviour.AutoAssignNetworkId = true;
                }
                else if (behaviour.TryGetComponent<ServerLogBehaviour>(out var logBehaviour) && logBehaviour.GetType() == typeof(ServerLogBehaviour))
                {
                    if (behaviour.networkId == NetworkIdConstants.ServerLogNetworkId && behaviour.AutoAssignNetworkId)
                        continue;
                    Undo.RecordObject(behaviour, "Re-assign predefined network ID");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    behaviour.networkId = NetworkIdConstants.ServerLogNetworkId;
                    behaviour.AutoAssignNetworkId = true;
                }
                else if (behaviour.TryGetComponent<DefaultServerHandler>(out var serverHandler) && serverHandler.GetType() == typeof(DefaultServerHandler))
                {
                    if (behaviour.networkId == NetworkIdConstants.DefaultServerHandlerNetworkId && behaviour.AutoAssignNetworkId)
                        continue;
                    Undo.RecordObject(behaviour, "Re-assign predefined network ID");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    behaviour.networkId = NetworkIdConstants.DefaultServerHandlerNetworkId;
                    behaviour.AutoAssignNetworkId = true;
                }
        }

        private static void ReassignAutoIds(List<ElympicsBehaviour> behaviours)
        {
            var sortedBehaviours = behaviours
                .Where(behaviour => behaviour.AutoAssignNetworkId && !IsPredefinedBehaviour(behaviour))
                .ToList();
            HierarchicalSorting.Sort(sortedBehaviours);
            foreach (var behaviour in sortedBehaviours)
            {
                var id = GetNextNetworkId();
                if (behaviour.networkId == id)
                    return;
                Undo.RecordObject(behaviour, "Assign predefined network ID");
                behaviour.networkId = id;
            }
        }

        private static void CheckForEmptyManualIds(List<ElympicsBehaviour> behaviours)
        {
            foreach (var behaviour in behaviours)
                if (!behaviour.AutoAssignNetworkId && behaviour.NetworkId == ElympicsBehaviour.UndefinedNetworkId)
                    ElympicsLogger.LogError($"Manual NetworkId not assigned on {behaviour.gameObject.name}. "
                        + "Use the inspector to assign an ID.", behaviour);
        }

        private static void CheckForOutOfRangeManualIds(List<ElympicsBehaviour> behaviours)
        {
            foreach (var behaviour in behaviours)
            {
                if (behaviour.AutoAssignNetworkId)
                    continue;

                var id = behaviour.NetworkId;
                if (id is not ElympicsBehaviour.UndefinedNetworkId and (< NetworkIdConstants.ManualIdMin or > NetworkIdConstants.ManualIdMax))
                    ElympicsLogger.LogError($"Manual NetworkId {id} on {behaviour.gameObject.name} is out of range [{NetworkIdConstants.ManualIdMin}, {NetworkIdConstants.ManualIdMax}].", behaviour);
            }
        }

        private static void CheckForDuplicateNetworkIds(List<ElympicsBehaviour> behaviours)
        {
            var behaviourNames = new Dictionary<int, string>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour.AutoAssignNetworkId)
                    continue;

                var networkId = behaviour.NetworkId;
                if (networkId == ElympicsBehaviour.UndefinedNetworkId)
                    continue;

                if (behaviourNames.TryGetValue(networkId, out var previousBehaviourName))
                {
                    ElympicsLogger.LogError($"Repeated network ID: {networkId} "
                        + $"(in object {behaviour.gameObject.name})!\n"
                        + $"Already used in object {previousBehaviourName}.");
                    continue;
                }

                behaviourNames.Add(networkId, behaviour.gameObject.name);
            }
        }
    }
}
