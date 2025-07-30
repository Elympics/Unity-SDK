using System;
using Elympics.Lobby.Models;

namespace Elympics.Communication.Lobby.Models.FromLobby
{
    public interface IDataFromLobby : IFromLobby
    {
        Guid RequestId { get; init; }
    }
}
