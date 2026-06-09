using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.InternalModels.FromLobby;
using Elympics.Core.Logger;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;

#nullable enable

namespace Elympics
{
    internal class UnauthorizedSocketConnectionStrategy : ConnectionStrategy
    {
        public UnauthorizedSocketConnectionStrategy(WebSocketSession socketSession) : base(socketSession)
        { }

        public override UniTask<GameDataResponseDto?> Connect(SessionConnectionDetails newConnectionDetails) =>
            throw new ElympicsException("Connecting canceled because user is not authenticated.");
    }
}
