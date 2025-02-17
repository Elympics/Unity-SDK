using System;
using System.IO;

namespace Elympics
{
    [Serializable]
    public class ElympicsGameObject : ElympicsVar<ElympicsBehaviour>
    {
        private const int NullReferenceNetworkId = -1;

        public ElympicsGameObject(ElympicsBehaviour value = default, bool enableSynchronization = true)
            : base(value, enableSynchronization, new ElympicsGameObjectEqualityComparer())
        { }

        public override void Serialize(BinaryWriter bw) => bw.Write(Value != null ? Value.networkId : NullReferenceNetworkId);

        protected override ElympicsBehaviour DeserializeInternal(BinaryReader br)
        {
            var valueNetworkId = br.ReadInt32();

            if (valueNetworkId == NullReferenceNetworkId)
                return null;

            // todo for elympics debug mode add information that null here means that client has access to ElympicsGameObject not visible to this client and therefore trigger reconciliation ~pprzestrzelski 15.06.2022
            return Elympics.TryGetBehaviour(valueNetworkId, out var elympicsBehaviour) ? elympicsBehaviour : null;
        }
    }
}
