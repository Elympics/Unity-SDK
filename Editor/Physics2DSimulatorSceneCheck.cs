using System.Collections.Generic;
using System.Linq;
using Elympics.Core.Logger;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics.Editor
{
    /// <summary>
    /// Warns about Elympics scenes that use 2D physics without <see cref="ElympicsUnityPhysics2DSimulator"/>: their 2D physics would run on
    /// Unity's timer instead of Elympics ticks. Runs for each scene of a build and when entering Play Mode.
    /// </summary>
    internal sealed class Physics2DSimulatorSceneCheck : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (LacksPhysics2DSimulator(scene.GetRootGameObjects()))
                ElympicsLogger.LogWarning($"Scene {scene.path} uses 2D physics but has no {nameof(ElympicsUnityPhysics2DSimulator)}, "
                    + "so its 2D physics doesn't follow Elympics ticks. Add the component to the Physics object of the Elympics System.");
        }

        internal static bool LacksPhysics2DSimulator(IReadOnlyCollection<GameObject> roots)
        {
            bool Contains<T>() where T : Component => roots.Any(root => root.GetComponentInChildren<T>(true) != null);
            return Contains<ElympicsBehavioursManager>()
                && (Contains<Rigidbody2D>() || Contains<Collider2D>())
                && !Contains<ElympicsUnityPhysics2DSimulator>();
        }
    }
}
