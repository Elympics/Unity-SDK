using System;
using MessagePack;

#nullable enable

namespace Elympics.Lobby.Models
{
    public abstract record LobbyOperation([property: Key(0)] Guid OperationId) : IToLobby
    {
        protected LobbyOperation() : this(Guid.NewGuid())
        { }
    }
}
