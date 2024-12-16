using UnityEngine;

namespace Elympics
{
    public class ElympicsSinglePlayer :  ElympicsBase
    {
        [SerializeField] private ElympicsClient client;
        [SerializeField] private ElympicsServer server;

        internal override void ElympicsFixedUpdate() { }

        internal override void SendRpcMessageList(ElympicsRpcMessageList rpcMessageList) => client.SendRpcMessageList(rpcMessageList);
    }
}
