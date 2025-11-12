#nullable enable
using System;
using System.Collections.Generic;
using Elympics.Communication.Authentication.Models;

namespace Elympics.Communication.Models.Public
{
    public struct MatchInitialData
    {
        public Guid MatchId { get; init; }
        public bool IsReplay { get; init; }
        public string QueueName { get; init; }
        public string RegionName { get; init; }
        public IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>> CustomRoomData { get; init; }
        public IReadOnlyDictionary<string, string>? CustomMatchmakingData { get; init; }
        public byte[]? ExternalGameData { get; init; }
        public IReadOnlyCollection<PlayerInitialData> PlayerInitialDatas { get; init; }
    }

    public struct PlayerInitialData
    {
        public ElympicsPlayer Player { get; init; }
        public Guid UserId { get; init; }
        public bool IsBot { get; init; }
        public double BotDifficulty { get; init; }
        public byte[] GameEngineData { get; init; }
        public float[] MatchmakerData { get; init; }
        public Guid? RoomId { get; init; }
        public uint? TeamIndex { get; init; }
        public string? Nickname { get; init; }
        public NicknameType NicknameType { get; init; }
        public IReadOnlyDictionary<string, string>? CustomData { get; init; }
    }
}
