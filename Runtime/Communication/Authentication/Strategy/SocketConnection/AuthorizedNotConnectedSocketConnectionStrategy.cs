using System;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    internal class AuthorizedNotConnectedStrategy : ConnectionStrategy
    {
        public AuthorizedNotConnectedStrategy(WebSocketSession socketSession, ElympicsLoggerContext logger) : base(socketSession, logger)
        { }

        public override async UniTask<GameDataResponse?> Connect(SessionConnectionDetails newConnectionDetails)
        {
            try
            {
                return await ConnectToLobby(newConnectionDetails);
            }
            catch (Exception e)
            {
                var logger = Logger.WithContext($"{nameof(AuthorizedNotConnectedStrategy)}").WithMethodName();
                throw logger.CaptureAndThrow(e);
            }
        }
    }
}
