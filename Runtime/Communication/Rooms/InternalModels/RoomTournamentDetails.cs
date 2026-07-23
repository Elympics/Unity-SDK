#nullable enable

using MessagePack;

namespace Elympics.Communication.Rooms.InternalModels
{
    [MessagePackObject]
    public record RoomTournamentDetails([property: Key(0)] string TournamentId, [property: Key(1)] ChainTypeDto? ChainType);
}
