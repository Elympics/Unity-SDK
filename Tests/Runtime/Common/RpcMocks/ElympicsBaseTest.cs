namespace Elympics.Tests.RpcMocks
{
    public record ElympicsStatus(bool IsClient, bool IsServer, bool IsBot, int PlayerIndex = 0)
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
        private int _playerIndex;

        public override bool IsClient => _isClient;
        public override bool IsServer => _isServer;
        public override bool IsBot => _isBot;
        public override long Tick => _tick;
        private long _tick;

        public void SetTick(long tick) => _tick = tick;

        public override ElympicsPlayer Player => _isClient || _isBot
            ? ElympicsPlayer.FromIndex(_playerIndex)
            : _isServer ? ElympicsPlayer.World : ElympicsPlayer.Invalid;

        internal override void ElympicsFixedUpdate()
        { }

        internal override void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) =>
            RpcMessagesToInvoke.AddRange(rpcMessageList);

        public void SetElympicsStatus(ElympicsStatus status) => (_isClient, _isServer, _isBot, _playerIndex) = status;

        public void ClearRpcQueues()
        {
            RpcMessagesToSend.Clear();
            RpcMessagesToInvoke.Clear();
        }
    }
}
