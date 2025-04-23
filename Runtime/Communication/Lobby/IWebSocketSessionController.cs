using Cysharp.Threading.Tasks;

namespace Elympics.Lobby
{
    internal interface IWebSocketSessionController
    {
        UniTask ReconnectIfPossible(DisconnectionData reason);
    }
}
