#nullable enable

using System;
using MessagePack;

namespace Elympics.Communication.Lobby.InternalModels.ToLobby
{
    public abstract record LobbyOperation([property: Key(0)] Guid OperationId) : IToLobby
    {
        protected LobbyOperation() : this(Guid.NewGuid())
        { }
    }
}
