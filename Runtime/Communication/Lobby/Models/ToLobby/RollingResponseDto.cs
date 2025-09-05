using System;
using MessagePack;

#nullable enable

namespace Communication.Lobby.Models.ToLobby
{
    [MessagePackObject]
    public class RollingResponseDto
    {
        [Key(0)] public Guid RollingTournamentBetConfigId { get; set; }
        //This data is included by backend but no needed by SDK, so we let MessagePack skip it on deserialization
        // [Key(1)] public Guid   CoinId       { get; set; }
        // [Key(2)] public uint   PlayersCount { get; set; }
        // [Key(3)] public string Prize        { get; set; }
        // [Key(6)] public int[] PrizeDistribution { get; set; }
        [Key(4)] public string EntryFee { get; set; }
        [Key(5)] public string Error { get; set; }
    }
}
