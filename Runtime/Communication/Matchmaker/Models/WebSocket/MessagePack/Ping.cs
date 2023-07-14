using MessagePack;

namespace Elympics.Models.Matchmaking.WebSocket
{
    [MessagePackObject]
    public readonly struct Ping : IFromLobby, IToLobby
    {
    }
}
