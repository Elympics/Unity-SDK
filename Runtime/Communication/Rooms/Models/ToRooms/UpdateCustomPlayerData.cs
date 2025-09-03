#nullable enable

using System;
using System.Collections.Generic;
using Elympics.Lobby.Models;
using MessagePack;

namespace Elympics.Rooms.Models
{
    public record UpdateCustomPlayerData(
        [property: Key(1)] Guid RoomId,
        [property: Key(2)] Dictionary<string, string> CustomPlayerData) : LobbyOperation;
}
