using System;
using System.Collections.Generic;
using System.IO;
using MatchTcpLibrary.Ntp;
using PlayerId = System.Int32;

namespace Elympics
{
	public class ElympicsSnapshot : ElympicsDataWithTick,IElympicsSerializable
	{
		public override long                                              Tick         { get; set; }
		public          DateTime                                          TickStartUtc { get; set; }
		public          FactoryState                                      Factory      { get; set; }
		public          List<KeyValuePair<int, byte[]>>                   Data         { get; set; }
		
		public Dictionary<PlayerId,TickToPlayerInput> TickToPlayersInputData { get; set; }



		void IElympicsSerializable.Deserialize(BinaryReader br)
		{
			Tick = br.ReadInt64();
			Factory = br.Deserialize<FactoryState>();
			Data = br.ReadListWithKvpIntToByteArray();
			TickToPlayersInputData = new Dictionary<int, TickToPlayerInput>();
			var lenght = br.ReadInt32();
			for (int i = 0; i < lenght; i++)
			{
				var tick = br.ReadInt32();
				var playerToInputDataPair = br.Deserialize<TickToPlayerInput>();
				TickToPlayersInputData.Add(tick,playerToInputDataPair);
			}
			var createdAt = br.ReadBytes(8);
			TickStartUtc = NtpUtils.NtpDataTimeStampToDateTime(createdAt);
		}


		void IElympicsSerializable.Serialize(BinaryWriter bw)
		{
			bw.Write(Tick);
			Factory.Serialize(bw);
			bw.Write(Data);
			bw.Write(TickToPlayersInputData.Count);
			foreach (var keyValuePair in TickToPlayersInputData)
			{
				bw.Write(keyValuePair.Key);
				keyValuePair.Value.Serialize(bw);
			}
			var createdAt = new byte[8];
			NtpUtils.DateTimeToNtpDataTimeStamp(TickStartUtc, createdAt);
			bw.Write(createdAt);
		}
	}
}
