using System;
using System.ComponentModel;
using System.IO;

namespace Elympics
{
    [Serializable]
    [DefaultValue(false)]
    public sealed class ElympicsBool : ElympicsVar<bool>
    {
        //Parameterless constructor for Unity serialization
        public ElympicsBool() : this(default, true)
        {
        }

        public ElympicsBool(bool value = default, bool enabledSynchronization = true) : base(value, enabledSynchronization)
        {
        }

        public ElympicsBool(bool value, ElympicsVarConfig config) : base(value, config.synchronizationEnabled)
        {
            Comparer.Tolerance = config.tolerance;
        }

        public override void Serialize(BinaryWriter bw) => bw.Write(Value);
        protected override bool DeserializeInternal(BinaryReader br) => br.ReadBoolean();
    }
}
