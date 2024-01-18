using System;
using MessagePack;

namespace Elympics.Lobby.Models
{
    [MessagePackObject]
    public record JoinLobby(
        [property: Key(1)] string SdkVersion,
        [property: Key(2)] Guid GameId,
        [property: Key(3)] string GameVersion,
        [property: Key(4)] string RegionName) : LobbyOperation
    {
        [SerializationConstructor]
        public JoinLobby(Guid operationId, string sdkVersion, Guid gameId, string gameVersion, string regionName) : this(sdkVersion, gameId, gameVersion, regionName) =>
            OperationId = operationId;
    }
}
