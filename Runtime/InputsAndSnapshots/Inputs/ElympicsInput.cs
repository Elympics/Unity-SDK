using System.Collections.Generic;
using System.IO;

namespace Elympics
{
    public class ElympicsInput : ElympicsDataWithTick, IElympicsSerializable
    {
        public override long Tick { get; set; }
        public ElympicsPlayer Player { get; set; }
        public List<KeyValuePair<int, byte[]>> Data { get; set; }

        public static readonly ElympicsInput Empty = new()
        {
            Data = new List<KeyValuePair<int, byte[]>>()
        };

        void IElympicsSerializable.Serialize(BinaryWriter bw)
        {
            bw.Write(Tick);
            bw.Write((int)Player);
            bw.Write(Data);
        }

        void IElympicsSerializable.Deserialize(BinaryReader br)
        {
            Tick = br.ReadInt64();
            Player = ElympicsPlayer.FromIndex(br.ReadInt32());
            Data = br.ReadListWithKvpIntToByteArray();
        }
    }
}
