using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Plugins.Elympics.Runtime.Util;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public class ElympicsSnapshot : ElympicsDataWithTick, IFromServer
    {
        internal const int DateTimeWeight = 8;

        [IgnoreMember] public sealed override long Tick { get; set; }
        [Key(1)] public DateTime TickStartUtc { get; set; }
        [Key(2)] public FactoryState Factory { get; set; }
        [Key(3)] public List<KeyValuePair<int, byte[]>> Data { get; set; }
        /// <summary>Key is PlayerId as int.</summary>
        [Key(4)] public Dictionary<int, TickToPlayerInput> TickToPlayersInputData { get; set; }

        public ElympicsSnapshot()
        { }

        protected ElympicsSnapshot(ElympicsSnapshot snapshot)
        {
            Tick = snapshot.Tick;
            TickStartUtc = snapshot.TickStartUtc;
            Factory = snapshot.Factory;
            Data = snapshot.Data;
            TickToPlayersInputData = snapshot.TickToPlayersInputData;
        }

        /// <summary>Turns this instance into non-recursive deep copy of <paramref name="other"/>.</summary>
        internal void DeepCopyFrom(ElympicsSnapshot other)
        {
            Tick = other.Tick;
            TickStartUtc = other.TickStartUtc;
            Factory = new() { Parts = other.Factory?.Parts == null ? null : new(other.Factory.Parts) };
            Data = other.Data?.ToList();
        }

        /// <summary>Add data for objects not contained in this snapshot that is present in <paramref name="source"/> to this snapshot.</summary>
        /// <remarks>
        /// This operation assumes that <see cref="Data"/> is sorted by network ID in both this object and in <paramref name="source"/>.
        /// After this operation is performed <see cref="Data"/> in this object is no longer sorted.
        /// </remarks>
        internal void FillMissingFrom(ElympicsSnapshot source)
        {
            Data.EnsureCapacity(source.Data.Count);

            var minIndex = 0; //Both lists are ordered, so no need to go back to items that are already checked
            var originalCount = Data.Count; //We will add missing items to this list, but there is no need to take them into the account
            foreach (var (sourceNetworkId, sourceData) in source.Data)
            {
                var isMissing = true;
                for (var i = minIndex; i < originalCount; i++)
                {
                    var originNetworkId = Data[i].Key;
                    if (originNetworkId == sourceNetworkId)
                    {
                        isMissing = false;
                        minIndex = i + 1;
                        break;
                    }
                    else if (originNetworkId > sourceNetworkId)
                    {
                        minIndex = i;
                        break;
                    }
                }

                if (isMissing)
                    Data.Add(new(sourceNetworkId, sourceData));
            }
        }

        internal void MergeWithSnapshot(ElympicsSnapshot receivedSnapshot)
        {
            if (receivedSnapshot == null)
                return;

            Tick = receivedSnapshot.Tick;
            TickStartUtc = receivedSnapshot.TickStartUtc;

            if (receivedSnapshot.Factory != null)
            {
                if (Data != null)
                {
                    foreach (var (playerId, factoryPartState) in Factory.Parts)
                    {
                        if (!receivedSnapshot.Factory.Parts.TryGetValue(playerId, out var receivedFactoryPartState))
                            continue;

                        foreach (var instanceId in factoryPartState.dynamicInstancesState.instances.Keys)
                        {
                            if (!receivedFactoryPartState.dynamicInstancesState.instances.ContainsKey(instanceId))
                            {
                                var index = Data.FindIndex(kvp => kvp.Key == instanceId);
                                if (index >= 0)
                                    Data.RemoveAt(index);
                            }
                        }
                    }
                }

                Factory = new() { Parts = receivedSnapshot.Factory.Parts == null ? null : new(receivedSnapshot.Factory.Parts) };
            }

            if (receivedSnapshot.TickToPlayersInputData != null)
                TickToPlayersInputData = receivedSnapshot.TickToPlayersInputData;

            if (receivedSnapshot.Data != null)
            {
                if (Data == null)
                {
                    Data = receivedSnapshot.Data;
                    return;
                }

                var localIndex = 0;
                var remoteIndex = 0;
                while (remoteIndex < receivedSnapshot.Data.Count && localIndex < Data.Count)
                {
                    var receievedNetworkId = receivedSnapshot.Data[remoteIndex].Key;
                    var localNetworkId = Data[localIndex].Key;
                    if (localNetworkId == receievedNetworkId)
                    {
                        Data[localIndex] = receivedSnapshot.Data[remoteIndex];
                        remoteIndex++;
                        continue;
                    }
                    if (localNetworkId > receievedNetworkId)
                    {
                        Data.Insert(localIndex, receivedSnapshot.Data[remoteIndex]);
                        remoteIndex++;
                        continue;
                    }
                    localIndex++;
                    if (localIndex >= Data.Count)
                    {
                        Data.Add(receivedSnapshot.Data[remoteIndex]);
                        remoteIndex++;
                    }
                }
            }
        }
    }
}
