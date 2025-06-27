using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

namespace Elympics
{
    [RequireComponent(typeof(ElympicsBehaviour))]
    public class ElympicsMonoBehaviour : MonoBehaviour, IObservable
    {
        private ElympicsBehaviour _elympicsBehaviour;
        private ElympicsBase _elympics;
        private ElympicsFactory _factory;

        public ElympicsBehaviour ElympicsBehaviour
        {
            get
            {
                if (_elympicsBehaviour is not null)
                    return _elympicsBehaviour;

                if (TryGetComponent(out _elympicsBehaviour))
                    return _elympicsBehaviour;

                var missingComponentMessage = $"{GetType().Name} has no {nameof(ElympicsBehaviour)} component.";
                ElympicsLogger.LogError(missingComponentMessage, this);
                throw new MissingComponentException(missingComponentMessage);
            }
        }

        internal ElympicsBase ElympicsBase
        {
            get
            {
                if (_elympics is not null)
                    return _elympics;

                _elympics = ElympicsBehaviour.ElympicsBase;

                if (_elympics != null)
                    return _elympics;

                var elympicsBaseNullReferenceMessage = $"{nameof(ElympicsBehaviour)} in {gameObject.name} object "
                    + "has not been initialized yet. Check Script Execution Order!";
                ElympicsLogger.LogError(elympicsBaseNullReferenceMessage, this);
                throw new UnassignedReferenceException(elympicsBaseNullReferenceMessage);
            }
        }

        public bool IsPredictableForMe => ElympicsBehaviour.IsPredictableTo(Elympics.Player);
        public ElympicsPlayer PredictableFor => ElympicsBehaviour.PredictableFor;

        /// <summary>Checks if Behaviour is enabled and its Game Object is active in hierarchy.</summary>
        /// <remarks>
        /// Works before OnEnabled is called and should be used instead of <see cref="Behaviour.isActiveAndEnabled"/>
        /// for example in <see cref="IInitializable.Initialize"/>.
        /// </remarks>
        public bool IsEnabledAndActive => enabled && gameObject.activeInHierarchy;

        /// <summary>
        /// Provides Elympics-specific game instance data and methods.
        /// </summary>
        /// <exception cref="MissingComponentException">ElympicsBehaviour component is not attached to the object.</exception>
        public IElympics Elympics => ElympicsBase;

        /// <summary>
        /// Synchronize a prefab instantiation and process all its ElympicsBehaviour components.
        /// </summary>
        /// <param name="pathInResources">Path to instantiated prefab which must reside in Resources.</param>
        /// <param name="player">Instantiated object should be predictable for this player.</param>
        /// <returns>Created game object.</returns>
        /// <remarks>For object destruction see <see cref="ElympicsDestroy"/>.</remarks>
        public GameObject ElympicsInstantiate(string pathInResources, ElympicsPlayer player)
        {
            ThrowIfCalledInWrongContextWithPlayer(player);
            return GetFactory().CreateInstance(pathInResources, player);
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private void ThrowIfCalledInWrongContextWithPlayer(ElympicsPlayer player, [CallerMemberName] string caller = "")
        {
            if (player == ElympicsPlayer.World && !Elympics.IsServer)
                throw new ElympicsException($"You cannot use {caller} with {player} option as a client or bot");
            if (player != ElympicsPlayer.All && !Elympics.IsServer && Elympics.Player != player)
                throw new ElympicsException($"You cannot use {caller} with {player} option as a client {Elympics.Player}");
            ThrowIfCalledInWrongContext(caller);
        }

        /// <summary>
        /// Synchronize a game object destruction.
        /// </summary>
        /// <param name="createdGameObject">Destroyed game object.</param>
        /// <remarks>Only objects instantiated with <see cref="ElympicsInstantiate"/> may be destroyed with this method.</remarks>
        public void ElympicsDestroy(GameObject createdGameObject)
        {
            ThrowIfCalledInWrongContext();
            GetFactory().DestroyInstance(createdGameObject);
        }

        private void ThrowIfCalledInWrongContext([CallerMemberName] string caller = "")
        {
            if (ElympicsBase.CurrentCallContext is not ElympicsBase.CallContext.ElympicsUpdate
                and not ElympicsBase.CallContext.Initialize)
                throw new ElympicsException($"You cannot use {caller} outside of {nameof(IUpdatable.ElympicsUpdate)} "
                    + $"or {nameof(IInitializable.Initialize)}");
        }

        private ElympicsFactory GetFactory() => _factory ??= FindObjectOfType<ElympicsFactory>();

        [UsedImplicitly] // from generated IL code
        protected internal MethodInfo GetMethodInfo(string methodName) =>
            GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        [UsedImplicitly] // from generated IL code
        protected internal ElympicsRpcProperties GetRpcProperties(MethodInfo methodInfo) =>
            methodInfo!.GetCustomAttributes<ElympicsRpcAttribute>().First().Properties;
    }
}
