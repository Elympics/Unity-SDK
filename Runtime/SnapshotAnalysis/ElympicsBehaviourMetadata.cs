using System.Collections.Generic;
using MessagePack;

namespace Elympics
{
    [MessagePackObject]
    public struct ElympicsBehaviourMetadata
    {
        [Key(0)] public string Name;
        [Key(1)] public int NetworkId;
        [Key(2)] public ElympicsPlayer PredictableFor;
        [Key(3)] public string PrefabName;
        [IgnoreMember] public List<(string, List<(string, string)>)> StateMetadata;

        public ElympicsBehaviourMetadata WithStateMetadata(List<(string, List<(string, string)>)> stateMetadata)
        {
            return new ElympicsBehaviourMetadata
            {
                Name = Name,
                NetworkId = NetworkId,
                PredictableFor = PredictableFor,
                PrefabName = PrefabName,
                StateMetadata = stateMetadata
            };
        }
    }
}
