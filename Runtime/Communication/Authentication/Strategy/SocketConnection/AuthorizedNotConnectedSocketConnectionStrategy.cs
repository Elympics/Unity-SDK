using System;
using Cysharp.Threading.Tasks;
using Elympics.Lobby;

namespace Elympics
{
    internal class AuthorizedNotConnectedStrategy : ConnectionStrategy
    {
        public AuthorizedNotConnectedStrategy(WebSocketSession socketSession) : base(socketSession)
        { }
        public override async UniTask Connect(SessionConnectionDetails newConnectionDetails)
        {
            try
            {
                await WebSocketSession.Connect(newConnectionDetails);
                ElympicsLogger.Log($"Successfully connected to lobby.\n Connection details: {newConnectionDetails}");
            }
            catch (Exception e)
            {
                _ = ElympicsLogger.LogException(e);
            }
        }
    }
}
