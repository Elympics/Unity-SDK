using System;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;

namespace Elympics
{
    internal class AuthorizedNotConnectedStrategy : ConnectionStrategy
    {
        public AuthorizedNotConnectedStrategy(WebSocketSession socketSession, ElympicsLoggerContext logger) : base(socketSession, logger)
        { }
        public override async UniTask Connect(SessionConnectionDetails newConnectionDetails)
        {
            try
            {
                await ConnectToLobby(newConnectionDetails);
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException(e);
            }
        }
    }
}
