using System;

namespace Elympics
{
    [Serializable]
    public class ElympicsVarConfig
    {
        public bool synchronizationEnabled;
        public float tolerance;

        public ElympicsVarConfig(bool synchronizationEnabled = true, float tolerance = 0.01f)
        {
            this.synchronizationEnabled = synchronizationEnabled;
            this.tolerance = tolerance;
        }
    }
}
