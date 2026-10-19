#nullable enable

using Elympics.Core.Logger;

namespace Elympics
{
    /// <summary>Base of the components that step a scene's physics once per Elympics tick, one instance per scene.</summary>
    /// <typeparam name="TSelf">The simulator type. Each one has its own instance, so a 2D simulator can sit next to the 3D one.</typeparam>
    public abstract class ElympicsPhysicsSimulatorBase<TSelf> : ElympicsMonoBehaviour, IInitializable, IUpdatable
        where TSelf : ElympicsPhysicsSimulatorBase<TSelf>
    {
        private static TSelf? instance;

        private protected bool IsInitialized { get; private set; }

        // The static would otherwise keep this component, and through it the match's ElympicsBase, alive after its scene unloads.
        private void OnDestroy()
        {
            if (!ReferenceEquals(instance, this))
                return;
            instance = null;
            OnInstanceDestroyed();
        }

        public void Initialize()
        {
            if (instance != null && instance != this)
            {
                ElympicsLogger.WithUnityContext(gameObject)
                    .LogError($"You can't use more than 1 instance of {typeof(TSelf).Name} in a single scene!\n"
                        + $"Previously detected on object: {instance.gameObject.name}, current object: {gameObject.name}");
                return;
            }
            instance = (TSelf)this;
            InitializeScene();
            IsInitialized = true;
        }

        public void ElympicsUpdate()
        {
            if (!isActiveAndEnabled)
                return;
            if (!IsInitialized)
            {
                ElympicsLogger.WithUnityContext(gameObject)
                    .LogError($"{typeof(TSelf).Name} not initialized!");
                return;
            }
            Simulate(Elympics.TickDuration);
        }

        /// <summary>Caches the physics scene of this object's scene and takes over its simulation.</summary>
        private protected abstract void InitializeScene();

        private protected abstract void Simulate(float deltaTime);

        private protected virtual void OnInstanceDestroyed()
        { }
    }
}
