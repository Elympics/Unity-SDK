using System.Collections;
using UnityEngine;

namespace Elympics
{
	internal class ElympicsComponentsContainer
	{
		private IEnumerable                  _botHandlers;
		public  IObservable[]                Observables            { get; }
		public  IStateSerializationHandler[] SerializationHandlers  { get; }
		public  IInitializable[]             Initializables         { get; }
		public  IUpdatable[]                 Updatables             { get; }
		public  IReconciliationHandler[]     ReconciliationHandlers { get; }
		public  IInputHandler                InputHandler           { get; } = null;
		public  IClientHandler[]             ClientHandlers         { get; }
		public  IServerHandler[]             ServerHandlers         { get; }
		public  IBotHandler[]                BotHandlers            { get; }

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
				for (var i = 1; i < inputHandlers.Length; i++)
					Debug.LogError($"More than one {nameof(IInputHandler)} component on {elympicsBehaviour.gameObject.name} {elympicsBehaviour.GetType().Name}! Ignoring {inputHandlers.GetType().Name}", elympicsBehaviour);
			}
			ClientHandlers = elympicsBehaviour.GetComponents<IClientHandler>();
			ServerHandlers = elympicsBehaviour.GetComponents<IServerHandler>();
			BotHandlers = elympicsBehaviour.GetComponents<IBotHandler>();
		}
	}
}
