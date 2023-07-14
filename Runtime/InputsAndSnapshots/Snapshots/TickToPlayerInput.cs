using System.Collections.Generic;
using System.IO;
using tick = System.Int64;
namespace Elympics
{
    public class TickToPlayerInput : IElympicsSerializable
    {
        public Dictionary<tick, ElympicsSnapshotPlayerInput> Data;

        void IElympicsSerializable.Serialize(BinaryWriter bw)
        {
            bw.Write(Data.Count);
            foreach (var (playerId, playerInput) in Data)
            {
                bw.Write(playerId);
                playerInput.Serialize(bw);
            }
        }

        void IElympicsSerializable.Deserialize(BinaryReader br)
        {
            Data = new Dictionary<long, ElympicsSnapshotPlayerInput>();
            var count = br.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var tick = br.ReadInt64();
                var snapshotPlayerInput = br.Deserialize<ElympicsSnapshotPlayerInput>();
                Data.Add(tick, snapshotPlayerInput);
            }
        }
    }
}

