#if UNITY_EDITOR
using System.Collections.Generic;
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

        internal static void AssignPredefinedNetworkIds(List<ElympicsBehaviour> behaviours)
        {
            foreach (var behaviour in behaviours)
            {
                if (behaviour.TryGetComponent<ElympicsUnityPhysicsSimulator>(out _))
                {
                    behaviour.networkId = NetworkIdConstants.PhysicsSimulatorNetworkId;
                    behaviour.autoAssignNetworkId = true;
                    EditorUtility.SetDirty(behaviour);
                }
                else if (behaviour.TryGetComponent<ServerLogBehaviour>(out _))
                {
                    behaviour.networkId = NetworkIdConstants.ServerLogNetworkId;
                    behaviour.autoAssignNetworkId = true;
                    EditorUtility.SetDirty(behaviour);
                }
                else if (behaviour.TryGetComponent<DefaultServerHandler>(out _))
                {
                    behaviour.networkId = NetworkIdConstants.DefaultServerHandlerNetworkId;
                    behaviour.autoAssignNetworkId = true;
                    EditorUtility.SetDirty(behaviour);
                }
            }
        }

        private static bool IsPredefinedBehaviour(ElympicsBehaviour behaviour)
            => behaviour.TryGetComponent<ElympicsUnityPhysicsSimulator>(out _)
               || behaviour.TryGetComponent<ServerLogBehaviour>(out _)
               || behaviour.TryGetComponent<DefaultServerHandler>(out _);

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
            behaviour.networkId = GetNextNetworkId();
            EditorUtility.SetDirty(behaviour);
        }

        private static void ReassignAutoIds(List<ElympicsBehaviour> behaviours)
        {
            var sortedBehaviours = new List<ElympicsBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour.autoAssignNetworkId && !IsPredefinedBehaviour(behaviour))
                    sortedBehaviours.Add(behaviour);
            }

            sortedBehaviours.Sort((x, y) => x.NetworkId.CompareTo(y.NetworkId));

            foreach (var behaviour in sortedBehaviours)
                AssignNetworkId(behaviour);
        }

        private static void CheckForEmptyManualIds(List<ElympicsBehaviour> behaviours)
        {
            foreach (var behaviour in behaviours)
            {
                if (!behaviour.autoAssignNetworkId && behaviour.NetworkId == ElympicsBehaviour.UndefinedNetworkId)
                    ElympicsLogger.LogWarning($"Manual NetworkId not assigned on {behaviour.gameObject.name}. "
                                              + "Use the inspector to assign an ID.",
                        behaviour);
            }
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
#endif
