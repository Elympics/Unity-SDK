using System;

#nullable enable

namespace Elympics.Behaviour
{
    internal readonly ref struct NetworkBehaviourFinder
    {
        private readonly ElympicsSnapshot _first;
        private readonly ElympicsSnapshot _second;

        public Enumerator GetEnumerator() => new(_first, _second);

        public NetworkBehaviourFinder(ElympicsSnapshot first, ElympicsSnapshot second)
        {
            _first = first ?? throw new ArgumentNullException(nameof(first));
            _second = second ?? throw new ArgumentNullException(nameof(second));
        }

        public ref struct Enumerator
        {
            private readonly ElympicsSnapshot _first;
            private readonly ElympicsSnapshot _second;

            private int _firstIndex;
            private int _secondIndex;

            public Enumerator(ElympicsSnapshot first, ElympicsSnapshot second)
            {
                _firstIndex = -1;
                _secondIndex = -1;
                _first = first;
                _second = second;
            }

            public bool MoveNext()
            {
                ++_firstIndex;
                ++_secondIndex;
                while (_firstIndex < _first.Data.Count && _secondIndex < _second.Data.Count)
                {
                    var (firstNetworkId, _) = _first.Data[_firstIndex];
                    var (secondNetworkId, _) = _second.Data[_secondIndex];

                    // Difference created by unpredictable factory
                    if (firstNetworkId > secondNetworkId)
                    {
                        _secondIndex++;
                        continue;
                    }

                    if (firstNetworkId < secondNetworkId)
                    {
                        _firstIndex++;
                        continue;
                    }
                    break;
                }
                return _firstIndex < _first.Data.Count && _secondIndex < _second.Data.Count;
            }

            public BehaviourPair Current => new()
            {
                NetworkId = _first.Data[_firstIndex].Key,
                IndexFromFirst = _firstIndex,
                IndexFromSecond = _secondIndex,
                DataFromFirst = _first.Data[_firstIndex].Value,
                DataFromSecond = _second.Data[_secondIndex].Value,
            };
        }

        public readonly struct BehaviourPair
        {
            public int NetworkId { get; init; }
            public int IndexFromFirst { get; init; }
            public int IndexFromSecond { get; init; }
            public byte[] DataFromFirst { get; init; }
            public byte[] DataFromSecond { get; init; }
        }
    }
}
