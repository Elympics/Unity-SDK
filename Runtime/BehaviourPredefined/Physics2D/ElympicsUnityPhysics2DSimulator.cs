using Elympics.Core.Logger;
using Elympics.ElympicsSystems;
using UnityEngine;

namespace Elympics
{
    /// <summary>
    /// Steps the scene's 2D physics once per Elympics tick. Add it next to <see cref="ElympicsUnityPhysicsSimulator"/>
    /// (the Physics object of the Elympics System) in scenes that use 2D physics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ElympicsUnityPhysics2DSimulator : ElympicsMonoBehaviour, IInitializable, IUpdatable, IReplayPhysicsStep
    {
        private static ElympicsUnityPhysics2DSimulator instance;

        private PhysicsScene2D? _currentPhysicsScene;
        private SimulationMode2D? _previousSimulationMode;
        private bool _isActive;

        private void OnEnable() => _isActive = true;
        private void OnDisable() => _isActive = false;

        private void OnDestroy()
        {
            if (!ReferenceEquals(instance, this))
                return;
            instance = null;
            if (_previousSimulationMode is { } previousMode)
                Physics2D.simulationMode = previousMode;
        }

        public void Initialize()
        {
            if (instance != null && instance != this)
            {
                ElympicsLogger.WithUnityContext(gameObject)
                    .LogError($"You can't use more than 1 instance of {nameof(ElympicsUnityPhysics2DSimulator)} in a single scene!\n"
                        + $"Previously detected on object: {instance.gameObject.name}, current object: {gameObject.name}");
                return;
            }
            instance = this;

            _currentPhysicsScene = gameObject.scene.GetPhysicsScene2D();
            _previousSimulationMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        public void ElympicsUpdate()
        {
            if (!_isActive)
                return;
            if (_currentPhysicsScene is not { } physicsScene)
            {
                ElympicsLogger.WithUnityContext(gameObject)
                    .LogError($"{nameof(ElympicsUnityPhysics2DSimulator)} not initialized!");
                return;
            }
            _ = physicsScene.Simulate(Elympics.TickDuration);
        }

        void IReplayPhysicsStep.StepForReplay() => _ = gameObject.scene.GetPhysicsScene2D().Simulate(float.Epsilon);
    }
}
