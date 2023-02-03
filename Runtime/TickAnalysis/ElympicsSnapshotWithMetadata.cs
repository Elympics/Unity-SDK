using System;
using System.Collections.Generic;
using System.IO;
using MatchTcpLibrary.Ntp;

namespace Elympics
{
	public class ElympicsSnapshotWithMetadata : ElympicsSnapshot
	{
		public ElympicsSnapshotWithMetadata()
		{
			Metadata = new List<ElympicsBehaviourMetadata>();
		}

		public ElympicsSnapshotWithMetadata(ElympicsSnapshot snapshot) : base(snapshot)
		{
			Metadata = new List<ElympicsBehaviourMetadata>();
		}

		public ElympicsSnapshotWithMetadata(ElympicsSnapshotWithMetadata snapshot) : base(snapshot)
		{
			Metadata = snapshot.Metadata;
		}

		public DateTime TickEndUtc { get; set; }
		public long FixedUpdateNumber { get; set; }
		public List<ElympicsBehaviourMetadata> Metadata { get; set; }

		public override void Deserialize(BinaryReader br)
		{
			base.Deserialize(br);

			FixedUpdateNumber = br.ReadInt64();
			var size = br.ReadInt32();
			Metadata = new List<ElympicsBehaviourMetadata>(size);
			for (var i = 0; i < size; i++)
			{
				var metadata = new ElympicsBehaviourMetadata();
				var hasPrefabName = br.ReadBoolean();
				if (hasPrefabName)
					metadata.PrefabName = br.ReadString();

				metadata.Name = br.ReadString();
				metadata.NetworkId = br.ReadInt32();
				metadata.PredictableFor = ElympicsPlayer.FromIndexExtended(br.ReadInt32());
				metadata.StateMetadata = br.ReadDictionaryStringToString();

				Metadata.Add(metadata);
			}
			var endedAt = br.ReadBytes(DateTimeWeight);
			TickEndUtc = NtpUtils.NtpDataTimeStampToDateTime(endedAt);
		}

		public override void Serialize(BinaryWriter bw)
		{
			base.Serialize(bw);

			bw.Write(FixedUpdateNumber);
			bw.Write(Metadata.Count);
			foreach (var metadata in Metadata)
			{
				var hasPrefabName = metadata.PrefabName != null;
				bw.Write(hasPrefabName);
				if (hasPrefabName)
					bw.Write(metadata.PrefabName);

				bw.Write(metadata.Name);
				bw.Write(metadata.NetworkId);
				bw.Write(metadata.PredictableFor.playerIndex);
				bw.Write(metadata.StateMetadata);
			}
			var endedAt = new byte[DateTimeWeight];
			NtpUtils.DateTimeToNtpDataTimeStamp(TickEndUtc, endedAt);
			bw.Write(endedAt);
		}
	}
}
