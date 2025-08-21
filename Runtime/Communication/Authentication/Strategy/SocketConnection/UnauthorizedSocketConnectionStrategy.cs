using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    internal class UnauthorizedSocketConnectionStrategy : ConnectionStrategy
    {
        public UnauthorizedSocketConnectionStrategy(WebSocketSession socketSession, ElympicsLoggerContext logger) : base(socketSession, logger)
        { }

        public override UniTask<GameDataResponse?> Connect(SessionConnectionDetails newConnectionDetails) =>
            throw new ElympicsException("Connecting canceled because user is not authenticated.");
    }
}
