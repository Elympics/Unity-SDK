using System.Collections.Generic;

namespace ElympicsApiModels.ApiModels.Games
{
	public class GetGameVersionsResponseModel
	{
		public string GameName { get; set; }

		public List<GameVersionResponseModel> Versions { get; set; }
	}
}