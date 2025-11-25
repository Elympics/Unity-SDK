#nullable enable
using System;
using System.Collections.Generic;

namespace MatchTcpModels.Messages
{
#pragma warning disable CS8618 // "Non-nullable field must contain a non-null value when exiting constructor" - This class is created using deserialization, but nullable annotations match the contract, so we want to keep them, but we can't use constructor for deserialization.
    [Serializable]
    public class MatchJoinedMessage : Message
    {
        public string MatchId;
        public string QueueName;
        public string RegionName;
        public string[] RoomGuids;
        public int[] CustomRoomDataNumberPerRoom;
        public string[] CustomRoomDataKeys;
        public string[] CustomRoomDataValues;
        public string[] CustomMatchmakingDataKeys;
        public string[] CustomMatchmakingDataValues;
        public byte[]? ExternalGameData;
        public List<InitialMatchPlayerData> UserInitialMatchData;

        public MatchJoinedMessage()
        {
            Type = MessageType.MatchJoined;
        }

        [Serializable]
        public class InitialMatchPlayerData
        {
            public string UserId;
            public bool IsBot;
            public double BotDifficulty;
            public byte[] GameEngineData;
            public float[] MatchmakerData;
            public string RoomId;
            public uint TeamIndex;
            public string Nickname;
            public string NicknameType;
            public string[] CustomDataKeys;
            public string[] CustomDataValues;
        }
    }
}
