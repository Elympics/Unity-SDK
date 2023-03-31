using System;
using MessagePack;

namespace Elympics.Models.Matchmaking.WebSocket
{
	[MessagePackObject]
	public readonly struct GameData : IToLobby
	{
		[Key(0)] public string SdkVersion { get; }
		[Key(1)] public Guid GameId { get; }
		[Key(2)] public string GameVersion { get; }

		public GameData(string sdkVersion, Guid gameId, string gameVersion)
		{
			SdkVersion = sdkVersion;
			GameId = gameId;
			GameVersion = gameVersion;
		}

		public GameData(string sdkVersion, JoinMatchmakerData joinMatchmakerData)
		{
			SdkVersion = sdkVersion;
			GameId = joinMatchmakerData.GameId;
			GameVersion = joinMatchmakerData.GameVersion;
		}

		public GameData(string sdkVersion, RejoinMatchData rejoinMatchData)
		{
			SdkVersion = sdkVersion;
			GameId = rejoinMatchData.GameId;
			GameVersion = rejoinMatchData.GameVersion;
		}
	}
}
