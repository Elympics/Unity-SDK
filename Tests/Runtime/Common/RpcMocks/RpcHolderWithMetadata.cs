using System;

#nullable enable

namespace Elympics.Tests.RpcMocks
{
    public class RpcHolderWithMetadata : ElympicsMonoBehaviour
    {
        public ValueTuple<RpcMetadata>? StpWithOptionalMetadataArgs;
        public ValueTuple<RpcMetadata>? PtsWithOptionalMetadataArgs;

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void StpWithOptionalMetadataUsingDefault(RpcMetadata metadata = default) => StpWithOptionalMetadataArgs = new ValueTuple<RpcMetadata>(metadata);

        [ElympicsRpc(ElympicsRpcDirection.ServerToPlayers)]
        public void StpWithOptionalMetadataUsingNew(RpcMetadata metadata = new()) => StpWithOptionalMetadataArgs = new ValueTuple<RpcMetadata>(metadata);

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PtsWithOptionalMetadataUsingDefault(RpcMetadata metadata = default) => PtsWithOptionalMetadataArgs = new ValueTuple<RpcMetadata>(metadata);

        [ElympicsRpc(ElympicsRpcDirection.PlayerToServer)]
        public void PtsWithOptionalMetadataUsingNew(RpcMetadata metadata = new()) => PtsWithOptionalMetadataArgs = new ValueTuple<RpcMetadata>(metadata);

        public void Reset()
        {
            StpWithOptionalMetadataArgs = null;
            PtsWithOptionalMetadataArgs = null;
        }
    }
}

