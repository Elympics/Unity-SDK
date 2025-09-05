using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record RoomTournamentDetails([property: Key(0)] string TournamentId, [property: Key(1)] ChainType? ChainType);
}
