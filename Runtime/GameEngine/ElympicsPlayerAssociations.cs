using System.Collections.Generic;

namespace Elympics
{
	public static class ElympicsPlayerAssociations
	{
		public static Dictionary<string, ElympicsPlayer> GetUserIdsToPlayers(List<string> userIds)
		{
			var userIdsToPlayers = new Dictionary<string, ElympicsPlayer>();
			for (var i = 0; i < userIds.Count; i++)
				userIdsToPlayers.Add(userIds[i], ElympicsPlayer.FromIndex(i));
			return userIdsToPlayers;
		}

		public static Dictionary<ElympicsPlayer, string> GetPlayersToUserIds(List<string> userIds)
		{
			var playersToUserIds = new Dictionary<ElympicsPlayer, string>();
			for (var i = 0; i < userIds.Count; i++)
				playersToUserIds.Add(ElympicsPlayer.FromIndex(i), userIds[i]);

			return playersToUserIds;
		}
	}
}
