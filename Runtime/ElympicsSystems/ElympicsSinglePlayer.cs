using UnityEngine;

namespace Elympics
{
    public class ElympicsSinglePlayer : ElympicsBase
    {
        [SerializeField] private ElympicsClient client;
        [SerializeField] private ElympicsServer server;
        public override long Tick => server.Tick;

        internal override void ElympicsFixedUpdate() { }

        internal override void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) => client.SendRpcMessageList(rpcMessageList);
    }
}
