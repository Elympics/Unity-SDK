#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Core.Utils;
using Elympics.ElympicsSystems.Internal;
using UnityEngine;

namespace Elympics
{
    public partial class ElympicsClient
    {
        private class ElympicsBehaviourFirstSnapshotTracker
        {
            /// <summary>
            /// Every <see cref="ElympicsBehaviour.networkId"/> that was already present in a snapshot received from the server stored with its network ID as key.
            /// Weak reference is used to check if same network ID was not reassigned to a different object.
            /// </summary>
            private readonly Dictionary<int, WeakReference<ElympicsBehaviour>> _knownBehaviours = new();
            private readonly List<ElympicsBehaviour> _newBehaviours = new();
            private readonly ElympicsBehavioursManager _manager;

            public ElympicsBehaviourFirstSnapshotTracker(ElympicsBehavioursManager manager) => _manager = manager;

            public void ProcessNewSnapshot(ElympicsSnapshot snapshot, ElympicsLoggerContext logger)
            {
                Debug.Log($"[InitializedByServerTest] Received snapshot with IDs: [{snapshot.Data.Select(x => x.Key).CommaList()}]");

                foreach (var (networkId, _) in snapshot.Data)
                {
                    //Even if creation of this behaviour was not predicted by this client it should have been created by the factory synchronization process by now,
                    //so the only case where it doesn't exist is if it was destroyed locally, in which case we should skip it, since it was either already initialized
                    //or didn't exist long enough to be initialized (spawned during a match and destroyed before the first snapshot containing it was received).
                    if (!_manager.TryGetBehaviour(networkId, out var newBehaviour))
                        continue;

                    //Check if we received this behaviour's state in any previous snapshot while keeping in mind that network IDs can be reused
                    if (_knownBehaviours.TryGetValue(networkId, out var knownBehaviourReference)
                        && knownBehaviourReference.TryGetTarget(out var knownBehaviour)
                        && ReferenceEquals(knownBehaviour, newBehaviour))
                        continue;

                    _knownBehaviours[networkId] = new WeakReference<ElympicsBehaviour>(newBehaviour);
                    _newBehaviours.Add(newBehaviour);
                }
            }

            public void InitializeNewBehaviours()
            {
                if (_newBehaviours.Count <= 0)
                    return;

                try
                {
                    foreach (var behaviour in _newBehaviours.OrderBy(x => x.networkId))
                    {
                        if (behaviour != null)
                            behaviour.InitializedByServer();
                    }
                }
                finally
                {
                    _newBehaviours.Clear();
                }
            }
        }
    }
}
