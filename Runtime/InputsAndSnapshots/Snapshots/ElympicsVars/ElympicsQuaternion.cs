using System;
using System.IO;
using UnityEngine;

namespace Elympics
{
    [Serializable]
    public sealed class ElympicsQuaternion : ElympicsVar<Quaternion>
    {
        //Parameterless constructor for Unity serialization
        public ElympicsQuaternion() : this(default, true)
        {
        }

        public ElympicsQuaternion(Quaternion value = default, bool enableSynchronization = true, ElympicsQuaternionEqualityComparer comparer = null)
            : base(value, enableSynchronization, comparer ?? new ElympicsQuaternionEqualityComparer())
        {
        }

        public ElympicsQuaternion(Quaternion value, ElympicsVarConfig config)
            : base(value, config.synchronizationEnabled, new ElympicsQuaternionEqualityComparer(config.tolerance))
        {
        }

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(Value.x);
            bw.Write(Value.y);
            bw.Write(Value.z);
            bw.Write(Value.w);
        }

        protected override Quaternion DeserializeInternal(BinaryReader br) =>
            new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        public override string ToString() => Value.ToString("G");
    }
}
