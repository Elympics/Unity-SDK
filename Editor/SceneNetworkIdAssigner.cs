using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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

        [InitializeOnLoadMethod]
        private static void RegisterValidationCallbacks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ValidateScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneSaved(Scene scene)
        {
            Debug.Log("Scene saved");
            ValidateScene(scene);
        }

        private static void ValidateScene(Scene scene)
        {
            var behaviours = SceneObjectsFinder.FindObjectsOfType<ElympicsBehaviour>(scene, true);
            AssignPredefinedNetworkIds(behaviours);
            CheckForEmptyManualIds(behaviours);
            CheckForOutOfRangeManualIds(behaviours);
            CheckForDuplicateNetworkIds(behaviours);
        }

        private static void CheckForOutOfRangeManualIds(List<ElympicsBehaviour> behaviours)
        {
            foreach (var behaviour in behaviours)
            {
                if (behaviour.autoAssignNetworkId)
                    continue;

                var id = behaviour.NetworkId;
                if (id is not ElympicsBehaviour.UndefinedNetworkId and (< NetworkIdConstants.ManualIdMin or > NetworkIdConstants.ManualIdMax))
                    ElympicsLogger.LogError($"Manual NetworkId {id} on {behaviour.gameObject.name} is out of range [{NetworkIdConstants.ManualIdMin}, {NetworkIdConstants.ManualIdMax}].", behaviour);
            }
        }

        internal static void ResetAllIds(List<ElympicsBehaviour> behaviours)
        {
            AssignPredefinedNetworkIds(behaviours);

            nextId = NetworkIdConstants.PredefinedBehaviourCount;
            ReassignAutoIds(behaviours);
            CheckForEmptyManualIds(behaviours);
            CheckForDuplicateNetworkIds(behaviours);
        }

        private static void AssignPredefinedNetworkIds(List<ElympicsBehaviour> behaviours)
        {
            // TODO: set it up so that the components from the Elympics prefab cannot be used in other places, seal the classes etc. ~dsygocki 2026-03-05
            foreach (var behaviour in behaviours)
                if (behaviour.TryGetComponent<ElympicsUnityPhysicsSimulator>(out var physicsSimulator) && physicsSimulator.GetType() == typeof(ElympicsUnityPhysicsSimulator))
                {
                    if (behaviour.networkId == NetworkIdConstants.PhysicsSimulatorNetworkId && behaviour.autoAssignNetworkId)
                        continue;
                    Undo.RecordObject(behaviour, "Re-assign predefined network ID");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    behaviour.networkId = NetworkIdConstants.PhysicsSimulatorNetworkId;
                    behaviour.autoAssignNetworkId = true;
                }
                else if (behaviour.TryGetComponent<ServerLogBehaviour>(out var logBehaviour) && logBehaviour.GetType() == typeof(ServerLogBehaviour))
                {
                    if (behaviour.networkId == NetworkIdConstants.ServerLogNetworkId && behaviour.autoAssignNetworkId)
                        continue;
                    Undo.RecordObject(behaviour, "Re-assign predefined network ID");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    behaviour.networkId = NetworkIdConstants.ServerLogNetworkId;
                    behaviour.autoAssignNetworkId = true;
                }
                else if (behaviour.TryGetComponent<DefaultServerHandler>(out var serverHandler) && serverHandler.GetType() == typeof(DefaultServerHandler))
                {
                    if (behaviour.networkId == NetworkIdConstants.DefaultServerHandlerNetworkId && behaviour.autoAssignNetworkId)
                        continue;
                    Undo.RecordObject(behaviour, "Re-assign predefined network ID");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    behaviour.networkId = NetworkIdConstants.DefaultServerHandlerNetworkId;
                    behaviour.autoAssignNetworkId = true;
                }
        }

        internal static bool IsPredefinedBehaviour(ElympicsBehaviour behaviour)
            => (behaviour.TryGetComponent<ElympicsUnityPhysicsSimulator>(out var physicsSimulator) && physicsSimulator.GetType() == typeof(ElympicsUnityPhysicsSimulator))
            || (behaviour.TryGetComponent<ServerLogBehaviour>(out var logBehaviour) && logBehaviour.GetType() == typeof(ServerLogBehaviour))
            || (behaviour.TryGetComponent<DefaultServerHandler>(out var serverHandler) && serverHandler.GetType() == typeof(DefaultServerHandler));

        private static int GetNextNetworkId()
        {
            if (nextId >= NetworkIdConstants.DefaultSceneObjectsReserved)
                ElympicsLogger.LogWarning($"Scene object NetworkId {nextId} exceeds reserved range "
                    + $"(max {NetworkIdConstants.DefaultSceneObjectsReserved - 1}). "
                    + "Consider increasing DefaultSceneObjectsReserved.");

            return nextId++;
        }

        private static void AssignNetworkId(ElympicsBehaviour behaviour)
        {
            var id = GetNextNetworkId();
            if (behaviour.networkId == id)
                return;
            Undo.RecordObject(behaviour, "Assign predefined network ID");
            behaviour.networkId = id;
        }

        private static void ReassignAutoIds(List<ElympicsBehaviour> behaviours)
        {
            var sortedBehaviours = new List<ElympicsBehaviour>();
            foreach (var behaviour in behaviours)
                if (behaviour.autoAssignNetworkId && !IsPredefinedBehaviour(behaviour))
                    sortedBehaviours.Add(behaviour);

            sortedBehaviours.Sort((x, y) => x.NetworkId.CompareTo(y.NetworkId));

            foreach (var behaviour in sortedBehaviours)
                AssignNetworkId(behaviour);
        }

        private static void CheckForEmptyManualIds(List<ElympicsBehaviour> behaviours)
        {
            foreach (var behaviour in behaviours)
                if (!behaviour.autoAssignNetworkId && behaviour.NetworkId == ElympicsBehaviour.UndefinedNetworkId)
                    ElympicsLogger.LogWarning($"Manual NetworkId not assigned on {behaviour.gameObject.name}. "
                        + "Use the inspector to assign an ID.", behaviour);
        }

        private static void CheckForDuplicateNetworkIds(List<ElympicsBehaviour> behaviours)
        {
            var behaviourNames = new Dictionary<int, string>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour.autoAssignNetworkId)
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
