using System;
using System.Collections.Generic;
using System.Linq;
using GameEngineCore.V1._4;

#nullable enable

namespace Elympics
{
    public class InitialMatchPlayerDatasGuid : List<InitialMatchPlayerDataGuid>
    {
        public Guid? MatchId { get; set; }

        public bool IsReplay { get; set; } = false;
        public string? QueueName { get; set; }
        public string? RegionName { get; set; }
        public IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>>? CustomRoomData { get; set; }
        public IReadOnlyDictionary<string, string>? CustomMatchmakingData { get; set; }
        public byte[]? ExternalGameData { get; set; }

        internal InitialMatchPlayerDatasGuid(IEnumerable<InitialMatchPlayerDataGuid> playerDatas) : base(playerDatas)
        { }

        internal InitialMatchPlayerDatasGuid(InitialMatchData initialMatchData, IReadOnlyDictionary<Guid, ElympicsPlayer> userIdsToPlayers, bool isReplay)
            : base(initialMatchData.UserData.Select(x => new InitialMatchPlayerDataGuid(userIdsToPlayers[x.UserId], x)))
        {
            MatchId = initialMatchData.MatchId;
            IsReplay = isReplay;
            QueueName = initialMatchData.QueueName;
            RegionName = initialMatchData.RegionName;
            CustomRoomData = initialMatchData.CustomRoomData;
            CustomMatchmakingData = initialMatchData.CustomMatchmakingData;
            ExternalGameData = initialMatchData.ExternalGameData;
        }
    }
}
