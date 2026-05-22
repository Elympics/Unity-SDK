#nullable enable

namespace Elympics.Core.Logger.State
{
    internal abstract class GameServerState : IVisitableState
    {
        public string ConnectionType;
        public string ServerAddress;

        public GameServerState(string connectionType, string serverAddress) => (ConnectionType, ServerAddress) = (connectionType, serverAddress);

        public virtual void Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(ConnectionType), ConnectionType);
            visitor.ProcessProperty(nameof(ServerAddress), ServerAddress);
        }
    }

    internal sealed class WebRtcState : GameServerState
    {
        public bool UsesTurn;

        public WebRtcState(string serverAddress) : base("WebRTC", serverAddress) { }

        public override void Visit(IStateVisitor visitor)
        {
            base.Visit(visitor);
            visitor.ProcessProperty(nameof(UsesTurn), UsesTurn.ToString());
        }
    }

    internal sealed class TcpUdpState : GameServerState
    {
        public TcpUdpState(string serverAddress) : base("TCP/UDP", serverAddress) { }
    }
}
