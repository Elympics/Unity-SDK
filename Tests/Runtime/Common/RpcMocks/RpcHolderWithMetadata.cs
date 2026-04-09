#nullable enable

namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderWithMetadata : ElympicsMonoBehaviour
    {
        public RpcMetadata? StpWithOptionalMetadataArgs;
        public RpcMetadata? PtsWithOptionalMetadataArgs;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void StpWithOptionalMetadataUsingDefault(RpcMetadata metadata = default) => StpWithOptionalMetadataArgs = metadata;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void StpWithOptionalMetadataUsingNew(RpcMetadata metadata = new()) => StpWithOptionalMetadataArgs = metadata;

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PtsWithOptionalMetadataUsingDefault(RpcMetadata metadata = default) => PtsWithOptionalMetadataArgs = metadata;

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PtsWithOptionalMetadataUsingNew(RpcMetadata metadata = new()) => PtsWithOptionalMetadataArgs = metadata;

        public void Reset()
        {
            StpWithOptionalMetadataArgs = null;
            PtsWithOptionalMetadataArgs = null;
        }
    }
}

