using System;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.InternalModels.FromLobby;
using Elympics.Core.Logger;
using Elympics.ElympicsSystems.Internal;
using Elympics.Lobby;

#nullable enable

namespace Elympics
{
    internal class AuthorizedNotConnectedStrategy : ConnectionStrategy
    {
        public AuthorizedNotConnectedStrategy(WebSocketSession socketSession, ApplicationState logger) : base(socketSession, logger)
        { }

        public override async UniTask<GameDataResponseDto?> Connect(SessionConnectionDetails newConnectionDetails)
        {
            try
            {
                return await ConnectToLobby(newConnectionDetails);
            }
            catch (Exception e)
            {
                var logger = Logger.WithContext($"{nameof(AuthorizedNotConnectedStrategy)}").WithMethodName();
                throw logger.LogExceptionAndReturn(e);
            }
        }
    }
}
