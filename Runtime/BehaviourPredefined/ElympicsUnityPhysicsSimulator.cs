using UnityEngine;

namespace Elympics
{
    public class ElympicsUnityPhysicsSimulator : ElympicsMonoBehaviour, IInitializable, IUpdatable
    {
        private PhysicsScene? _currentPhysicsScene;
        private PhysicsScene2D? _currentPhysicsScene2D;

        private bool isActive = false;

        private void OnEnable()
        {
            isActive = true;
        }

        private void OnDisable()
        {
            isActive = false;
        }

        public void Initialize()
        {
            if (gameObject.FindObjectsOfTypeOnScene<ElympicsUnityPhysicsSimulator>().Count > 1)
            {
                Debug.LogError($"You can't use more than 1 {nameof(ElympicsUnityPhysicsSimulator)}!", gameObject);
                return;
            }

            var currentScene = gameObject.scene;
            _currentPhysicsScene = currentScene.GetPhysicsScene();
            _currentPhysicsScene2D = currentScene.GetPhysicsScene2D();

            Physics.autoSimulation = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        public void ElympicsUpdate()
        {
            if (!isActive)
                return;
            if (_currentPhysicsScene == null || _currentPhysicsScene2D == null)
            {
                Debug.LogError($"{nameof(ElympicsUnityPhysicsSimulator)} not initialized!", gameObject);
                return;
            }

            _currentPhysicsScene?.Simulate(Elympics.TickDuration);
            _ = (_currentPhysicsScene2D?.Simulate(Elympics.TickDuration));
        }
    }
}
