using Elympics.ElympicsSystems;
using UnityEngine;

namespace Elympics
{
    /// <summary>
    /// Steps the scene's 2D physics once per Elympics tick. Add it next to <see cref="ElympicsUnityPhysicsSimulator"/>
    /// (the Physics object of the Elympics System) in scenes that use 2D physics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ElympicsUnityPhysics2DSimulator : ElympicsPhysicsSimulatorBase<ElympicsUnityPhysics2DSimulator>, IReplayPhysicsStep
    {
        private PhysicsScene2D _physicsScene;
        private SimulationMode2D? _previousSimulationMode;

        private protected override void InitializeScene()
        {
            _physicsScene = gameObject.scene.GetPhysicsScene2D();
            _previousSimulationMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        private protected override void Simulate(float deltaTime) => _ = _physicsScene.Simulate(deltaTime);

        private protected override void OnInstanceDestroyed()
        {
            if (_previousSimulationMode is { } previousMode)
                Physics2D.simulationMode = previousMode;
        }

        void IReplayPhysicsStep.StepForReplay()
        {
            if (IsInitialized)
                Simulate(float.Epsilon);
        }
    }
}
