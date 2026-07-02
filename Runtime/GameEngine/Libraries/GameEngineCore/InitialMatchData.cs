#nullable enable
using System;
using System.Collections.Generic;

namespace GameEngineCore
{
    public class InitialMatchData
    {
        public IReadOnlyList<InitialMatchUserData> UserData { get; set; } = null!;
        public Guid? MatchId { get; set; }
        public string? QueueName { get; set; }
        public string? RegionName { get; set; }
        public IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string>>? CustomRoomData { get; set; }
        public IReadOnlyDictionary<string, string>? CustomMatchmakingData { get; set; }
        public byte[]? ExternalGameData { get; set; }
    }
}
