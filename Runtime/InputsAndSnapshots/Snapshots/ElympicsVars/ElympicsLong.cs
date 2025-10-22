using System;
using System.IO;

namespace Elympics
{
    [Serializable]
    public sealed class ElympicsLong : ElympicsVar<long>
    {
        public ElympicsLong(long value = default, bool enableSynchronization = true, ElympicsLongEqualityComparer comparer = null)
            : base(value, enableSynchronization, comparer ?? new ElympicsLongEqualityComparer())
        { }

        public ElympicsLong(long value, ElympicsVarConfig config)
            : base(value, config.synchronizationEnabled, new ElympicsLongEqualityComparer(config.tolerance))
        { }

        public override void Serialize(BinaryWriter bw) => bw.Write(Value);
        protected override long DeserializeInternal(BinaryReader br) => br.ReadInt64();
    }
}
