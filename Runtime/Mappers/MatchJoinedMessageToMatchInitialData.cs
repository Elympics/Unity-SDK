using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Communication.Models.Public;
using MatchTcpModels.Messages;


namespace Elympics.Mappers
{
    public static class MatchJoinedMessageToMatchInitialData
    {
        public static MatchInitialData Map(this MatchJoinedMessage matchJoinedMessage)
        {
            if (!Guid.TryParse(matchJoinedMessage.MatchId, out var matchId))
                throw new FormatException($"Invalid MatchId GUID format: {matchJoinedMessage.MatchId}");

            if (matchJoinedMessage.CustomMatchmakingDataKeys.Length != matchJoinedMessage.CustomMatchmakingDataValues.Length)
                throw new ArgumentException(
                    $"The length of {nameof(matchJoinedMessage.CustomMatchmakingDataKeys)} ({matchJoinedMessage.CustomMatchmakingDataKeys}) is not the same as the length of {nameof(matchJoinedMessage.CustomMatchmakingDataValues)} ({matchJoinedMessage.CustomMatchmakingDataValues.Length}).",
                    nameof(matchJoinedMessage));

            var pointer = 0;
            return new MatchInitialData
            {
                MatchId = matchId,
                IsReplay = false,
                QueueName = matchJoinedMessage.QueueName,
                RegionName = matchJoinedMessage.RegionName,
                CustomRoomData = matchJoinedMessage.RoomGuids.Select((roomId, index) =>
                {
                    var guid = Guid.Parse(roomId);
                    var roomCustomData = new Dictionary<string, string>();
                    var dataAmount = matchJoinedMessage.CustomRoomDataNumberPerRoom[index];
                    var dataCounter = 0;
                    while (dataCounter < dataAmount)
                    {
                        var key = matchJoinedMessage.CustomRoomDataKeys[pointer];
                        var value = matchJoinedMessage.CustomRoomDataValues[pointer];
                        roomCustomData.Add(key, value);
                        ++dataCounter;
                        ++pointer;
                    }
                    return (guid, (IReadOnlyDictionary<string, string>)roomCustomData);
                }).ToDictionary(pair => pair.guid, pair => pair.Item2),
                CustomMatchmakingData = matchJoinedMessage.CustomMatchmakingDataKeys.Select((key, index) =>
                {
                    var value = matchJoinedMessage.CustomMatchmakingDataValues[index];
                    return (key, value);
                }).ToDictionary(pair => pair.key, pair => pair.value),
                ExternalGameData = matchJoinedMessage.ExternalGameData,
                PlayerInitialDatas = RetrieveInitialMatchPlayerData(matchJoinedMessage)
            };
        }

        private static List<PlayerInitialData> RetrieveInitialMatchPlayerData(MatchJoinedMessage matchJoinedMessage)
        {
            return matchJoinedMessage.UserInitialMatchData.Select((x, index) => new PlayerInitialData
            {
                Player = ElympicsPlayer.FromIndex(index),
                UserId = Guid.Parse(x.UserId),
                IsBot = x.IsBot,
                BotDifficulty = x.BotDifficulty,
                GameEngineData = x.GameEngineData,
                MatchmakerData = x.MatchmakerData,
                RoomId = Guid.Parse(x.RoomId),
                TeamIndex = x.TeamIndex,
                Nickname = x.Nickname,
                NicknameType = NicknameMapper.ConvertToNickNameType(x.NicknameType),
                CustomData = x.CustomDataKeys?.Select((key, index) =>
                    {
                        var value = x.CustomDataValues[index];
                        return (key, value);
                    }).ToDictionary(kvp => kvp.key, kvp => kvp.value)
                    ?? new Dictionary<string, string>(),
            }).ToList();
        }
    }
}
