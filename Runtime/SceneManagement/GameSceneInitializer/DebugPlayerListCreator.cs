using System;
using System.Collections.Generic;
using System.Linq;
using GameEngineCore.V1._4;

namespace Elympics
{
    internal static class DebugPlayerListCreator
    {
        internal static List<InitialMatchUserData> CreatePlayersList(ElympicsGameConfig elympicsGameConfig)
        {
            return elympicsGameConfig.TestPlayers.Select((initialUserData, index) => new InitialMatchUserData
            {
                UserId = new Guid(index, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                IsBot = initialUserData.isBot,
                BotDifficulty = initialUserData.botDifficulty,
                MatchmakerData = initialUserData.matchmakerData,
                GameEngineData = initialUserData.gameEngineData
            })
                .ToList();
        }
    }
}
