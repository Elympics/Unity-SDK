using MessagePack;

namespace Elympics.Models.Matchmaking.WebSocket
{
    [Union(0, typeof(Ping))]
    [Union(1, typeof(Pong))]
    [Union(2, typeof(MatchFound))]
    [Union(3, typeof(MatchData))]
    [Union(4, typeof(MatchmakingError))]
    public interface IFromMatchmaker
    { }
}
