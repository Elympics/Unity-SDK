#nullable enable

namespace Elympics.Core.Logger.State
{
    internal abstract class GameServerState : IVisitableState
    {
        public readonly string ConnectionType;
        public string? ServerAddress;

        protected GameServerState(string connectionType) => ConnectionType = connectionType;

        public virtual bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(ConnectionType), ConnectionType);
            visitor.ProcessProperty(nameof(ServerAddress), ServerAddress);
            return true;
        }
    }

    internal sealed class WebRtcState : GameServerState
    {
        public bool UsesTurn;

        public WebRtcState() : base("WebRTC") { }

        public override bool Visit(IStateVisitor visitor)
        {
            base.Visit(visitor);
            visitor.ProcessProperty(nameof(UsesTurn), UsesTurn.ToString());
            return true;
        }
    }

    internal sealed class TcpUdpState : GameServerState
    {
        public TcpUdpState() : base("TCP/UDP") { }
    }
}
