using System.Collections.Generic;

namespace Elympics
{
    internal class ElympicsBehavioursContainer
    {
        private readonly ElympicsPlayer _player;

        private readonly SortedDictionary<int, ElympicsBehaviour> _elympicsBehaviours = new();
        private readonly SortedDictionary<int, ElympicsBehaviour> _elympicsBehavioursWithInput = new();
        private readonly SortedDictionary<int, ElympicsBehaviour> _elympicsBehavioursPredictable = new();
        private readonly SortedDictionary<int, ElympicsBehaviour> _elympicsBehavioursUnpredictable = new();

        /// <summary>Dictionary where values are ElympicBehaviours and keys are their network IDs.</summary>
        /// <remarks>This dictionary is sorted by network ID in ascending order.</remarks>
        public IReadOnlyDictionary<int, ElympicsBehaviour> Behaviours => _elympicsBehaviours;
        /// <summary>Dictionary where values are ElympicBehaviours for which <see cref="ElympicsBehaviour.HasAnyInput"/> is true and keys are their network IDs.</summary>
        /// <remarks>This dictionary is sorted by network ID in ascending order.</remarks>
        public IReadOnlyDictionary<int, ElympicsBehaviour> BehavioursWithInput => _elympicsBehavioursWithInput;
        /// <summary>Dictionary where values are ElympicBehaviours that are predictable for <see cref="_player"/> and keys are their network IDs.</summary>
        /// <remarks>This dictionary is sorted by network ID in ascending order.</remarks>
        public IReadOnlyDictionary<int, ElympicsBehaviour> BehavioursPredictable => _elympicsBehavioursPredictable;
        /// <summary>Dictionary where values are ElympicBehaviours that are not predictable for <see cref="_player"/> and keys are their network IDs.</summary>
        /// <remarks>This dictionary is sorted by network ID in ascending order.</remarks>
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
            _ = _elympicsBehaviours.Remove(networkId);
            _ = _elympicsBehavioursWithInput.Remove(networkId);
            _ = _elympicsBehavioursPredictable.Remove(networkId);
            _ = _elympicsBehavioursUnpredictable.Remove(networkId);
        }
    }
}
