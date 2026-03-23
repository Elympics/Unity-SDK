using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels
{
    [MessagePackObject]
    public record RoomCurrencyDto(
        [property: Key(0)] string Ticker,
        [property: Key(1)] string? Address,
        [property: Key(2)] int Decimals,
        [property: Key(3)] string IconUrl);
}
