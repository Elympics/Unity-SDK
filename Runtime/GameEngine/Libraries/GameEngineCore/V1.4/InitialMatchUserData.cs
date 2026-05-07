#nullable enable
using System;
using System.Collections.Generic;

namespace GameEngineCore.V1._4
{
    public class InitialMatchUserData
    {
        public Guid UserId { get; set; }
        public bool IsBot { get; set; }
        public double BotDifficulty { get; set; }
        public float[]? MatchmakerData { get; set; }
        public byte[]? GameEngineData { get; set; }
        public Guid? RoomId { get; set; }
        public uint? TeamIndex { get; set; }

        public string? Telegramid { get; set; }

        public string? Address { get; set; }
        public string? Nickname { get; set; }
        public string? NicknameType { get; set; }
        public IReadOnlyDictionary<string, string>? CustomData { get; set; }
    }
}
