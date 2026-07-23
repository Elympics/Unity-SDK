#nullable enable

using System;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Lobby.InternalModels.FromLobby;
using Elympics.Core.Logger;
using Elympics.Lobby;

namespace Elympics
{
    internal class AuthorizedNotConnectedStrategy : ConnectionStrategy
    {
        public AuthorizedNotConnectedStrategy(WebSocketSession socketSession) : base(socketSession)
        { }

        public override async UniTask<GameDataResponseDto?> Connect(SessionConnectionDetails newConnectionDetails)
        {
            try
            {
                return await ConnectToLobby(newConnectionDetails);
            }
            catch (Exception e)
            {
                var logger = Logger.WithClass(typeof(AuthorizedNotConnectedStrategy))
                    .WithMethodName();
                throw logger.LogExceptionAndReturn(e);
            }
        }
    }
}
