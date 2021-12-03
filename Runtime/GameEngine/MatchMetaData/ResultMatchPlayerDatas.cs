using System.Collections.Generic;

namespace Elympics
{
	public class ResultMatchPlayerDatas : List<ResultMatchPlayerData>
	{
		public ResultMatchPlayerDatas()
		{
		}

		public ResultMatchPlayerDatas(List<ResultMatchPlayerData> playerDatas) : base(playerDatas)
		{
		}
	}
}
