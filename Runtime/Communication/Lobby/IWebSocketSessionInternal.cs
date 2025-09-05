using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby.Models;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics.Lobby
{
    internal interface IWebSocketSessionInternal
    {
        event Action? Connected;
        event Action<IFromLobby>? MessageReceived;

        SessionConnectionDetails? ConnectionDetails { get; }
        UniTask<GameDataResponseDto> Connect(SessionConnectionDetails details, CancellationToken ct = default);
        void Disconnect(DisconnectionReason reason);
        UniTask<OperationResultDto> ExecuteOperation(LobbyOperation message, CancellationToken ct = default);
    }
}
