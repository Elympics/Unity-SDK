using System.Collections;

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
		public  IInputHandler[]              InputHandlers          { get; }
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
			InputHandlers = elympicsBehaviour.GetComponents<IInputHandler>();
			ClientHandlers = elympicsBehaviour.GetComponents<IClientHandler>();
			ServerHandlers = elympicsBehaviour.GetComponents<IServerHandler>();
			BotHandlers = elympicsBehaviour.GetComponents<IBotHandler>();
		}
	}
}
