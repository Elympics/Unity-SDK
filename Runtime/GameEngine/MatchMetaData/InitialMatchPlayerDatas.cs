using System.Collections.Generic;

namespace Elympics
{
	public class InitialMatchPlayerDatas : List<InitialMatchPlayerData>
	{
		public InitialMatchPlayerDatas()
		{
		}

		public InitialMatchPlayerDatas(List<InitialMatchPlayerData> playerDatas) : base(playerDatas)
		{
		}
	}
}
