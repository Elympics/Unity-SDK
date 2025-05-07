using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Plugins.Elympics.Runtime.Util;
using MessagePack;
using PlayerId = System.Int32;

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
        [Key(4)] public Dictionary<PlayerId, TickToPlayerInput> TickToPlayersInputData { get; set; }

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
            Factory = new() { Parts = other.Factory?.Parts?.ToList() };
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
    }
}
