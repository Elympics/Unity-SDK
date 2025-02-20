using UnityEngine;

namespace Elympics
{
    [DisallowMultipleComponent]
    public class ElympicsUnityPhysicsSimulator : ElympicsMonoBehaviour, IInitializable, IUpdatable, IStateSerializationHandler
    {
        private static ElympicsUnityPhysicsSimulator instance;

        private PhysicsScene? _currentPhysicsScene;
        private PhysicsScene2D? _currentPhysicsScene2D;
        private bool _isActive;

        private void OnEnable() => _isActive = true;
        private void OnDisable() => _isActive = false;

        public void Initialize()
        {
            if (instance != null && instance != this)
            {
                ElympicsLogger.LogError("You can't use more than 1 instance of "
                    + $"{nameof(ElympicsUnityPhysicsSimulator)} in a single scene!\n"
                    + $"Previously detected on object: {instance.gameObject.name}, "
                    + $"current object: {gameObject.name}",
                    gameObject);
                return;
            }
            instance = this;

            var currentScene = gameObject.scene;
            _currentPhysicsScene = currentScene.GetPhysicsScene();
            _currentPhysicsScene2D = currentScene.GetPhysicsScene2D();

            Physics.autoSimulation = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        public void ElympicsUpdate() => SimulatePhysics(Elympics.TickDuration);
        public void OnPostStateDeserialize()
        {
            //This is special case when Prediction is turned off for entire game.
            if (ElympicsBase.Config.Prediction)
                return;

            //We must to force physics simulation because Unity GameObject Transform does not refresh automatically when ElympicsRigidBody components are updated.
            SimulatePhysics(float.Epsilon);
        }
        private void SimulatePhysics(float deltaTime)
        {
            if (!_isActive)
                return;
            if (_currentPhysicsScene == null || _currentPhysicsScene2D == null)
            {
                ElympicsLogger.LogError($"{nameof(ElympicsUnityPhysicsSimulator)} not initialized!", gameObject);
                return;
            }
            _currentPhysicsScene?.Simulate(deltaTime);
            _ = _currentPhysicsScene2D?.Simulate(deltaTime);
        }
        public void OnPreStateSerialize() { }
    }
}
