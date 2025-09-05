using System;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Lobby.InternalModels.ToLobby
{
    public abstract record LobbyOperation([property: Key(0)] Guid OperationId) : IToLobby
    {
        protected LobbyOperation() : this(Guid.NewGuid())
        { }
    }
}
