using System;

#nullable enable

namespace Elympics.Communication.Lobby.InternalModels.FromLobby
{
    internal interface ILobbyResponse : IFromLobby
    {
        Guid RequestId { get; }
    }
}
