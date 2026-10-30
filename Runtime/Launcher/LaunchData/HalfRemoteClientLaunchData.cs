#nullable enable

using System.Collections.Generic;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>Every property is optional: null means the value from <see cref="ElympicsGameConfig"/> (or its application parameter).</summary>
    /// <remarks>
    /// The player list, with bot flags, always comes from <see cref="ElympicsGameConfig.TestPlayers"/>,
    /// as the half-remote server derives the same player IDs from its own config.
    /// </remarks>
    [PublicAPI]
    public sealed class HalfRemoteClientLaunchData : LaunchData
    {
        /// <summary><c>ip:port</c> of the server. Null means <see cref="ElympicsGameConfig.IpForHalfRemoteMode"/> and <see cref="ElympicsGameConfig.PortForHalfRemoteMode"/>.</summary>
        public string? ServerAddress { get; }
        /// <summary>Null means <see cref="ElympicsGameConfig.UseWeb"/> decides.</summary>
        public bool? UseTcpUdpInsteadOfWebRtc { get; }
        /// <summary>Index into <see cref="ElympicsGameConfig.TestPlayers"/>. Null means <see cref="ElympicsGameConfig.PlayerIndexForHalfRemoteMode"/>.</summary>
        public int? PlayerIndex { get; }
        /// <summary>Null means the region of <see cref="ElympicsGameConfig.TestMatchData"/>.</summary>
        public string? RegionName { get; }
        /// <summary>Null means the queue of <see cref="ElympicsGameConfig.TestMatchData"/>.</summary>
        public string? QueueName { get; }
        /// <summary>Of the local player. Null means the value of its <see cref="ElympicsGameConfig.TestPlayers"/> entry.</summary>
        public IReadOnlyList<byte>? GameEngineData { get; }
        /// <summary>Of the local player. Null means the value of its <see cref="ElympicsGameConfig.TestPlayers"/> entry.</summary>
        public IReadOnlyList<float>? MatchmakerData { get; }
        /// <summary>
        /// Custom matchmaking data in the client's match data; set it to the server's, which the client doesn't receive.
        /// Null means the <c>ELYMPICS_CUSTOM_MATCHMAKING_DATA</c> parameter, else <see cref="ElympicsGameConfig.TestMatchData"/>. Empty means none (an empty array on the wire means unset).
        /// </summary>
        public IReadOnlyDictionary<string, string>? CustomMatchmakingData { get; }

        public HalfRemoteClientLaunchData(string? serverAddress = null, bool? useTcpUdpInsteadOfWebRtc = null, int? playerIndex = null,
            string? regionName = null, string? queueName = null, IEnumerable<byte>? gameEngineData = null, IEnumerable<float>? matchmakerData = null,
            IReadOnlyDictionary<string, string>? customMatchmakingData = null)
        {
            ServerAddress = serverAddress;
            UseTcpUdpInsteadOfWebRtc = useTcpUdpInsteadOfWebRtc;
            PlayerIndex = playerIndex;
            RegionName = regionName;
            QueueName = queueName;
            GameEngineData = gameEngineData == null ? null : Copy(gameEngineData);
            MatchmakerData = matchmakerData == null ? null : Copy(matchmakerData);
            CustomMatchmakingData = CopyOrNull(customMatchmakingData);
        }

        internal override string ModeName => "HalfRemoteClient";

        internal override bool TryValidate(ElympicsGameConfig gameConfig, ElympicsLaunchPlatform platform, out ElympicsLaunchFailure? failure)
        {
            if (!platform.HasHalfRemoteClient)
                return Fail(ElympicsLaunchErrorCode.UnsupportedMode, $"{ModeName} is not supported: {ElympicsLaunchPlatform.NoHalfRemoteClientMessage}.", out failure);
            if (UseTcpUdpInsteadOfWebRtc == true && !platform.CanUseTcpUdp)
                return Fail(ElympicsLaunchErrorCode.UnsupportedMode, ElympicsLaunchPlatform.NoTcpUdpMessage, out failure);
            if (ServerAddress != null && !LaunchDataParsing.TryParseEndPoint(ServerAddress, out _))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(ServerAddress)} must be ip:port, got \"{ServerAddress}\".", out failure);
            if (PlayerIndex is < 0)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(PlayerIndex)} must not be negative, got {PlayerIndex}.", out failure);
            return TryValidateCustomMatchmakingData(CustomMatchmakingData, out failure);
        }

#if UNITY_WEBGL && !UNITY_EDITOR && !ELYMPICS_WEBGL_HALF_REMOTE
        // Unreachable after validation. Not referencing the initializer lets the linker drop the half-remote client.
        private protected override GameSceneInitializer CreateInitializer(GameSceneLaunchContext context) =>
            throw new ElympicsException(ElympicsLaunchPlatform.NoHalfRemoteClientMessage);
#else
        private protected override GameSceneInitializer CreateInitializer(GameSceneLaunchContext context) => new HalfRemoteGameClientInitializer(this);
#endif
    }
}
