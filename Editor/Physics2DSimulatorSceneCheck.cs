#nullable enable

using System.Collections.Generic;
using Elympics.Core.Logger;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics.Editor
{
    /// <summary>
    /// Reports Elympics scenes that use 2D physics without <see cref="ElympicsUnityPhysics2DSimulator"/>: their 2D physics would run on
    /// Unity's timer instead of Elympics ticks. Runs for each scene of a build (as an error) and when entering Play Mode (as a warning).
    /// The build isn't failed, because scenes with only trigger colliders can be fine without it.
    /// </summary>
    internal sealed class Physics2DSimulatorSceneCheck : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport? report)
        {
            if (!LacksPhysics2DSimulator(scene.GetRootGameObjects()))
                return;
            var message = $"Scene {scene.path} uses 2D physics but has no {nameof(ElympicsUnityPhysics2DSimulator)}, so its 2D physics doesn't follow Elympics ticks. "
                + $"Add it to the Physics object of the Elympics System with {ElympicsEditorMenuPaths.AddPhysics2DSimulator}.";
            // The report is null when entering Play Mode.
            if (report != null)
                ElympicsLogger.LogError(message);
            else
                ElympicsLogger.LogWarning(message);
        }

        internal static bool LacksPhysics2DSimulator(IReadOnlyCollection<GameObject> roots) =>
            !HasInChildren<ElympicsUnityPhysics2DSimulator>(roots)
            && HasInChildren<ElympicsBehavioursManager>(roots)
            && (HasInChildren<Rigidbody2D>(roots) || HasInChildren<Collider2D>(roots));

        [MenuItem(ElympicsEditorMenuPaths.AddPhysics2DSimulator, priority = 6)]
        private static void AddPhysics2DSimulator()
        {
            var target = FindPhysics2DSimulatorTarget(SceneManager.GetActiveScene().GetRootGameObjects());
            if (target == null)
                return;
            _ = Undo.AddComponent<ElympicsUnityPhysics2DSimulator>(target);
            _ = EditorSceneManager.MarkSceneDirty(target.scene);
        }

        [MenuItem(ElympicsEditorMenuPaths.AddPhysics2DSimulator, true)]
        private static bool CanAddPhysics2DSimulator() =>
            !EditorApplication.isPlaying && FindPhysics2DSimulatorTarget(SceneManager.GetActiveScene().GetRootGameObjects()) != null;

        /// <returns>
        /// The object with <see cref="ElympicsUnityPhysicsSimulator"/> (the Physics object of the Elympics System), or null if there is none
        /// or the scene already has <see cref="ElympicsUnityPhysics2DSimulator"/>. There's no fallback to another object, because the
        /// simulator requires an <see cref="ElympicsBehaviour"/>, which would make that object networked.
        /// </returns>
        internal static GameObject? FindPhysics2DSimulatorTarget(IReadOnlyCollection<GameObject> roots)
        {
            if (HasInChildren<ElympicsUnityPhysics2DSimulator>(roots))
                return null;
            foreach (var root in roots)
            {
                var simulator = root.GetComponentInChildren<ElympicsUnityPhysicsSimulator>(true);
                if (simulator != null)
                    return simulator.gameObject;
            }
            return null;
        }

        private static bool HasInChildren<T>(IReadOnlyCollection<GameObject> roots) where T : Component
        {
            foreach (var root in roots)
                if (root.GetComponentInChildren<T>(true) != null)
                    return true;
            return false;
        }
    }
}
