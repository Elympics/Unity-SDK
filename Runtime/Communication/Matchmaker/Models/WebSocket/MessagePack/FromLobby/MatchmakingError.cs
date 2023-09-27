using MessagePack;

namespace Elympics.Models.Matchmaking.WebSocket
{
    [MessagePackObject]
    public readonly struct MatchmakingError : IFromMatchmaker
    {
        [Key(0)] public ErrorBlame ErrorBlame { get; }
        [Key(1)] public MatchmakerStatusCodes StatusCode { get; }

        public MatchmakingError(ErrorBlame errorBlame, MatchmakerStatusCodes statusCode)
        {
            ErrorBlame = errorBlame;
            StatusCode = statusCode;
        }
    }
}
