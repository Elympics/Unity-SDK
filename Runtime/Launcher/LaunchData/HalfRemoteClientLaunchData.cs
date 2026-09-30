#nullable enable

using System.Collections.Generic;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>
    /// Joins a half-remote server (see <see cref="HalfRemoteServerLaunchData"/>) as one of its test players.
    /// Every property is optional: null means the value from <see cref="ElympicsGameConfig"/> (or its application parameter).
    /// </summary>
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

        public HalfRemoteClientLaunchData(string? serverAddress = null, bool? useTcpUdpInsteadOfWebRtc = null, int? playerIndex = null,
            string? regionName = null, string? queueName = null, IEnumerable<byte>? gameEngineData = null, IEnumerable<float>? matchmakerData = null)
        {
            ServerAddress = serverAddress;
            UseTcpUdpInsteadOfWebRtc = useTcpUdpInsteadOfWebRtc;
            PlayerIndex = playerIndex;
            RegionName = regionName;
            QueueName = queueName;
            GameEngineData = gameEngineData == null ? null : Copy(gameEngineData);
            MatchmakerData = matchmakerData == null ? null : Copy(matchmakerData);
        }

        internal override string ModeName => "HalfRemoteClient";

        internal override bool TryValidate(ElympicsGameConfig gameConfig, bool platformCanListen, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            if (UseTcpUdpInsteadOfWebRtc == true && !LaunchDataParsing.CanUseTcpUdp)
                return Fail(ElympicsLaunchErrorCode.UnsupportedMode, LaunchDataParsing.NoTcpUdpMessage, out failure);
            if (ServerAddress != null && !LaunchDataParsing.TryParseEndPoint(ServerAddress, out _))
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(ServerAddress)} must be ip:port, got \"{ServerAddress}\".", out failure);
            if (PlayerIndex is < 0)
                return Fail(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(PlayerIndex)} must not be negative, got {PlayerIndex}.", out failure);
            return true;
        }

        private protected override GameSceneInitializer CreateInitializer(GameSceneLaunchContext context) => new HalfRemoteGameClientInitializer(this);
    }
}
