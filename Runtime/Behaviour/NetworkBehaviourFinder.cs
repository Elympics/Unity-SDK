using System;
using System.Collections.Generic;
using System.Linq;

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
            private readonly IReadOnlyDictionary<int, byte[]> _first;
            private readonly IReadOnlyDictionary<int, byte[]> _second;
            private readonly IEnumerator<int> _keys;

            private static readonly Dictionary<int, byte[]> EmptyDictionary = new(0);

            public Enumerator(ElympicsSnapshot first, ElympicsSnapshot second)
            {
                _first = first.Data ?? EmptyDictionary;
                _second = second.Data ?? EmptyDictionary;
                _keys = _first.Keys.Intersect(_second.Keys).GetEnumerator();
            }

            public bool MoveNext() => _keys.MoveNext();

            public BehaviourPair Current => new()
            {
                NetworkId = _keys.Current,
                DataFromFirst = _first[_keys.Current],
                DataFromSecond = _second[_keys.Current],
            };
        }

        public readonly struct BehaviourPair
        {
            public int NetworkId { get; init; }
            public byte[] DataFromFirst { get; init; }
            public byte[] DataFromSecond { get; init; }
        }
    }
}
