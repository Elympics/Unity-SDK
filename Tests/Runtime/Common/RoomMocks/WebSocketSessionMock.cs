using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;
using Elympics.Lobby.Models;

#nullable enable

namespace Elympics.Tests.Common.RoomMocks
{
    internal class WebSocketSessionMock : IWebSocketSessionInternal
    {
        public event Action? Connected
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }
        public event Action<DisconnectionData>? Disconnected
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event Action<IFromLobby>? MessageReceived;
        public bool IsConnected => throw new NotImplementedException();
        public SessionConnectionDetails ConnectionDetails
        {
            get => _connectionDetails ?? throw new InvalidOperationException();
            set => _connectionDetails = value;
        }
        private SessionConnectionDetails? _connectionDetails;

        public UniTask Connect(SessionConnectionDetails details, CancellationToken ct = default) =>
            throw new NotImplementedException();
        public void Disconnect(DisconnectionReason reason) => throw new NotImplementedException();

        public UniTask<OperationResult> ExecuteOperation(LobbyOperation message, CancellationToken ct = default)
        {
            ExecutedOperations.Add(message);
            return UniTask.FromResult(ResultToReturn);
        }

        public readonly List<IToLobby> ExecutedOperations = new();

        public OperationResult ResultToReturn = new(Guid.Empty);

        public void InvokeMessageReceived(IFromLobby message) => MessageReceived?.Invoke(message);

        public void Reset()
        {
            ExecutedOperations.Clear();
            ResultToReturn = new OperationResult(Guid.Empty);
            _connectionDetails = null;
        }
    }
}
