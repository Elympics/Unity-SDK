using System;
using Elympics.Lobby.Models;

#nullable enable

namespace Elympics.Communication.Lobby.Models.FromLobby
{
    internal interface ILobbyResponse : IFromLobby
    {
        Guid RequestId { get; init; }
    }
}
