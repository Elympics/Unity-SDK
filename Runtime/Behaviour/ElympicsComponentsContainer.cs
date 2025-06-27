using System;
using UnityEngine;

namespace Elympics
{
    internal class ElympicsComponentsContainer
    {
        public IObservable[] Observables { get; }
        public IStateSerializationHandler[] SerializationHandlers { get; }
        public IInitializable[] Initializables { get; }
        public IUpdatable[] Updatables { get; }
        public IReconciliationHandler[] ReconciliationHandlers { get; }
        public IInputHandler InputHandler { get; }
#pragma warning disable CS0618
        public IClientHandler[] ClientHandlers { get; }
        public IServerHandler[] ServerHandlers { get; }
        public IBotHandler[] BotHandlers { get; }
#pragma warning restore CS0618
        public IClientHandlerGuid[] ClientHandlersGuid { get; }
        public IServerHandlerGuid[] ServerHandlersGuid { get; }
        public IBotHandlerGuid[] BotHandlersGuid { get; }
        public IRenderer[] Renderers { get; }

        public ElympicsComponentsContainer(ElympicsBehaviour elympicsBehaviour)
        {
            Observables = elympicsBehaviour.GetComponents<IObservable>();
            SerializationHandlers = elympicsBehaviour.GetComponents<IStateSerializationHandler>();
            Initializables = elympicsBehaviour.GetComponents<IInitializable>();
            Updatables = elympicsBehaviour.GetComponents<IUpdatable>();
            ReconciliationHandlers = elympicsBehaviour.GetComponents<IReconciliationHandler>();
            var inputHandlers = elympicsBehaviour.GetComponents<IInputHandler>();
            if (inputHandlers.Length > 0)
            {
                InputHandler = inputHandlers[0];
                if (inputHandlers.Length > 1)
                {
                    var allComponents = elympicsBehaviour.GetComponents<Component>();
                    for (var i = 1; i < inputHandlers.Length; i++)
                    {
                        var inputHandler = inputHandlers[i];
                        var componentIndex = Array.IndexOf(allComponents, inputHandler);
                        ElympicsLogger.LogError($"More than one {nameof(IInputHandler)} component found on "
                            + $"{elympicsBehaviour.gameObject.name}! Ignoring component no. {componentIndex} of type "
                            + $"{inputHandler.GetType().Name}", allComponents[componentIndex]);
                    }
                }
            }
#pragma warning disable CS0618
            ClientHandlers = elympicsBehaviour.GetComponents<IClientHandler>();
            ServerHandlers = elympicsBehaviour.GetComponents<IServerHandler>();
            BotHandlers = elympicsBehaviour.GetComponents<IBotHandler>();
#pragma warning restore CS0618
            ClientHandlersGuid = elympicsBehaviour.GetComponents<IClientHandlerGuid>();
            ServerHandlersGuid = elympicsBehaviour.GetComponents<IServerHandlerGuid>();
            BotHandlersGuid = elympicsBehaviour.GetComponents<IBotHandlerGuid>();
            Renderers = elympicsBehaviour.GetComponents<IRenderer>();
        }
    }
}
