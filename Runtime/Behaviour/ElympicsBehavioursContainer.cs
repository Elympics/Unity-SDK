using System.Collections.Generic;

namespace Elympics
{
	internal class ElympicsBehavioursContainer
	{
		private readonly ElympicsPlayer _player;

		private readonly SortedDictionary<int, ElympicsBehaviour> _elympicsBehaviours              = new SortedDictionary<int, ElympicsBehaviour>();
		private readonly SortedDictionary<int, ElympicsBehaviour> _elympicsBehavioursWithInput     = new SortedDictionary<int, ElympicsBehaviour>();
		private readonly SortedDictionary<int, ElympicsBehaviour> _elympicsBehavioursPredictable   = new SortedDictionary<int, ElympicsBehaviour>();
		private readonly SortedDictionary<int, ElympicsBehaviour> _elympicsBehavioursUnpredictable = new SortedDictionary<int, ElympicsBehaviour>();

		public IReadOnlyDictionary<int, ElympicsBehaviour> Behaviours              => _elympicsBehaviours;
		public IReadOnlyDictionary<int, ElympicsBehaviour> BehavioursWithInput     => _elympicsBehavioursWithInput;
		public IReadOnlyDictionary<int, ElympicsBehaviour> BehavioursPredictable   => _elympicsBehavioursPredictable;
		public IReadOnlyDictionary<int, ElympicsBehaviour> BehavioursUnpredictable => _elympicsBehavioursUnpredictable;

		public ElympicsBehavioursContainer(ElympicsPlayer player)
		{
			_player = player;
		}

		public bool Contains(int networkId) => _elympicsBehaviours.ContainsKey(networkId);

		public void Add(ElympicsBehaviour elympicsBehaviour)
		{
			if (!elympicsBehaviour.IsVisibleTo(_player))
				return;

			var networkId = elympicsBehaviour.NetworkId;
			_elympicsBehaviours.Add(networkId, elympicsBehaviour);

			if (elympicsBehaviour.HasAnyInput)
				_elympicsBehavioursWithInput.Add(networkId, elympicsBehaviour);

			if (elympicsBehaviour.IsPredictableTo(_player))
				_elympicsBehavioursPredictable.Add(networkId, elympicsBehaviour);
			else
				_elympicsBehavioursUnpredictable.Add(networkId, elympicsBehaviour);
		}

		public void Remove(int networkId)
		{
			_elympicsBehaviours.Remove(networkId);
			_elympicsBehavioursWithInput.Remove(networkId);
			_elympicsBehavioursPredictable.Remove(networkId);
			_elympicsBehavioursUnpredictable.Remove(networkId);
		}
	}
}
