using System;
using Elympics.Core.Logger;
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
                        ElympicsLogger.WithUnityContext(allComponents[componentIndex])
                            .LogError($"More than one {nameof(IInputHandler)} component found on "
                                + $"{elympicsBehaviour.gameObject.name}! Ignoring component no. {componentIndex} of type "
                                + $"{inputHandler.GetType().Name}");
                    }
                }
            }
            ClientHandlersGuid = elympicsBehaviour.GetComponents<IClientHandlerGuid>();
            ServerHandlersGuid = elympicsBehaviour.GetComponents<IServerHandlerGuid>();
            BotHandlersGuid = elympicsBehaviour.GetComponents<IBotHandlerGuid>();
            Renderers = elympicsBehaviour.GetComponents<IRenderer>();
        }
    }
}
