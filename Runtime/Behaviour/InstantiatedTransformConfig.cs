using UnityEngine;

namespace Elympics
{
    internal readonly struct InstantiatedTransformConfig
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public InstantiatedTransformConfig(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
