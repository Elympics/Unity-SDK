using UnityEngine;

namespace Elympics
{
    [DisallowMultipleComponent]
    public class ElympicsUnityPhysicsSimulator : ElympicsMonoBehaviour, IInitializable, IUpdatable
    {
        private static ElympicsUnityPhysicsSimulator instance;

        private PhysicsScene? _currentPhysicsScene;
        private PhysicsScene2D? _currentPhysicsScene2D;
        private bool _isActive;

        private void OnEnable() => _isActive = true;
        private void OnDisable() => _isActive = false;

        public void Initialize()
        {
            if (instance)
            {
                ElympicsLogger.LogError("You can't use more than 1 instance of "
                    + $"{nameof(ElympicsUnityPhysicsSimulator)} in a single scene!\n"
                    + $"Previously detected on object: {instance.gameObject.name}, "
                    + $"current object: {gameObject.name}", gameObject);
                return;
            }
            instance = this;

            var currentScene = gameObject.scene;
            _currentPhysicsScene = currentScene.GetPhysicsScene();
            _currentPhysicsScene2D = currentScene.GetPhysicsScene2D();

            Physics.autoSimulation = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        public void ElympicsUpdate()
        {
            if (!_isActive)
                return;
            if (_currentPhysicsScene == null || _currentPhysicsScene2D == null)
            {
                ElympicsLogger.LogError($"{nameof(ElympicsUnityPhysicsSimulator)} not initialized!", gameObject);
                return;
            }

            _currentPhysicsScene?.Simulate(Elympics.TickDuration);
            _ = _currentPhysicsScene2D?.Simulate(Elympics.TickDuration);
        }
    }
}
