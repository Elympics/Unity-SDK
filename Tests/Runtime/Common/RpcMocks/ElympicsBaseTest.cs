using System.Collections.Generic;

namespace Elympics.Tests.RpcMocks
{
    public record ElympicsStatus(bool IsClient, bool IsServer, bool IsBot)
    {
        public static ElympicsStatus StandaloneClient => new(true, false, false);
        public static ElympicsStatus StandaloneServer => new(false, true, false);
        public static ElympicsStatus StandaloneBot => new(false, false, true);
        public static ElympicsStatus ServerWithBots => new(false, true, true);
        public static ElympicsStatus LocalPlayerWithBots => new(true, true, true);
    }

    public class ElympicsBaseTest : ElympicsBase
    {
        private bool _isClient;
        private bool _isServer;
        private bool _isBot;

        public override bool IsClient => _isClient;
        public override bool IsServer => _isServer;
        public override bool IsBot => _isBot;
        public override long Tick => 0;

        internal override void ElympicsFixedUpdate()
        { }

        internal override void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList)
        {
            var newElympicsRpcMessageList = new ElympicsRpcMessageList
            {
                Tick = rpcMessageList.Tick,
                Messages = new List<ElympicsRpcMessage>(rpcMessageList.Messages),
            };
            RpcMessagesToInvoke.Add(newElympicsRpcMessageList);
        }

        public void SetElympicsStatus(ElympicsStatus status) => (_isClient, _isServer, _isBot) = status;

        public void ClearRpcQueues()
        {
            RpcMessagesToSend.Messages.Clear();
            RpcMessagesToInvoke.Clear();
        }
    }
}
