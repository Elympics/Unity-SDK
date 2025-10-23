using System;
using System.IO;

namespace Elympics
{
    [Serializable]
    public sealed class ElympicsInt : ElympicsVar<int>
    {
        //Parameterless constructor for Unity serialization
        public ElympicsInt() : this(default, true)
        {
        }

        public ElympicsInt(int value = default, bool enableSynchronization = true, ElympicsIntEqualityComparer comparer = null)
            : base(value, enableSynchronization, comparer ?? new ElympicsIntEqualityComparer())
        {
        }

        public ElympicsInt(int value, ElympicsVarConfig config)
            : base(value, config.synchronizationEnabled, new ElympicsIntEqualityComparer(config.tolerance))
        {
        }

        public override void Serialize(BinaryWriter bw) => bw.Write(Value);
        protected override int DeserializeInternal(BinaryReader br) => br.ReadInt32();
    }
}
