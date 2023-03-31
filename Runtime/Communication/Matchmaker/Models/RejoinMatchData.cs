using System;

namespace Elympics.Models.Matchmaking
{
	public struct RejoinMatchData
	{
		public Guid GameId { get; set; }
		public string GameVersion { get; set; }
	}
}
