using System;
using System.IO;
using UnityEngine;

namespace Elympics
{
    [Serializable]
    public sealed class ElympicsVector3 : ElympicsVar<Vector3>
    {
        //Parameterless constructor for Unity serialization
        public ElympicsVector3() : this(default, true)
        {
        }

        public ElympicsVector3(Vector3 value = default, bool enableSynchronization = true, ElympicsVector3EqualityComparer comparer = null)
            : base(value, enableSynchronization, comparer ?? new ElympicsVector3EqualityComparer())
        {
        }

        public ElympicsVector3(Vector3 value, ElympicsVarConfig config)
            : base(value, config.synchronizationEnabled, new ElympicsVector3EqualityComparer(config.tolerance))
        {
        }

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(Value.x);
            bw.Write(Value.y);
            bw.Write(Value.z);
        }

        protected override Vector3 DeserializeInternal(BinaryReader br) => new(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        public override string ToString() => Value.ToString("G");
    }
}
