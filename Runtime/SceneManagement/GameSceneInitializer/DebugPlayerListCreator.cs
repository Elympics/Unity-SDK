using System;
using System.Collections.Generic;
using System.Linq;
using GameEngineCore.V1._3;

namespace Elympics
{
	internal static class DebugPlayerListCreator
	{
		internal static List<InitialMatchUserData> CreatePlayersList(ElympicsGameConfig elympicsGameConfig)
		{
			var userIdSet = new HashSet<string>(elympicsGameConfig.TestPlayers.Select(x => x.userId));
			if (userIdSet.Count != elympicsGameConfig.TestPlayers.Count)
				throw new ArgumentException("User ids in test players should be unique!");

			return elympicsGameConfig.TestPlayers.Select(initialUserData => new InitialMatchUserData
				{
					UserId = initialUserData.userId,
					IsBot = initialUserData.isBot,
					BotDifficulty = initialUserData.botDifficulty,
					MatchmakerData = initialUserData.matchmakerData,
					GameEngineData = initialUserData.gameEngineData
				})
				.ToList();
		}
	}
}
