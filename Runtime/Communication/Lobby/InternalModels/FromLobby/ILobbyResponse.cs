#nullable enable

using System;

namespace Elympics.Communication.Lobby.InternalModels.FromLobby
{
    internal interface ILobbyResponse : IFromLobby
    {
        Guid RequestId { get; }
    }
}
