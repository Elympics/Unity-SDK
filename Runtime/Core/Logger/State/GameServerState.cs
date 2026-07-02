#nullable enable

namespace Elympics.Core.Logger.State
{
    internal class GameServerState : IVisitableState
    {
        public string? ConnectionType;
        public string? TcpUdpServerAddress;
        public string? WebServerAddress;
        public bool? UsesTurn;

        private static class Names
        {
            public const string ConnectionType = "connectionType";
            public const string TcpUdpServerAddress = "tcpUdpServerAddress";
            public const string WebServerAddress = "webServerAddress";
            public const string UsesTurn = "usesTurn";
        }

        public virtual bool Visit(IStateVisitor visitor)
        {
            var visited = false;
            visited |= visitor.ProcessOptionalProperty(Names.ConnectionType, ConnectionType);
            visited |= visitor.ProcessOptionalProperty(Names.TcpUdpServerAddress, TcpUdpServerAddress);
            visited |= visitor.ProcessOptionalProperty(Names.WebServerAddress, WebServerAddress);
            visited |= visitor.ProcessOptionalProperty(Names.UsesTurn, UsesTurn?.ToString());
            return visited;
        }
    }
}
