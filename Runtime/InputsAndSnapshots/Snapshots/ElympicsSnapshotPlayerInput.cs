using System.Collections.Generic;
using System.IO;
using NetworkId = System.Int32;

namespace Elympics
{
    public class ElympicsSnapshotPlayerInput : IElympicsSerializable
    {
        public List<KeyValuePair<NetworkId, byte[]>> Data { get; set; }
        void IElympicsSerializable.Serialize(BinaryWriter bw)
        {
            bw.Write(Data);
        }

        void IElympicsSerializable.Deserialize(BinaryReader br)
        {
            Data = br.ReadListWithKvpIntToByteArray();
        }
    }
}
