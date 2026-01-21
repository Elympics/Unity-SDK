#nullable enable

using System;
using System.Collections.Generic;
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
        [Key(3)] public Dictionary<int, byte[]>? Data { get; set; }
        /// <summary>Key is PlayerId as int.</summary>
        [Key(4)] public Dictionary<int, TickToPlayerInput>? TickToPlayersInputData { get; set; }

        public ElympicsSnapshot(FactoryState factory, Dictionary<int, byte[]>? data) : this(-1, default, factory, data, null) { }

        protected ElympicsSnapshot(ElympicsSnapshot snapshot) : this(snapshot.Tick, snapshot.TickStartUtc, snapshot.Factory, snapshot.Data, snapshot.TickToPlayersInputData) { }

        [SerializationConstructor]
        public ElympicsSnapshot(long tick, DateTime tickStartUtc, FactoryState factory, Dictionary<int, byte[]>? data, Dictionary<int, TickToPlayerInput>? tickToPlayersInputData)
        {
            Tick = tick;
            TickStartUtc = tickStartUtc;
            Factory = factory;
            Data = data;
            TickToPlayersInputData = tickToPlayersInputData;
        }

        public static ElympicsSnapshot CreateEmpty() => new(new FactoryState(new Dictionary<int, FactoryPartState>()), null);

        /// <summary>Turns this instance into non-recursive deep copy of <paramref name="other"/>.</summary>
        internal static ElympicsSnapshot CreateDeepCopy(ElympicsSnapshot other) => new(other.Tick, other.TickStartUtc, new(new(other.Factory.Parts)), new(other.Data), new(other.TickToPlayersInputData));

        /// <summary>Add data for objects not contained in this snapshot that is present in <paramref name="source"/> to this snapshot.</summary>
        /// <remarks>
        /// This operation assumes that <see cref="Data"/> is sorted by network ID in both this object and in <paramref name="source"/>.
        /// After this operation is performed <see cref="Data"/> in this object is no longer sorted.
        /// </remarks>
        internal void FillMissingFrom(ElympicsSnapshot source)
        {
            if (source.Data == null)
                return;
            if (Data == null)
            {
                Data = new(source.Data);
                return;
            }

            _ = Data.EnsureCapacity(source.Data.Count);

            foreach (var (id, state) in source.Data)
            {
                if (!Data.ContainsKey(id))
                    Data.Add(id, state);
            }
        }

        internal void MergeWithSnapshot(ElympicsSnapshot? receivedSnapshot)
        {
            if (receivedSnapshot == null)
                return;

            Tick = receivedSnapshot.Tick;
            TickStartUtc = receivedSnapshot.TickStartUtc;

            if (Data != null)
            {
                foreach (var (playerId, factoryPartState) in Factory.Parts)
                {
                    if (!receivedSnapshot.Factory.Parts.TryGetValue(playerId, out var receivedFactoryPartState))
                        continue;

                    foreach (var instanceId in factoryPartState.DynamicInstancesState.Instances.Keys)
                    {
                        if (!receivedFactoryPartState.DynamicInstancesState.Instances.ContainsKey(instanceId))
                            _ = Data.Remove(instanceId);
                    }
                }
            }

            Factory = new(new(receivedSnapshot.Factory.Parts));

            if (receivedSnapshot.TickToPlayersInputData != null)
                TickToPlayersInputData = receivedSnapshot.TickToPlayersInputData;

            if (receivedSnapshot.Data != null)
            {
                if (Data == null)
                {
                    Data = receivedSnapshot.Data;
                }
                else
                {
                    foreach (var (id, state) in receivedSnapshot.Data)
                        Data[id] = state;
                }
            }
        }
    }
}
