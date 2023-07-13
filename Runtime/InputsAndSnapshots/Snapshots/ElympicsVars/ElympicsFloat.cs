using System;
using System.IO;

namespace Elympics
{
    [Serializable]
    public sealed class ElympicsFloat : ElympicsVar<float>
    {
        public ElympicsFloat(float value = 0.0f, bool enableSynchronization = true, ElympicsFloatEqualityComparer comparer = null)
            : base(value, enableSynchronization, comparer ?? new ElympicsFloatEqualityComparer())
        {
        }

        public ElympicsFloat(float value, ElympicsVarConfig config)
            : base(value, config.synchronizationEnabled, new ElympicsFloatEqualityComparer(config.tolerance))
        {
        }

        public override void Serialize(BinaryWriter bw) => bw.Write(Value);
        protected override float DeserializeInternal(BinaryReader br) => br.ReadSingle();
    }
}
