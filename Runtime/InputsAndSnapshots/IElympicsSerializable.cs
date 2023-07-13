using System.IO;

namespace Elympics
{
    internal interface IElympicsSerializable
    {
        void Serialize(BinaryWriter bw);
        void Deserialize(BinaryReader br);
    }
}
