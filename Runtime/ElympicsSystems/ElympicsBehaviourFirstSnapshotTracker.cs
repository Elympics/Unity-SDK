using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.ElympicsSystems.Internal;

namespace Elympics
{
    public partial class ElympicsClient
    {
        private class ElympicsBehaviourFirstSnapshotTracker
        {
            /// <summary>
            /// Sorted list with every <see cref="ElympicsBehaviour.networkId"/> that was already present in a snapshot received from the server.
            /// </summary>
            private readonly List<int> _knownIDs = new();
            private readonly List<int> _newIDs = new();
            private readonly ElympicsBehavioursManager _manager;

            public ElympicsBehaviourFirstSnapshotTracker(ElympicsBehavioursManager manager) => _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            public void ProcessNewSnapshot(ElympicsSnapshot snapshot)
            {
                foreach (var (networkId, _) in snapshot.Data)
                {
                    var index = _knownIDs.BinarySearch(networkId);

                    if (index >= 0)
                        continue;

                    _knownIDs.Insert(~index, networkId);
                    _newIDs.Add(networkId);
                }
            }

            public void InitializeNewBehaviours()
            {
                if (_newIDs.Count <= 0)
                    return;

                try
                {
                    foreach (var networkId in _newIDs.OrderBy(x => x))
                    {
                        if (_manager.TryGetBehaviour(networkId, out var behaviour))
                            behaviour.InitializedByServer();
                    }
                }
                finally
                {
                    _newIDs.Clear();
                }
            }
        }
    }
}
