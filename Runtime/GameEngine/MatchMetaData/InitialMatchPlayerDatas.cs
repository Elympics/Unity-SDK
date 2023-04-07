using System;
using System.Collections.Generic;
using System.Linq;

namespace Elympics
{
	[Obsolete("Use " + nameof(InitialMatchPlayerDatasGuid) + " instead")]
	public class InitialMatchPlayerDatas : List<InitialMatchPlayerData>
	{
		public InitialMatchPlayerDatas()
		{
		}

		public InitialMatchPlayerDatas(List<InitialMatchPlayerData> playerDatas) : base(playerDatas)
		{
		}

		public InitialMatchPlayerDatas(InitialMatchPlayerDatasGuid initialMatchPlayerDatasGuid)
			: this(initialMatchPlayerDatasGuid.Select(x => new InitialMatchPlayerData(x)).ToList())
		{ }
	}
}
