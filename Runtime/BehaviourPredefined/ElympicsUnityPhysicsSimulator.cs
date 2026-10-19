using UnityEngine;

namespace Elympics
{
    /// <summary>Steps the scene's 3D physics once per Elympics tick. 2D physics needs <c>ElympicsUnityPhysics2DSimulator</c> (assembly Elympics.Physics2D).</summary>
    [DisallowMultipleComponent]
    public sealed class ElympicsUnityPhysicsSimulator : ElympicsPhysicsSimulatorBase<ElympicsUnityPhysicsSimulator>
    {
        private PhysicsScene _physicsScene;

        private protected override void InitializeScene()
        {
            _physicsScene = gameObject.scene.GetPhysicsScene();
            Physics.simulationMode = SimulationMode.Script;
        }

        private protected override void Simulate(float deltaTime) => _physicsScene.Simulate(deltaTime);
    }
}
