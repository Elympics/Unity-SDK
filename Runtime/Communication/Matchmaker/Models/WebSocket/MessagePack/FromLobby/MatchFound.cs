using System;
using MessagePack;

namespace Elympics.Models.Matchmaking.WebSocket
{
    [MessagePackObject]
    public readonly struct MatchFound : IFromLobby
    {
        [Key(0)] public Guid MatchId { get; }

        public MatchFound(Guid matchId)
        {
            MatchId = matchId;
        }
    }
}
