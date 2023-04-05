using System;

namespace Elympics
{
	public class InitialMatchPlayerDataGuid
	{
		public ElympicsPlayer Player         { get; set; }
		public Guid           UserId         { get; set; }
		public bool           IsBot          { get; set; }
		public double         BotDifficulty  { get; set; }
		public byte[]         GameEngineData { get; set; }
		public float[]        MatchmakerData { get; set; }
	}
}
