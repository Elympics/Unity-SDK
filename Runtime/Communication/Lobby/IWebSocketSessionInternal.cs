using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby.Models;

#nullable enable

namespace Elympics.Lobby
{
    internal interface IWebSocketSessionInternal
    {
        event Action? Connected;
        event Action<DisconnectionData>? Disconnected;
        event Action<IFromLobby>? MessageReceived;

        SessionConnectionDetails ConnectionDetails { get; }
        UniTask Connect(SessionConnectionDetails details, CancellationToken ct = default);
        void Disconnect(DisconnectionReason reason);

        UniTask<OperationResult> ExecuteOperation(LobbyOperation message, CancellationToken ct = default);
    }
}
